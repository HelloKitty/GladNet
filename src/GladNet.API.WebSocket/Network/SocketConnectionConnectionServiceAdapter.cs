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
	public sealed class SocketConnectionConnectionServiceAdapter : INetworkConnectionService
	{
		/// <summary>
		/// Internal socket connection.
		/// </summary>
		private IWebSocketConnection Connection { get; }

		/// <inheritdoc />
		public bool isConnected => (Connection.State == WebSocketState.Open || Connection.State == WebSocketState.Connecting)
		                           && !Connection.CloseStatus.HasValue;

		public SocketConnectionConnectionServiceAdapter(IWebSocketConnection connection)
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
			if (isConnected)
				return false;

			await Connection.ConnectAsync(new Uri(ip), CancellationToken.None);
			return Connection.State == WebSocketState.Open;
		}
	}
}
