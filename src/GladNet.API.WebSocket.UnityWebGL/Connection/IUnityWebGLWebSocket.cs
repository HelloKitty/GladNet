using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Interface representing a WebSocket implementation for Unity WebGL.
	/// Based on GaiaOnline.Towns3 WebSocket implementation.
	/// </summary>
	public interface IUnityWebGLWebSocket
	{
		/// <summary>
		/// Event triggered when the WebSocket connection is successfully opened.
		/// </summary>
		event WebSocketOpenEventHandler OnOpen;

		/// <summary>
		/// Event triggered when a message is received from the WebSocket.
		/// </summary>
		event WebSocketMessageEventHandler OnMessage;

		/// <summary>
		/// Event triggered when an error occurs in the WebSocket connection.
		/// </summary>
		event WebSocketErrorEventHandler OnError;

		/// <summary>
		/// Event triggered when the WebSocket connection is closed.
		/// </summary>
		event WebSocketCloseEventHandler OnClose;

		/// <summary>
		/// The current state of the WebSocket connection.
		/// </summary>
		WebSocketState State { get; }
	}

}
