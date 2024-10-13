using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using JetBrains.Annotations;

namespace GladNet
{
	/// <summary>
	/// Class providing static access methods to work with JSLIB WebSocket or WebSocketSharp interface
	/// </summary>
	public static class UnityWebGLWebSocketFactory
	{
		public static Dictionary<Int32, WebGLWebSocketConnection> Instances { get; } = new();

		/* Delegates */
		public delegate void OnOpenCallback(int instanceId);

		public delegate void OnMessageCallback(int instanceId, System.IntPtr msgPtr, int msgSize);

		public delegate void OnErrorCallback(int instanceId, System.IntPtr errorPtr);

		public delegate void OnCloseCallback(int instanceId, int closeCode);

		/* WebSocket JSLIB callback setters and other functions */
		[DllImport("__Internal")]
		public static extern int WebSocketAllocate(string url);

		[DllImport("__Internal")]
		public static extern int WebSocketAddSubProtocol(int instanceId, string subprotocol);

		[DllImport("__Internal")]
		public static extern void WebSocketFree(int instanceId);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnOpen(OnOpenCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnMessage(OnMessageCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnError(OnErrorCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnClose(OnCloseCallback callback);

		/// <summary>
		/// If callbacks was initialized and set
		/// </summary>
		public static bool IsInitialized { get; private set; } = false;

		/// <summary>
		/// Initialize WebSocket callbacks to JSLIB
		/// </summary>
		public static void Initialize()
		{
			if(IsInitialized)
				return;

			WebSocketSetOnOpen(DelegateOnOpenEvent);
			WebSocketSetOnMessage(DelegateOnMessageEvent);
			WebSocketSetOnError(DelegateOnErrorEvent);
			WebSocketSetOnClose(DelegateOnCloseEvent);

			IsInitialized = true;
		}

		/// <summary>
		/// Called when instance is destroyed (by destructor)
		/// Method removes instance from map and free it in JSLIB implementation
		/// </summary>
		/// <param name="instanceId">Instance identifier.</param>
		public static void HandleInstanceDestroy(int instanceId)
		{
			try
			{
				Instances.Remove(instanceId);
			}
			finally
			{
				WebSocketFree(instanceId);
			}
		}

		[MonoPInvokeCallback(typeof(OnOpenCallback))]
		public static void DelegateOnOpenEvent(int instanceId)
		{
			if(Instances.TryGetValue(instanceId, out var instanceRef))
				instanceRef.DelegateOnOpenEvent();
		}

		[MonoPInvokeCallback(typeof(OnMessageCallback))]
		public static void DelegateOnMessageEvent(int instanceId, System.IntPtr msgPtr, int msgSize)
		{
			if(Instances.TryGetValue(instanceId, out var instanceRef))
			{
				// TODO: We should avoid allocating here and use a shared buffer instead maybe or something
				byte[] msg = new byte[msgSize];
				Marshal.Copy(msgPtr, msg, 0, msgSize);

				instanceRef.DelegateOnMessageEvent(msg);
			}
		}

		[MonoPInvokeCallback(typeof(OnErrorCallback))]
		public static void DelegateOnErrorEvent(int instanceId, System.IntPtr errorPtr)
		{
			if(Instances.TryGetValue(instanceId, out var instanceRef))
			{
				string errorMsg = Marshal.PtrToStringAuto(errorPtr);
				instanceRef.DelegateOnErrorEvent(errorMsg);
			}
		}

		[MonoPInvokeCallback(typeof(OnCloseCallback))]
		public static void DelegateOnCloseEvent(int instanceId, int closeCode)
		{
			if(Instances.TryGetValue(instanceId, out var instanceRef))
				instanceRef.DelegateOnCloseEvent(closeCode);
		}

		public static int CreateSocket([NotNull] WebGLWebSocketConnection connection, [NotNull] Uri uri)
		{
			if(connection == null) throw new ArgumentNullException(nameof(connection));
			if(uri == null) throw new ArgumentNullException(nameof(uri));

			var id = WebSocketAllocate(uri.ToString());
			Instances.Add(id, connection);

			return id;
		}
	}
}