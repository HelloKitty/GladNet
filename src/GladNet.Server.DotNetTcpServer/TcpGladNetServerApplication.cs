using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using Glader.Essentials;
using GladNet;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	/// <summary>
	/// Base application base for a high performance async TCP server application.
	/// Builds, manages and maintains <see cref="ManagedSession"/>s internally
	/// when clients connect.
	/// </summary>
	public abstract class TcpGladNetServerApplication<TManagedSessionType> : GladNetServerApplication<TManagedSessionType, SessionCreationContext>
		where TManagedSessionType : ManagedSession
	{
		private int _lifetimeConnectionCount = 0;

		/// <summary>
		/// The number of connections that have been serviced
		/// lifetime by this application.
		/// </summary>
		public int LifetimeConnectionCount
		{
			get => _lifetimeConnectionCount;
			private set => _lifetimeConnectionCount = value;
		}

		/// <inheritdoc />
		protected TcpGladNetServerApplication(NetworkAddressInfo serverAddress, ILog logger)
			: base(serverAddress, logger)
		{

		}

		/// <summary>
		/// Indicates if the provided <see cref="Socket"/> is acceptable.
		/// Return true if the client should be handled. This will likely lead to
		/// a call to <see cref="Create()"/>. Returning false
		/// will mean this connection should be disconnected and rejected and no
		/// client representation will be created for it.
		/// </summary>
		/// <param name="connection">The <see cref="Socket"/> to check the acceptance for.</param>
		/// <returns></returns>
		protected abstract bool IsClientAcceptable(Socket connection);

		/// <inheritdoc />
		public override async Task BeginListeningAsync(CancellationToken token = default)
		{
			SocketConnection.AssertDependencies();

			if (Logger.IsInfoEnabled)
				Logger.Info($"Server begin listening.");

			using (Socket listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
			{
				listenSocket.Bind(new IPEndPoint(ServerAddress.AddressEndpoint, ServerAddress.Port));

				if (!listenSocket.IsBound)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Socket failed to bind to Port: {ServerAddress.Port} on Address: {ServerAddress.AddressEndpoint.ToString()}");

					return;
				}

				//TODO: This is the maximum unacceptaed in-queue connection attempts. I don't know if this should be configurable
				//See: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.listen?view=netframework-4.7.2
				listenSocket.Listen(1000);

				if(token.IsCancellationRequested)
					return;

				if(Logger.IsInfoEnabled)
					Logger.Info($"Server bound to Port: {ServerAddress.Port} on Address: {ServerAddress.AddressEndpoint.ToString()}");

				try
				{
					await SocketAcceptLoopAsync(token, listenSocket);
				}
				catch (Exception e)
				{
					if (Logger.IsErrorEnabled)
						Logger.Error($"Server encountered unhandled exception in socket accept loop. Error: {e}");

					throw;
				}
			}
		}

		private async Task SocketAcceptLoopAsync(CancellationToken token, Socket listenSocket)
		{
			if (listenSocket == null) throw new ArgumentNullException(nameof(listenSocket));

			while (!token.IsCancellationRequested)
			{
				//For some reason the REMOTE connection/socket closing before this
				//completes can cause a throw
				//Example: System.Net.Sockets.SocketException (10054): An existing connection was forcibly closed by the remote host.
				//Therefore we must wrap this in a try
				Socket socket = null;
				try
				{
					socket = await listenSocket.AcceptAsync();
				}
				catch(SocketException e)
				{
					socket?.Dispose();

					SocketError error = (SocketError) e.ErrorCode;
					switch (error)
					{
						case SocketError.ConnectionReset: // 10054
						case SocketError.ConnectionAborted:
							if(Logger.IsInfoEnabled)
								Logger.Info($"Socket disconnected before accept. This is expected and can occur if the remote client disconnects before fully accepted.");
							continue; //continue the loop on this expected exception.
						default:
							throw;
					}
				}

				//Try required because we're calling into user code.
				try
				{
					if (!IsClientAcceptable(socket))
					{
						socket.Shutdown(SocketShutdown.Both);
						socket.Dispose();
						continue;
					}
				}
				catch (Exception e)
				{
					if (Logger.IsInfoEnabled)
						Logger.Error($"Socket failed to creation. Exception in accept check. Reason: {e}");

					socket.Shutdown(SocketShutdown.Both);
					socket.Dispose();
					continue;
				}

				try
				{
					TManagedSessionType session = await AcceptNewSessionsAsync(socket, token);

					//Once everything is setup for the session we should let the session know it's initialized.
					session.OnSessionInitialized();
				}
				catch (Exception e)
				{
					if (Logger.IsErrorEnabled)
						Logger.Error($"Socket failed to create session. Reason: {e}");

					//We wrap this in a trp because socket maybe fails to shutdown, but we MUST dispose.
					try
					{
						if(socket.Connected)
							socket.Shutdown(SocketShutdown.Both);
					}
					finally
					{
						socket.Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Preforms the socket and session managed and creation logic for creating a new managed network session.
		/// </summary>
		/// <param name="socket">Socket to make a session for.</param>
		/// <param name="token">Cancel token.</param>
		/// <returns>Awaitable that indicates when the session has connected.</returns>
		private async Task<TManagedSessionType> AcceptNewSessionsAsync(Socket socket, CancellationToken token)
		{
			SocketConnection connection = default;
			TManagedSessionType clientSession = default;
			int connectionId = 0;
			try
			{
				IPEndPoint clientAddress = ((IPEndPoint)socket.RemoteEndPoint);
				connectionId = Interlocked.Increment(ref _lifetimeConnectionCount);
				connection = SocketConnection.Create(socket, PipeOptions.Default, PipeOptions.Default, SocketConnectionOptions.ZeroLengthReads);

				if(Logger.IsInfoEnabled)
					Logger.Info($"Attempting to create Session for Address: {clientAddress.Address} Id: {connectionId}");

				clientSession = Create(new SessionCreationContext(connection, new SessionDetails(new NetworkAddressInfo(clientAddress.Address, ServerAddress.Port), connectionId)));

				clientSession.AttachDisposable(connection);
				clientSession.AttachDisposable(socket);

				StartNetworkSessionTasks(token, clientSession);

				if(!Sessions.TryAdd(connectionId, clientSession))
					throw new InvalidOperationException($"Failed to add Session: {clientSession} to {Sessions} container with Id: {connectionId}");

				return clientSession;
			}
			catch (Exception e)
			{
				if (Logger.IsErrorEnabled)
					Logger.Error($"Failed to creation Session: {connectionId}. Reason: {e}");

				//Encounter a critical issue and could not create the session
				//but we also don't want to leak.
				clientSession?.Dispose();
				connection?.Dispose();
				throw;
			}
		}
	}
}
