using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Zenject;

namespace InfiniteBeatSaber.DebugTools
{
    internal class Eval : IInitializable, IDisposable
    {
        private const string evalClassName = "InfiniteBeatSaber.DebugTools.EvalProgram";
        private const string evalMethodName = "EvalMain";

        // Gives the caller the opportunity to preserve state across evals.
        private readonly IDictionary<string, object> _state = new Dictionary<string, object>();

        public async void EvalDll(string dllPath)
        {
            try
            {
                var assemblyBytes = File.ReadAllBytes(dllPath);
                var pdbBytes = LoadPdbIfExists(dllPath); // Enables us to see line numbers in exceptions
                var assembly = Assembly.Load(assemblyBytes, pdbBytes);

                var type = assembly.GetType(evalClassName);
                if (type == null)
                {
                    Plugin.Log.Info("EvalDll error: Could not find class " + evalClassName + " in DLL.");
                    return;
                }

                var method = type.GetMethod(evalMethodName);
                if (method == null)
                {
                    Plugin.Log.Info("EvalDll error: Could not find method " + evalMethodName + " in class " + evalClassName + " in DLL.");
                    return;
                }

                var returnValue = method.Invoke(null, new object[] { _state });
                if (returnValue is Task task)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Info("EvalDll exception: " + ex.ToString());
                if (ex.InnerException != null)
                {
                    Plugin.Log.Info("EvalDll inner exception: " + ex.InnerException.ToString());
                }
            }
        }

        private byte[] LoadPdbIfExists(string dllPath)
        {
            var pdbPath = Path.ChangeExtension(dllPath, "pdb");
            return !File.Exists(pdbPath) ? null :
                File.ReadAllBytes(pdbPath);
        }

        #region Initialization & cleanup

        private readonly WebSocketServer _webSocketServer;
        private Action _unregisterWebSocket;

        [Inject]
        public Eval(WebSocketServer webSocketServer)
        {
            Util.AssertDebugBuild();

            _webSocketServer = webSocketServer;
        }

        public void Initialize()
        {
            _unregisterWebSocket = _webSocketServer.Register(OnWebSocketMessage);
        }

        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            _unregisterWebSocket();
        }

        #endregion

        #region WebSocket message handler

        private bool OnWebSocketMessage(string cmdName, string cmdArgs)
        {
            switch (cmdName)
            {
                case "evalDll":
                    {
                        var args = JsonConvert.DeserializeObject<EvalArgs>(cmdArgs);
                        EvalDll(args.DllPath);
                        return true;
                    }
                default:
                    return false;
            }
        }

        private class EvalArgs
        {
            [JsonProperty]
            public string DllPath { get; set; }
        }

        #endregion
    }
}
