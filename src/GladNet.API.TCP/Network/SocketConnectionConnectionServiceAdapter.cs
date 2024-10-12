using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	/// <summary>
	/// Implementation of <see cref="INetworkConnectionService"/> based around <see cref="SocketConnection"/>
	/// </summary>
	public sealed class SocketConnectionConnectionServiceAdapter : INetworkConnectionService
	{
		/// <summary>
		/// Internal socket connection.
		/// </summary>
		private SocketConnection Connection { get; }

		// This is because Connection.Socket.Connected may still be true even after Connection.Socket.ConnectAsync
		// down below finishes awaiting.
		/// <inheritdoc />
		public bool isConnected => Connection.Socket.Connected 
		                           && Connection.ShutdownKind == PipeShutdownKind.None;

		public SocketConnectionConnectionServiceAdapter(SocketConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		/// <inheritdoc />
		public async Task DisconnectAsync()
		{
			//This may look RIDICULOUS but this seem to be the only way for a server
			//to successfully disconnect a client without raising exceptions on the read/write thread
			TaskCompletionSource<object> source = new TaskCompletionSource<object>();
			Connection.Socket.BeginDisconnect(false, ar =>
			{
				Connection.Socket.EndDisconnect(ar);
				source.SetResult(null);
			}, null);

			//TODO: We don't have a mechanism to flush??
			Connection.TrySetProtocolShutdown(PipeShutdownKind.ProtocolExitServer);
			Connection.Input.CancelPendingRead();
			Connection.Output.CancelPendingFlush();

			await source.Task;
			return;
		}

		//TODO: This is kind of stupid to even implement. This should NEVER be called on serverside!
		/// <inheritdoc />
		public async Task<bool> ConnectAsync(string ip, int port)
		{
			if (Connection.Socket.Connected)
				return false;

			await Connection.Socket.ConnectAsync(ip, port);
			return Connection.Socket.Connected;
		}
	}
}
