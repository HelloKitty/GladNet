using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Event delegate for the closure of a Unity WebSocket connection.
	/// </summary>
	/// <param name="closeCode">The close code.</param>
	public delegate void WebSocketCloseEventHandler(WebSocketCloseStatus closeCode);
}
