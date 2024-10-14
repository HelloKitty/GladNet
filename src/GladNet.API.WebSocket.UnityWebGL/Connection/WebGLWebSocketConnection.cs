using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// WebGL compatible implementation of GladNet's <see cref="IWebSocketConnection"/>.
	/// </summary>
	public sealed class WebGLWebSocketConnection : IWebSocketConnection, IUnityWebGLWebSocket
	{
		/* WebSocket JSLIB functions */
		[DllImport("__Internal")]
		public static extern int WebSocketConnect(int instanceId);

		[DllImport("__Internal")]
		public static extern int WebSocketClose(int instanceId, int code, string reason);

		[DllImport("__Internal")]
		public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

		[DllImport("__Internal")]
		public static extern int WebSocketSendText(int instanceId, string message);

		[DllImport("__Internal")]
		public static extern int WebSocketGetState(int instanceId);

		/// <summary>
		/// Triggered when the WebSocket connection is successfully opened.
		/// From <see cref="IUnityWebGLWebSocket"/> designed by https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs
		/// </summary>
		public event WebSocketOpenEventHandler OnOpen;

		/// <summary>
		/// Triggered when a message is received over the WebSocket connection.
		/// From <see cref="IUnityWebGLWebSocket"/> designed by https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs
		/// </summary>
		public event WebSocketMessageEventHandler OnMessage;

		/// <summary>
		/// Triggered when an error occurs in the WebSocket connection.
		/// From <see cref="IUnityWebGLWebSocket"/> designed by https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs
		/// </summary>
		public event WebSocketErrorEventHandler OnError;

		/// <summary>
		/// Triggered when the WebSocket connection is closed.
		/// From <see cref="IUnityWebGLWebSocket"/> designed by https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs
		/// </summary>
		public event WebSocketCloseEventHandler OnClose;

		/// <inheritdoc />
		public WebSocketState State => GetState();

		// TODO: Can we even implement this??
		/// <inheritdoc />
		public WebSocketCloseStatus? CloseStatus { get; } = null;

		/// <summary>
		/// More efficient approximation of if the socket is open.
		/// </summary>
		bool IsAssumedOpen { get; set; } = false;

		private int InstanceId { get; set; } = -1;

		private ArraySegment<byte> PendingReadBuffer { get; set; }

		private TaskCompletionSource<object> ReadNotifyTask { get; set; }

		private List<ArraySegment<byte>> ByteMessageQueue { get; } = new();

		/// <summary>
		/// Initializes a new instance of <see cref="WebGLWebSocketConnection"/> and sets up message queue handling.
		/// </summary>
		public WebGLWebSocketConnection()
		{
			// Setup the message queue stuff
			OnMessage += OnByteMessage;
		}

		/// <summary>
		/// Handles the event when a message is received, adding the message data to the byte message queue.
		/// </summary>
		/// <param name="data">The message data received.</param>
		private void OnByteMessage(byte[] data)
		{
			ByteMessageQueue.Add(data);

			//UnityEngine.Debug.LogError($"MessageQueueSize: {ByteMessageQueue.Count}");

			ProcessMessageQueue();
		}

		/// <summary>
		/// Processes the byte message queue and notifies the reader if all data has been processed.
		/// </summary>
		private void ProcessMessageQueue()
		{
			// WARNING: AI once chnaged this watch out!
			var pendingReadBuffer = PendingReadBuffer;
			if (PendingReadBuffer == null
			    || ReadNotifyTask == null)
				return;

			try
			{
				ProcessPendingBytes(ref pendingReadBuffer);
				PendingReadBuffer = pendingReadBuffer;

				// Notify the reader we have bytes fully now
				if (PendingReadBuffer.Count == 0)
				{
					PendingReadBuffer = null;
					var task = ReadNotifyTask;
					ReadNotifyTask = null;
					task?.SetResult(null);
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError($"Processing Bytes Failed: {e}");
			}
		}

		/// <summary>
		/// Processes the pending bytes in the byte message queue and fills the provided buffer.
		/// </summary>
		private void ProcessPendingBytes(ref ArraySegment<byte> buffer)
		{
			try
			{
				for(int i = 0; i < ByteMessageQueue.Count; i++)
				{
					if(buffer.Count == 0)
						return;

					// We have some data to read, not more than exists.
					if(ByteMessageQueue[i].Count <= buffer.Count)
					{
						// Copy, we got enough thankfully
						Buffer.BlockCopy(ByteMessageQueue[i].Array, ByteMessageQueue[i].Offset, buffer.Array, buffer.Offset, ByteMessageQueue[i].Count);

						// Update segment, it's empty now. We read everything.
						buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + ByteMessageQueue[i].Count, buffer.Count - ByteMessageQueue[i].Count);
						ByteMessageQueue[i] = new ArraySegment<byte>(Array.Empty<byte>());
					}
					else
					{
						// The buffer in this index is BIGGER than what we need
						Buffer.BlockCopy(ByteMessageQueue[i].Array, ByteMessageQueue[i].Offset, buffer.Array, buffer.Offset, buffer.Count);

						ByteMessageQueue[i] = new ArraySegment<byte>(ByteMessageQueue[i].Array, ByteMessageQueue[i].Offset + buffer.Count, ByteMessageQueue[i].Count - buffer.Count);
						buffer = new ArraySegment<byte>(Array.Empty<byte>());
					}
				}
			}
			finally
			{
				ByteMessageQueue.RemoveAll(bytes => bytes.Count == 0);
			}
		}

		/// <inheritdoc />
		public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default)
		{
			int resultCode = WebSocketClose(InstanceId, (int)closeStatus, statusDescription);
			ResetConnection();

			if(resultCode < 0)
				throw UnityWebGLWebSocketHelpers.GetErrorMessageFromCode(resultCode, null);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when the client closes.
		/// Resets the WebSocket connection and releases any pending read operations.
		/// </summary>
		private void ResetConnection()
		{
			PendingReadBuffer = null;
			IsAssumedOpen = false;
			ReadNotifyTask?.SetException(new OperationCanceledException($"WebGL socket connect reset/disposed."));
			ReadNotifyTask = null;
		}

		/// <inheritdoc />
		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
		{
			UnityWebGLWebSocketFactory.Initialize();
			InstanceId = UnityWebGLWebSocketFactory.CreateSocket(this, uri);

			int resultCode = WebSocketConnect(InstanceId);

			if(resultCode < 0)
				throw UnityWebGLWebSocketHelpers.GetErrorMessageFromCode(resultCode, null);

			IsAssumedOpen = true;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task ReceiveAsync(byte[] buffer, int count, CancellationToken token = default)
		{
			await ReceiveAsync(buffer, 0, count, token);
		}

		/// <inheritdoc />
		public async Task ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
		{
			if(!IsAssumedOpen)
				throw new InvalidOperationException($"WebGL socket was no longer open during read.");

			//UnityEngine.Debug.LogError($"About to process bytes in ReceiveAsync");

			if(TryProcessReceiveNonAsync(buffer, offset, count))
			{
				//UnityEngine.Debug.LogError($"Enough bytes read, returning to gladnet internal.");
				return;
			}

			//UnityEngine.Debug.LogError($"Not enough bytes available, awaiting notify task");

			// This will complete once the data is ready, it wasn't read above in the non-async call.
			await ReadNotifyTask.Task;
			PendingReadBuffer = null;
			ReadNotifyTask = null;
		}

		/// <inheritdoc />
		public async Task<int> ReceiveAnyAsync(byte[] buffer, CancellationToken token = default)
		{
			throw new NotSupportedException($"{nameof(ReceiveAnyAsync)} is not supported in WebGL");
		}

		/// <summary>
		/// Attempts to process the received data without awaiting an asynchronous task.
		/// </summary>
		private bool TryProcessReceiveNonAsync(byte[] buffer, int offset, int count)
		{
			// TODO: We might want to process existing stuff in the future, since this awaits we'll only read afew packets per frame
			PendingReadBuffer = new ArraySegment<byte>(buffer, offset, count);

			var pendingReadBuffer = PendingReadBuffer;
			ProcessPendingBytes(ref pendingReadBuffer);
			PendingReadBuffer = pendingReadBuffer;

			// If the buffer was filled then we're good to go!
			if(pendingReadBuffer.Count == 0)
			{
				PendingReadBuffer = null;
				ReadNotifyTask = null;
				return true;
			}

			// TODO: Support cancel token.
			ReadNotifyTask = new TaskCompletionSource<object>();
			return false;
		}

		/// <inheritdoc />
		public Task SendAsync(ArraySegment<byte> buffer, bool endMessage, CancellationToken token = default)
		{
			// TODO: Wtf, no support for "EndMessage"???
			if(buffer.Offset != 0)
				throw new NotSupportedException($"Cannot support sending WebGL socket bytes with segment isn't starting at zero-index.");

			int result = WebSocketSend(InstanceId, buffer.Array, buffer.Count);

			if(result < 0)
				throw UnityWebGLWebSocketHelpers.GetErrorMessageFromCode(result, null);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets the <see cref="WebSocketState"/> from the JS implementation.
		/// Retrieves the current WebSocket connection state.
		/// </summary>
		/// <returns></returns>
		private WebSocketState GetState()
		{
			// Some things are checking state right away so we better not call into JS
			if(InstanceId < 0 || !IsAssumedOpen)
				return WebSocketState.None;

			// See: https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs#L320
			int state = WebSocketGetState(InstanceId);

			if(state < 0)
				throw UnityWebGLWebSocketHelpers.GetErrorMessageFromCode(state, null);

			switch(state)
			{
				case 0:
					return WebSocketState.Connecting;

				case 1:
					return WebSocketState.Open;

				case 2:
					return WebSocketState.CloseSent;

				case 3:
					return WebSocketState.Closed;

				default:
					return WebSocketState.Closed;
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			UnityWebGLWebSocketFactory.HandleInstanceDestroy(InstanceId);
			ResetConnection();
		}

		public void DelegateOnOpenEvent()
		{
			this.OnOpen?.Invoke();
		}

		public void DelegateOnMessageEvent(byte[] data)
		{
			this.OnMessage?.Invoke(data);
		}

		public void DelegateOnErrorEvent(string errorMsg)
		{
			this.OnError?.Invoke(errorMsg);
		}

		public void DelegateOnCloseEvent(int closeCode)
		{
			this.OnClose?.Invoke(UnityWebGLWebSocketHelpers.ParseCloseCodeEnum(closeCode));
		}
	}
}