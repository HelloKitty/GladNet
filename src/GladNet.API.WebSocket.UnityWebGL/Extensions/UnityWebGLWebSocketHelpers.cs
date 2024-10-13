using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// See: https://github.com/endel/NativeWebSocket/blob/master/NativeWebSocket/Assets/WebSocket/WebSocket.cs#L126
	/// </summary>
	public static class UnityWebGLWebSocketHelpers
	{
		public static Exception GetErrorMessageFromCode(int errorCode, Exception inner)
		{
			switch(errorCode)
			{
				case -1:
					return new WebSocketUnexpectedException("WebSocket instance not found.", inner);
				case -2:
					return new WebSocketInvalidStateException("WebSocket is already connected or in connecting state.", inner);
				case -3:
					return new WebSocketInvalidStateException("WebSocket is not connected.", inner);
				case -4:
					return new WebSocketInvalidStateException("WebSocket is already closing.", inner);
				case -5:
					return new WebSocketInvalidStateException("WebSocket is already closed.", inner);
				case -6:
					return new WebSocketInvalidStateException("WebSocket is not in open state.", inner);
				case -7:
					return new WebSocketInvalidArgumentException("Cannot close WebSocket. An invalid code was specified or reason is too long.", inner);
				default:
					return new WebSocketUnexpectedException("Unknown error.", inner);
			}
		}

		public class CustomWebSocketException : Exception
		{
			public CustomWebSocketException() { }
			public CustomWebSocketException(string message) : base(message) { }
			public CustomWebSocketException(string message, Exception inner) : base(message, inner) { }
		}

		public class WebSocketUnexpectedException : CustomWebSocketException
		{
			public WebSocketUnexpectedException() { }
			public WebSocketUnexpectedException(string message) : base(message) { }
			public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
		}

		public class WebSocketInvalidArgumentException : CustomWebSocketException
		{
			public WebSocketInvalidArgumentException() { }
			public WebSocketInvalidArgumentException(string message) : base(message) { }
			public WebSocketInvalidArgumentException(string message, Exception inner) : base(message, inner) { }
		}

		public class WebSocketInvalidStateException : CustomWebSocketException
		{
			public WebSocketInvalidStateException() { }
			public WebSocketInvalidStateException(string message) : base(message) { }
			public WebSocketInvalidStateException(string message, Exception inner) : base(message, inner) { }
		}

		public static WebSocketCloseStatus ParseCloseCodeEnum(int closeCode)
		{
			return (WebSocketCloseStatus)closeCode;
		}
	}
}
