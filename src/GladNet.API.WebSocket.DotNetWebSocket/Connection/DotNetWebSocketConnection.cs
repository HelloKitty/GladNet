using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace GladNet
{
	// See: https://github.com/dotnet/corefx/blob/d6b11250b5113664dd3701c25bdf9addfacae9cc/src/Common/src/System/Net/WebSockets/ManagedWebSocket.cs#L22-L28
	// for threadsafety restrictions
	// - It's acceptable to call ReceiveAsync and SendAsync in parallel.  One of each may run concurrently.
	// - It's acceptable to have a pending ReceiveAsync while CloseOutputAsync or CloseAsync is called.
	// - Attemping to invoke any other operations in parallel may corrupt the instance.  Attempting to invoke
	//   a send operation while another is in progress or a receive operation while another is in progress will
	//  result in an exception.
	/// <summary>
	/// DotNet <see cref="WebSocket"/> implementation of the <see cref="IWebSocketConnection"/>.
	/// </summary>
	public sealed class DotNetWebSocketConnection : IWebSocketConnection
	{
		/// <summary>
		/// The adapted dotnet socket.
		/// </summary>
		private WebSocket Connection { get; }

		/// <inheritdoc />
		public WebSocketState State => Connection.State;

		/// <inheritdoc />
		public WebSocketCloseStatus? CloseStatus => Connection.CloseStatus;

		private AsyncLock SyncObj { get; } = new();

		private bool IsCloseRequested = false;

		/// <summary>
		/// Creates a new <see cref="DotNetWebSocketConnection"/> that implements <see cref="IWebSocketConnection"/>
		/// adapting the provided <see cref="WebSocket"/> connection.
		/// </summary>
		/// <param name="connection"></param>
		public DotNetWebSocketConnection(WebSocket connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		/// <inheritdoc />
		public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default)
		{
			// Because Close and Send aren't threadsafe at the same time we must lock
			using (await SyncObj.LockAsync())
			{
				// Sometimes we hung on Aborted it seemed like.
				if (IsCloseRequested || Connection.State == WebSocketState.Aborted)
					return;

				IsCloseRequested = true;
				await Connection.CloseAsync(closeStatus, statusDescription, cancellationToken);
			}
		}

		/// <inheritdoc />
		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
		{
			if (Connection is ClientWebSocket cws)
			{
				return cws.ConnectAsync(uri, CancellationToken.None);
			}
			else
				throw new NotSupportedException($"It is not supported to call {nameof(ConnectAsync)} on a non-client websocket.");
		}

		// WARNING: These should not be called at the same time RecieveAnyAsync is called
		/// <inheritdoc />
		public async Task ReceiveAsync(byte[] buffer, int count, CancellationToken token = default)
		{
			ArraySegment<byte> bufferSegment = new ArraySegment<byte>(buffer, 0, count);
			await ReceiveAsyncInternal(bufferSegment, count, token);
		}

		private async Task ReceiveAsyncInternal(ArraySegment<byte> bufferSegment, int count, CancellationToken token)
		{
			int totalBytesRead = 0;
			do
			{
				WebSocketReceiveResult result
					= await Connection.ReceiveAsync(bufferSegment, token);

				// No longer computable from the offset because we might BE at offset 2 starting and we read 1 byte, that would have
				// been THREE but that's wrong.
				totalBytesRead += result.Count;

				// Read the buffer, don't rely on it being EndOfMessage. We might have the payload as apart of the same message
				if (totalBytesRead
				    == count)
					break;
				else if (totalBytesRead > count)
					throw new InvalidOperationException($"Read more bytes than request. Read: {totalBytesRead} Expected: {count}.");

				// Move the segment forward
				if (result.Count > bufferSegment.Count)
					throw new InvalidOperationException($"The WebSocket read more data ({result.Count}) than available in the buffer ({bufferSegment.Count}).");

				// Move the segment forward
				bufferSegment = new ArraySegment<byte>(bufferSegment.Array, bufferSegment.Offset + result.Count, bufferSegment.Count - result.Count);

			} while (!IsCloseRequested
			         && !token.IsCancellationRequested
			         && Connection.State == WebSocketState.Open);
		}

		/// <inheritdoc />
		public async Task ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
		{
			ArraySegment<byte> bufferSegment = new ArraySegment<byte>(buffer, offset, count);
			await ReceiveAsyncInternal(bufferSegment, count, token);
		}

		/// <inheritdoc />
		public async Task<int> ReceiveAnyAsync(byte[] buffer, CancellationToken token = default)
		{
			ArraySegment<byte> bufferSegment = new ArraySegment<byte>(buffer, 0, buffer.Length);

			WebSocketReceiveResult result
				= await Connection.ReceiveAsync(bufferSegment, token);

			var totalBytesRead = result.Count;

			if(totalBytesRead == 0)
				return 0;

			if(totalBytesRead > buffer.Length)
				throw new InvalidOperationException($"Read more bytes than request. Read: {totalBytesRead} Expected less than or equal to: {buffer.Length}.");

			return totalBytesRead;
		}

		/// <inheritdoc />
		public async Task SendAsync(ArraySegment<byte> buffer, bool endMessage, CancellationToken token = default)
		{
			if (IsCloseRequested)
				return;

			// Because Close and Send aren't threadsafe at the same time we must lock
			using(await SyncObj.LockAsync())
				await Connection.SendAsync(buffer, WebSocketMessageType.Binary, endMessage, token);
		}

		public void Dispose()
		{
			Connection.Dispose();
		}
	}
}
