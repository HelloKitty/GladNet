using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Implementation of <see cref="INetworkConnectionService"/> based around <see cref="WebSocket"/>
	/// </summary>
	public sealed class WebSocketConnectionConnectionServiceAdapter : INetworkConnectionService
	{
		/// <summary>
		/// Internal socket connection.
		/// </summary>
		private IWebSocketConnection Connection { get; }

		/// <inheritdoc />
		public bool IsConnected => (Connection.State == WebSocketState.Open || Connection.State == WebSocketState.Connecting)
		                           && !Connection.CloseStatus.HasValue;

		public WebSocketConnectionConnectionServiceAdapter(IWebSocketConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		/// <inheritdoc />
		public async Task DisconnectAsync()
		{
			await Connection.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, CancellationToken.None);
		}

		//TODO: This is kind of stupid to even implement. This should NEVER be called on serverside!
		/// <inheritdoc />
		public async Task<bool> ConnectAsync(string ip, int port)
		{
			if (IsConnected)
				return false;

			// Use UriBuilder to modify the URI
			UriBuilder uriBuilder = new UriBuilder(ip);

			// Check if the port is already present in the URI
			// -1 means no port was specified
			if (uriBuilder.Port <= 0
				|| port > 0 && uriBuilder.Port != port) 
				uriBuilder.Port = port; 

			Uri finalUri = uriBuilder.Uri;

			await Connection.ConnectAsync(finalUri, CancellationToken.None);
			return Connection.State == WebSocketState.Open;
		}
	}
}
