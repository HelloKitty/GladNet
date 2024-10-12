using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	public interface IWebSocketConnection : IDisposable
	{
		/// <summary>
		/// Closes the WebSocket connection as an asynchronous operation using the close handshake defined in the WebSocket protocol specification section 7.
		/// </summary>
		/// <param name="closeStatus">Indicates the reason for closing the WebSocket connection.</param>
		/// <param name="statusDescription">Specifies a human readable explanation as to why the connection is closed.</param>
		/// <param name="cancellationToken">The token that can be used to propagate notification that operations should be canceled.</param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default);

		/// <summary>
		/// Returns the current state of the WebSocket connection.
		/// </summary>
		WebSocketState State { get; }

		/// <summary>
		/// Indicates the reason why the remote endpoint initiated the close handshake.
		/// If the close handshake has not been initiated yet, WebSocketCloseStatus.None is returned.
		/// </summary>
		WebSocketCloseStatus? CloseStatus { get; }

		/// <summary>
		/// Connects to a WebSocket server as an asynchronous operation.
		/// WARNING: This only works for clients.
		/// </summary>
		/// <param name="uri">The URI of the WebSocket server to connect to.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that the operation should be canceled.</param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		/// <exception cref="NotSupportedException">Thrown if the socket isn't for clients.</exception>
		Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);

		/// <summary>
		/// Reads the <see cref="count"/> many bytes into the provider <see cref="buffer"/> starting at index 0.
		/// Doesn't match the .NET API, is abit simplier.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="count">The amount of bytes to read.</param>
		/// <param name="token">Cancel token.</param>
		/// <returns>Awaitable</returns>
		Task ReceiveAsync(byte[] buffer, int count, CancellationToken token = default);

		/// <summary>
		/// Sends data on ClientWebSocket as an asynchronous operation.
		/// </summary>
		/// <param name="buffer">The buffer containing the message to be sent.</param>
		/// <param name="endMessage">true to indicate this is the final asynchronous send; otherwise, false.</param>
		/// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>The task object representing the asynchronous operation.</returns>
		Task SendAsync(ArraySegment<byte> buffer, bool endMessage, CancellationToken token = default);
	}
}
