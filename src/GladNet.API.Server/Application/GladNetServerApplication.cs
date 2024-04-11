using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Glader.Essentials;

namespace GladNet
{
	/// <summary>
	/// Base type for GladNet server application bases.
	/// </summary>
	/// <typeparam name="TManagedSessionType"></typeparam>
	/// <typeparam name="TSessionCreationContextType"></typeparam>
	public abstract class GladNetServerApplication<TManagedSessionType, TSessionCreationContextType>
		: IServerApplicationListenable, IFactoryCreatable<TManagedSessionType, TSessionCreationContextType>
		where TManagedSessionType : ManagedSession
	{
		/// <summary>
		/// Network address information for the server.
		/// </summary>
		public NetworkAddressInfo ServerAddress { get; }

		/// <summary>
		/// Server application logger.
		/// </summary>
		public ILog Logger { get; }

		//TODO: We need a better API for exposing this.
		/// <summary>
		/// Collection that maps connection id to the managed session types.
		/// </summary>
		protected ConcurrentDictionary<int, TManagedSessionType> Sessions { get; } = new ConcurrentDictionary<int, TManagedSessionType>();

		/// <summary>
		/// Event that is fired when a managed session is ended.
		/// This could be caused by disconnection but is not required to be related to disconnection.
		/// </summary>
		public event EventHandler<ManagedSessionContextualEventArgs<TManagedSessionType>> OnManagedSessionEnded;

		/// <summary>
		/// Creates a new server application with the specified address.
		/// </summary>
		/// <param name="serverAddress">Address for listening.</param>
		/// <param name="logger">The logger.</param>
		protected GladNetServerApplication(NetworkAddressInfo serverAddress, ILog logger)
		{
			ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public abstract Task BeginListeningAsync(CancellationToken token = default);

		//Should be overriden by the consumer of the library.
		/// <summary>
		/// Called internally when a session is being created.
		/// This method should produce a valid session and is considered the hub of the connection.
		/// </summary>
		/// <param name="context">The context for creating the managed session.</param>
		/// <returns>A non-null session.</returns>
		public abstract TManagedSessionType Create(TSessionCreationContextType context);

		/// <summary>
		/// Starts the read/write network tasks.
		/// </summary>
		/// <param name="token">The network cancel tokens.</param>
		/// <param name="clientSession">The session.</param>
		protected void StartNetworkSessionTasks(CancellationToken token, TManagedSessionType clientSession)
		{
			Task writeTask = Task.Run(async () =>
			{
				try
				{
					await StartSessionNetworkThreadAsync(clientSession.Details, clientSession.StartWritingAsync(token), token, "Write");
				}
				catch (Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Session: {clientSession.Details.ConnectionId} Write thread encountered critical failure. Error: {e}");
					throw;
				}
			}, token);

			Task readTask = Task.Run(async () =>
			{
				try
				{
					await StartSessionNetworkThreadAsync(clientSession.Details, clientSession.StartListeningAsync(token), token, "Read");
				}
				catch (Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Session: {clientSession.Details.ConnectionId} Read thread encountered critical failure. Error: {e}");
					throw;
				}
			}, token);

			Task.Run(async () =>
			{
				try
				{
					await clientSession.ConnectionService.DisconnectAsync();
				}
				catch(Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Session: {clientSession.Details.ConnectionId} was open but failed to disconnect. Reason: {e}");
				}
				finally
				{
					try
					{
						clientSession.Dispose();
					}
					catch(Exception e)
					{
						if (Logger.IsErrorEnabled)
							Logger.Error($"Session: {clientSession.Details.ConnectionId} failed to dispose. Reason: {e}");
					}
				}

				try
				{
					// Important that NO MATTER WHAT even if some cancel logic fails in this call that 
					// OnManagedSessionEnded is invoked
					await AwaitManagedReadWriteTasksAsync(clientSession, readTask, writeTask, token);
				}
				finally
				{
					if (Logger.IsDebugEnabled)
						Logger.Debug($"Session: {clientSession.Details.ConnectionId} Stopped Network Read/Write.");

					try
					{
						//Fire off to anyone interested in managed session ending. We should do this before we fully dispose it and remove it from the session collection.
						OnManagedSessionEnded?.Invoke(this, new ManagedSessionContextualEventArgs<TManagedSessionType>(clientSession));
					}
					catch(Exception e)
					{
						if (Logger.IsErrorEnabled)
							Logger.Error($"Failed Session: {clientSession.Details.ConnectionId} ended event. Reason: {e}");
					}
					finally
					{
						Sessions.TryRemove(clientSession.Details.ConnectionId, out _);

						try
						{
							clientSession.Dispose();
						}
						catch(Exception e)
						{
							if(Logger.IsErrorEnabled)
								Logger.Error($"Encountered error in Client: {clientSession.Details.ConnectionId} session disposal. Error: {e}");
							throw;
						}
					}
				}
			}, token);
		}

		private async Task AwaitManagedReadWriteTasksAsync(TManagedSessionType clientSession, Task readTask, Task writeTask, CancellationToken token)
		{
			try
			{
				await Task.WhenAny(readTask, writeTask);
			}
			catch (Exception e)
			{
				//Suppress this exception, we have critical deconstruction code to run.
				if (Logger.IsErrorEnabled)
					Logger.Error($"Session: {clientSession.Details.ConnectionId} encountered critical failure in awaiting network task. Error: {e}");
			}
			finally
			{
				
			}
		}

		private async Task StartSessionNetworkThreadAsync(SessionDetails details, Task task, CancellationToken token, string taskName)
		{
			if(details == null) throw new ArgumentNullException(nameof(details));
			if(task == null) throw new ArgumentNullException(nameof(task));

			try
			{
				await task;
			}
			catch(Exception e)
			{
				if(Logger.IsErrorEnabled)
					Logger.Error($"Session: {details.ConnectionId} encountered error in network {taskName} thread. Error: {e}");
			}
			finally
			{

			}
		}
	}
}
