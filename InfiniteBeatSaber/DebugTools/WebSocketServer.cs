using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using InfiniteBeatSaber.Extensions;
using MessageHandler = System.Func<string, string, bool>;
using Zenject;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace InfiniteBeatSaber.DebugTools
{
    internal class WebSocketServer : IInitializable, IDisposable
    {
        private readonly List<WebSocketClient> _clients = new List<WebSocketClient>();
        private readonly List<MessageHandler> _handlers = new List<MessageHandler>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private TcpListener _server;

        public void Initialize()
        {
            Util.AssertDebugBuild();

            StartServerLoop().LogOnFailure();
        }

        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            _cts.Cancel();
        }

        public Action Register(MessageHandler handleMessage)
        {
            _handlers.Add(handleMessage);

            return () =>
            {
                _handlers.Remove(handleMessage);
            };
        }

        public Task SendMessage(string cmd, object args)
        {
            var message = new JArray(cmd, JsonConvert.SerializeObject(args)).ToString();
            return SendMessage(message);
        }

        public Task SendMessage(string message)
        {
            var clients = new List<WebSocketClient>(_clients);
            foreach (var client in clients)
            {
                try
                {
                    client.SendMessage(message, _cts.Token).LogOnFailure();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Info($"WebSocketServer.SendMessage error: {ex}");
                }
            }

            return Task.CompletedTask;
        }

        private async Task StartServerLoop()
        {
            int port = 2019;
            _server = new TcpListener(IPAddress.Loopback, port);

            _server.Start();
            //Plugin.Log.Info($"WebSocketServer listening on {ip}:{port}");

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var client = await Cancelable(_server.AcceptTcpClientAsync(), _cts.Token);
                    HandleClient(client).LogOnFailure();
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _cts.Token)
            {
                // No-op
            }
            finally
            {
                _server?.Stop();
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            WebSocketClient webSocketClient = null;

            byte[] bytes = new byte[client.ReceiveBufferSize];

            try
            {
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    int bytesRead = await stream.ReadAsync(bytes, 0, bytes.Length, _cts.Token);
                    string s = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    var request = new HttpRequest(s);
                    if (!Regex.IsMatch(request.RequestLine, "^GET", RegexOptions.IgnoreCase))
                    {
                        byte[] response404 = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 404 Not Found\r\n" +
                            "Content-Length: 0\r\n\r\n");
                        await stream.WriteAsync(response404, 0, response404.Length, _cts.Token);
                        return;
                    }

                    var origin = request.Origin;
                    if (origin == null || origin.Host != "localhost")
                    {
                        Plugin.Log.Info($"WebSocketServer: Expected Origin \"localhost\" but found Origin \"{origin?.Host ?? "<null>"}\". Closing connection.");
                        byte[] response403 = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 403 Forbidden\r\n" +
                            "Content-Length: 0\r\n\r\n");
                        await stream.WriteAsync(response403, 0, response403.Length, _cts.Token);
                        return;
                    }

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = request.Headers["sec-websocket-key"];
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    await stream.WriteAsync(response, 0, response.Length, _cts.Token);
                    webSocketClient = new WebSocketClient(stream);
                    _clients.Add(webSocketClient);
                }


                while (!_cts.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(bytes, 0, bytes.Length, _cts.Token);
                    string s = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                    int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
                    ulong msglen = (ulong)bytes[1] & 0b01111111;
                    ulong offset = 2;

                    if (fin)
                    {
                        if (opcode == 0x8) // Close
                        {
                            break;
                        }
                        else if (opcode == 0x1) // Text
                        {
                            if (msglen == 126)
                            {
                                // bytes are reversed because websocket will print them in Big-Endian, whereas
                                // BitConverter will want them arranged in little-endian on windows
                                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                                offset = 4;
                            }
                            else if (msglen == 127)
                            {
                                // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                                // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                                // websocket frame available through client.Available).
                                msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                                offset = 10;
                            }

                            if (msglen == 0)
                            {
                                Plugin.Log.Info("WebSocketServer: msglen == 0");
                            }
                            else if (!mask)
                            {
                                Plugin.Log.Info("WebSocketServer: Mask bit not set. Closing connection.");
                                break;
                            }
                            else
                            {
                                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                var bytesRemaining = msglen + offset - (ulong)bytesRead;
                                var allBytes = bytes;

                                if (bytesRemaining > 0)
                                {
                                    using (var bufferStream = new MemoryStream())
                                    {
                                        bufferStream.Write(bytes, 0, bytesRead);
                                        await ReadAsync(stream, client.ReceiveBufferSize, bufferStream, bytesRemaining, _cts.Token);
                                        allBytes = bufferStream.ToArray();
                                    }
                                }

                                byte[] decoded = new byte[msglen];
                                for (ulong i = 0; i < msglen; ++i)
                                    decoded[i] = (byte)(allBytes[offset + i] ^ masks[i % 4]);

                                string text = Encoding.UTF8.GetString(decoded);
                                HandleMessage(text);
                            }
                        }
                        else
                        {
                            Plugin.Log.Info($"WebSocketServer: Unsupported opcode: {opcode}. Closing connection.");
                            break;
                        }
                    }
                    else
                    {
                        Plugin.Log.Info($"WebSocketServer: Only fin frames are supported. Closing connection.");
                        break;
                    }
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _cts.Token)
            {
                // No-op
            }
            finally
            {
                if (webSocketClient != null)
                {
                    _clients.Remove(webSocketClient);
                    client.Close();
                }
            }
        }


        private static async Task ReadAsync(
            NetworkStream inputStream,
            int chunkSize,
            MemoryStream outputStream,
            ulong bytesToRead,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[chunkSize];
            while (bytesToRead > 0)
            {
                int bytesToReadInt = bytesToRead > int.MaxValue ? int.MaxValue : (int)bytesToRead;

                var bytesRead = await inputStream.ReadAsync(buffer, 0, Math.Min(bytesToReadInt, buffer.Length), cancellationToken);
                outputStream.Write(buffer, 0, bytesRead);
                bytesToRead -= (ulong)bytesRead;
            }
        }

        private void HandleMessage(string message)
        {
            var commandArray = JArray.Parse(message);
            if (commandArray != null)
            {
                var commandName = (string)commandArray[0];
                var commandArgs = (string)commandArray[1];
                if (commandName != null)
                {
                    var handled = false;
                    foreach (var handler in _handlers)
                    {
                        handled = handler(commandName, commandArgs);
                        if (handled)
                            break;
                    }

                    if (!handled)
                    {
                        Plugin.Log.Info("WebSocketServer: Unknown command: " + commandName);
                    }
                }
            }
        }

        private static async Task Cancelable(Task task, CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);
            var result = await Task.WhenAny(task, cancellationTask);
            if (result == task)
            {
                await task;
            }
            else
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        private static async Task<T> Cancelable<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);
            var result = await Task.WhenAny(task, cancellationTask);
            if (result == task)
            {
                return await task;
            }
            else
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        private class WebSocketClient
        {
            private readonly NetworkStream _stream;
            private readonly AwaitingQueue _awaitingQueue;

            public WebSocketClient(NetworkStream stream)
            {
                _stream = stream;
                _awaitingQueue = new AwaitingQueue();
            }

            public Task SendMessage(string message, CancellationToken cancellationToken)
            {
                return _awaitingQueue.RunOrQueue(async () =>
                {
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    using (var output = new MemoryStream())
                    {
                        void WriteMessageLength()
                        {
                            if (messageBytes.Length < 126)
                            {
                                output.WriteByte((byte)messageBytes.Length);
                            }
                            else if (messageBytes.Length < 65536)
                            {
                                output.WriteByte(126);
                                output.WriteByte((byte)((messageBytes.Length >> 8) & 0xFF)); // High byte
                                output.WriteByte((byte)(messageBytes.Length & 0xFF));
                            }
                            else
                            {
                                output.WriteByte(127);

                                // 64-bit length, big-endian
                                ulong messageLength = (ulong)messageBytes.Length;
                                for (int i = 7; i >= 0; i--)
                                {
                                    output.WriteByte((byte)(messageLength >> (8 * i) & 0xFF));
                                }
                            }
                        }

                        output.WriteByte(0x81); // denotes this is the final message and it is in text format
                        WriteMessageLength();
                        output.Write(messageBytes, 0, messageBytes.Length);

                        var outputBytes = output.ToArray();
                        await Cancelable(_stream.WriteAsync(outputBytes, 0, outputBytes.Length), cancellationToken);
                        //Plugin.Log.Info($"WebSocketServer: Sent {messageBytes.Length} message bytes");
                    }
                });
            }
        }
        
        private class HttpRequest
        {
            public readonly string RequestLine;
            public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
            public Uri Origin
            {
                get
                {
                    if (Headers.TryGetValue("origin", out var origin))
                    {
                        return new Uri(origin);
                    }

                    return null;
                }
            }

            public HttpRequest(string request)
            {
                var endOfHeadersIndex = request.IndexOf("\r\n\r\n");
                var requestWithoutBody = endOfHeadersIndex == -1 ? request : request.Substring(0, endOfHeadersIndex);
                var requestLines = requestWithoutBody.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (requestLines.Length > 0)
                {
                    RequestLine = requestLines[0];
                    for (var i = 1; i < requestLines.Length; ++i)
                    {
                        var parts = requestLines[i].Split(new char[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            Headers.Add(parts[0].ToLower().Trim(), parts[1].Trim());
                        }
                    }
                }
            }
        }
    }
}
