using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using Glader.Essentials;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// Base application base for a high performance async WebSocket server application.
	/// Builds, manages and maintains <see cref="ManagedSession"/>s internally
	/// when clients connect.
	/// </summary>
	public abstract class WebSocketGladNetServerApplication<TManagedSessionType> : GladNetServerApplication<TManagedSessionType, SessionCreationContext>
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
		protected WebSocketGladNetServerApplication(NetworkAddressInfo serverAddress, ILog logger)
			: base(serverAddress, logger)
		{

		}

		/// <summary>
		/// Indicates if the provided <see cref="Socket"/> is acceptable.
		/// Return true if the client should be handled. This will likely lead to
		/// a call to <see cref="Create"/>. Returning false
		/// will mean this connection should be disconnected and rejected and no
		/// client representation will be created for it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="connection">The <see cref="Socket"/> to check the acceptance for.</param>
		/// <returns></returns>
		protected abstract bool IsClientAcceptable(HttpListenerContext context, IWebSocketConnection connection);

		/// <inheritdoc />
		public override async Task BeginListeningAsync(CancellationToken token = default)
		{
			if (Logger.IsInfoEnabled)
				Logger.Info($"Server begin listening.");

			using (HttpListener listenSocket = new HttpListener())
			{
				RegisterListenerEndpoints(ServerAddress, listenSocket.Prefixes);
				listenSocket.Start();

				if (!listenSocket.IsListening)
				{
					if (Logger.IsErrorEnabled)
						Logger.Error($"Socket failed to bind to Port: {ServerAddress.Port} on Address: {ServerAddress.AddressEndpoint.ToString()}");

					return;
				}

				if (Logger.IsInfoEnabled)
					Logger.Info($"Server bound to Port: {ServerAddress.Port} on Address: {ServerAddress.AddressEndpoint.ToString()}");

				try
				{
					await SocketAcceptLoopAsync(listenSocket, token);
				}
				catch(Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Server encountered unhandled exception in socket accept loop. Error: {e}");

					throw;
				}
			}
		}

		/// <summary>
		/// Implementers can derive this to override the default handling for registering endpoints.
		/// </summary>
		/// <param name="endpointCollection">Endpoints.</param>
		/// <param name="info">The network address info.</param>
		protected virtual void RegisterListenerEndpoints(NetworkAddressInfo info, HttpListenerPrefixCollection endpointCollection)
		{
			endpointCollection.Add($"https://{info.AddressEndpoint.MapToIPv4().ToString()}:{info.Port}/");
		}

		private async Task SocketAcceptLoopAsync(HttpListener listenSocket, CancellationToken token)
		{
			if (listenSocket == null) throw new ArgumentNullException(nameof(listenSocket));

			while (!token.IsCancellationRequested)
			{
				IWebSocketConnection socket = null;
				HttpListenerContext context = null;
				try
				{
					context = await listenSocket.GetContextAsync();

					// Only handle websocket request, no idea what else can be went. Guess normal HTTP lol?
					if (!context.Request.IsWebSocketRequest)
						continue;

					HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
					socket = new DotNetWebSocketConnection(webSocketContext.WebSocket);
				}
				catch (Exception e)
				{
					if (Logger.IsErrorEnabled)
						Logger.Error($"Encountered error in WebSocket accept. Error: {e}");

					continue;
				}

				//Try required because we're calling into user code.
				try
				{
					if (!IsClientAcceptable(context, socket))
					{
						await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, $"Client unacceptable.", token);
						socket.Dispose();
						continue;
					}
				}
				catch (Exception e)
				{
					if (Logger.IsInfoEnabled)
						Logger.Error($"Socket failed to creation. Exception in accept check. Reason: {e}");

					await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, $"Socket accept Error: {e.GetType().Name}", token);
					socket.Dispose();
					continue;
				}

				try
				{
					TManagedSessionType session = await AcceptNewSessionsAsync(context, socket, token);

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
						if (socket.State == WebSocketState.Open)
							await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, $"Session Creation Error: {e.GetType().Name}", token);
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
		private Task<TManagedSessionType> AcceptNewSessionsAsync(HttpListenerContext context, IWebSocketConnection socket, CancellationToken token)
		{
			TManagedSessionType clientSession = default;
			int connectionId = 0;
			try
			{
				IPEndPoint clientAddress = context.Request.RemoteEndPoint;
				connectionId = Interlocked.Increment(ref _lifetimeConnectionCount);

				if (Logger.IsInfoEnabled)
					Logger.Info($"Attempting to create Session for Address: {clientAddress?.Address} Id: {connectionId}");

				clientSession = Create(new SessionCreationContext(socket, new SessionDetails(new NetworkAddressInfo(clientAddress?.Address, ServerAddress.Port), connectionId)));

				clientSession.AttachDisposable(socket);

				StartNetworkSessionTasks(token, clientSession);

				if (!Sessions.TryAdd(connectionId, clientSession))
					throw new InvalidOperationException($"Failed to add Session: {clientSession} to {Sessions} container with Id: {connectionId}");

				return Task.FromResult(clientSession);
			}
			catch (Exception e)
			{
				if (Logger.IsErrorEnabled)
					Logger.Error($"Failed to creation Session: {connectionId}. Reason: {e}");

				//Encounter a critical issue and could not create the session
				//but we also don't want to leak.
				clientSession?.Dispose();
				socket?.Dispose();
				throw;
			}
		}
	}
}
