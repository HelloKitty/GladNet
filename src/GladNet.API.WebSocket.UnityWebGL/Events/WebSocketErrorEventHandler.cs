namespace GladNet
{
	/// <summary>
	/// Delegate to handle WebSocket error events.
	/// This delegate will be triggered when an error occurs on the WebSocket connection.
	/// </summary>
	/// <param name="errorMsg">The error message describing the issue encountered.</param>
	public delegate void WebSocketErrorEventHandler(string errorMsg);
}