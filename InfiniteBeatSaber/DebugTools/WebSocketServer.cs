using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InfiniteBeatSaber.Extensions;
using MessageHandler = System.Func<string, string, bool>;
using Zenject;

namespace InfiniteBeatSaber.DebugTools
{
    internal class WebSocketServer : IInitializable, IDisposable
    {
        public void Initialize()
        {
            StartServerLoop().LogOnFailure();
        }

        private bool _disposed;
        
        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            _disposed = true;
            _server?.Stop();
        }

        private static async Task SendMessage(NetworkStream stream, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] finalMessage = new byte[messageBytes.Length + 2];
            finalMessage[0] = 0x81; // denotes this is the final message and it is in text format
            finalMessage[1] = (byte)messageBytes.Length; // payload length
            Array.Copy(messageBytes, 0, finalMessage, 2, messageBytes.Length);
            await stream.WriteAsync(finalMessage, 0, finalMessage.Length);
        }

        private Task HandleMessage(NetworkStream stream, string message)
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
                        Plugin.Log.Info("Unknown command: " + commandName);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private TcpListener _server;
        private readonly List<NetworkStream> _clientStreams = new List<NetworkStream>();

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            _clientStreams.Add(stream);

            byte[] bytes = new byte[client.ReceiveBufferSize];

            while (!_disposed)
            {
                int bytesRead = await stream.ReadAsync(bytes, 0, bytes.Length);
                string s = Encoding.UTF8.GetString(bytes, 0, bytesRead);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    await stream.WriteAsync(response, 0, response.Length);
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        offset = 2;
                    ulong msglen = (ulong)bytes[1] & 0b01111111;

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
                        //Console.WriteLine("msglen == 0");
                    }
                    else if (mask)
                    {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (ulong i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[(ulong)offset + i] ^ masks[i % 4]);

                        string text = Encoding.UTF8.GetString(decoded);
                        await HandleMessage(stream, text);
                    }
                    else
                    {
                        //Console.WriteLine("mask bit not set");
                    }
                }
            }
        }

        public async Task SendMessage(string cmd, object args)
        {
            var message = new JArray(cmd, JsonConvert.SerializeObject(args)).ToString();
            var streams = new List<NetworkStream>(_clientStreams);
            foreach (var stream in streams)
            {
                await SendMessage(stream, message);
            }
        }

        public async Task SendMessage(string message)
        {
            var streams = new List<NetworkStream>(_clientStreams);
            foreach (var stream in streams)
            {
                try
                {
                    await SendMessage(stream, message);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Info("WebSocketServer: Error writing to client: " + ex.ToString());
                    _clientStreams.Remove(stream);
                }
            }
        }

        public async Task StartServerLoop()
        {
            string ip = "127.0.0.1";
            int port = 2019;
            _server = new TcpListener(IPAddress.Parse(ip), port);

            _server.Start();
            Console.WriteLine($"WebSocketServer has started on {ip}:{port}, Waiting for a connection...");

            try
            {
                while (!_disposed)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    var clientTask = HandleClient(client);
                }
            }
            catch (SocketException)
            {
                if (!_disposed)
                {
                    throw;
                }
            }
        }

        private readonly List<MessageHandler> _handlers = new List<MessageHandler>();
        public Action Register(MessageHandler handleMessage)
        {
            _handlers.Add(handleMessage);

            return () =>
            {
                _handlers.Remove(handleMessage);
            };
        }
    }
}
