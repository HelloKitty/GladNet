namespace GladNet
{
	/// <summary>
	/// Delegate to handle WebSocket message events.
	/// This delegate will be triggered when a message is received via the WebSocket connection.
	/// </summary>
	/// <param name="data">The message data received, represented as a byte array.</param>
	public delegate void WebSocketMessageEventHandler(byte[] data);
}