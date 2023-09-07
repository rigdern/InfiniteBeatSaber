import { pp } from './util.js';

export default class BeatSaberClient {
  constructor(handlers) {
    this._socket = undefined;
    this._handlers = handlers;
  }

  _connect() {
    return new Promise((resolve, reject) => {
      this._socket = new WebSocket('ws://localhost:2019');
      window.ahc = x => this._socket.send(x);
  
      // Event handler for when the connection is established
      this._socket.onopen = (event) => {
        console.log('WebSocket connection established');
        resolve();
        this._handlers.onConnected && this._handlers.onConnected();
      };
  
      // Event handler for when a message is received from the server
      this._socket.onmessage = (event) => {
        const [cmd, argsJson] = JSON.parse(event.data);
        const args = JSON.parse(argsJson);

        // console.log('Message from server: ', pp([cmd, args]));
        this._handlers.onMessage && this._handlers.onMessage(cmd, args);
      };
  
      // Event handler for when the connection is closed
      this._socket.onclose = (event) => {
        console.log('WebSocket connection closed');
        this._onDisconnected();
      };
  
      // Event handler for any errors that occur
      this._socket.onerror = (error) => {
        console.log('WebSocket Error: ', error);
        this._onDisconnected();
      };
    });
  }

  _onDisconnected() {
    console.log('WebSocket disconnected');
    this._socket = undefined;
    this._handlers.onDisconnected && this._handlers.onDisconnected();
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
