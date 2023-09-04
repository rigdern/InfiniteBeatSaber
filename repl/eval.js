// Compiles `EvalProgram.cs` and sends it to the WebSocket server running in the
// mod for evaluation. Enables a REPL-like experience.
//
// TODO:
// - Remove dependency on node so it doesn't introduce any dependency into the
//   build process. Options:
//   - Port it to a language likely to be available on the platform like C# or
//     PowerShell.
//   - Move as much of the logic into the server as possible.
// - Display errors in Visual Studio's "Error List" pane. Double-clicking an
//   error should jump to the appropriate spot.
// - Return data so it can be explored in external tools such as a visualizer.
//   Think about Clojure's REBL.
// - What can be learned from existing REPLs? e.g. Clojure, Common Lisp.
// - Is this obsoleted by other tools like hot code reloading, C# immediate
//   pane, etc.? In any case, it's neat that this implementation is so simple. I
//   feel like I could wire it up to any C# program. Whereas if I'm having
//   trouble getting one of the other tools working with a program, I'm not sure
//   how I would go about trying to get it to work.
// - Instead of hardcoding the list of assemblies in the build command,
//   automatically figure it out based on project references.
// - Automatic pretty printing of C# objects. Especially ones that are intended
//   to be just data like PracticeSettings.

import child_process from 'child_process';
import WebSocket from 'ws';
import path, { dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const dllPath = path.join(__dirname, 'EvalProgram.dll');

function assert(pred, msg) {
  if (!pred) {
    throw new Error('Assertion failed: ' + msg);
  }
}

function pp(x) {
  return JSON.stringify(x, undefined, 2);
}

function fail(msg) {
  throw new Error('Fatal error: ' + msg);
}

function spawn(command, args, options=undefined) {
  const result = child_process.spawnSync(command, args, options);
  assert(
    !result.error,
    'spawn failed:\n' +
    '  error: ' + result.error + '\n' +
    '  cmd: ' + JSON.stringify([command, ...args])
  );
  assert(
    result.status === 0,
    'command failed:\n' +
    '  status: ' + result.status + '\n' +
    '  error: ' + result.stderr + '\n' +
    '  stdout: ' + result.stdout + '\n' +
    '  cmd: ' + JSON.stringify([command, ...args])
  );
  
  return result;
}

export default class BeatSaberClient {
  constructor(onMessage) {
    this._socket = undefined;
    this._onMessage = onMessage;
  }

  _connect() {
    return new Promise((resolve, reject) => {
      this._socket = new WebSocket('ws://127.0.0.1:2019');

      console.log('Connecting to WebSocket...');
      // Event handler for when the connection is established
      this._socket.on('open', (event) => {
        console.log('WebSocket connection established');
        resolve();
      });
  
      // Event handler for when a message is received from the server
      this._socket.on('message', (data) => {
        const [cmd, argsJson] = JSON.parse(data);
        const args = JSON.parse(argsJson);

        console.log('Message from server: ', pp([cmd, args]));
        this._onMessage && this._onMessage(cmd, args);
      });
  
      // Event handler for when the connection is closed
      this._socket.on('close', (event) => {
        console.log('WebSocket connection closed');
        this._onDisconnected();
      });
  
      // Event handler for any errors that occur
      this._socket.on('error', (error) => {
        console.log('WebSocket Error: ', error);
        this._onDisconnected();
      });
    });
  }

  _onDisconnected() {
    console.log('WebSocket disconnected');
    this._socket = undefined;
  }

  async ensureConnected() {
    if (!this._socket) {
      await this._connect();
    }
  }

  async send(cmd, args={}) {
    await this.ensureConnected();
    this._socket.send(JSON.stringify([
      cmd,
      JSON.stringify(args),
    ]));
  }
}

async function main(args) {
  console.log(
    spawn('C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\Roslyn\\csc.exe', [
      '/noconfig',
      '/nowarn:1701,1702,2008',
      '/fullpaths',
      '/nostdlib+',
      '/errorreport:prompt',
      '/warn:4',
      '/define:DEBUG;TRACE',
      '/errorendlocation',
      '/preferreduilang:en-US',
      '/highentropyva+',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Libs\\0Harmony.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\BeatmapCore.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Plugins\\BSML.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\GameplayCore.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\HMLib.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\HMUI.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\IPA.Loader.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Main.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\mscorlib.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Libs\\Newtonsoft.Json.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Polyglot.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Plugins\\SiraUtil.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Configuration.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Core.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Data.DataSetExtensions.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Data.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.IO.Compression.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Net.Http.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Numerics.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\Facades\\System.Reflection.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Runtime.Serialization.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Security.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Windows.Forms.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Xaml.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Xml.dll',
      '/reference:C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.7.2\\System.Xml.Linq.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Unity.TextMeshPro.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.AudioModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.CoreModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.UI.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.UIElementsModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.UIModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.UnityWebRequestAudioModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.UnityWebRequestModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\UnityEngine.VRModule.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Zenject-usage.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Zenject.dll',
      '/reference:C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Plugins\\InfiniteBeatSaber.dll',
      '/debug+',
      '/debug:portable',
      '/filealign:512',
      '/optimize-',
      '/out:EvalProgram.dll',
      '/subsystemversion:6.00',
      '/target:library',
      '/utf8output',
      '/langversion:7.3',
      '..\\InfiniteBeatSaber\\DebugTools\\EvalProgram.cs',
      '.NETFramework,Version=v4.7.2.AssemblyAttributes.cs',
    ]).stdout.toString('utf8').trim()
  );

  const beatSaberClient = new BeatSaberClient();
  await beatSaberClient.send('evalDll', { dllPath: dllPath });
}

process.exit(await main(process.argv.slice(2)));
