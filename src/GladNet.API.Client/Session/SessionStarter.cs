using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

namespace GladNet
{
	//TODO: This is unfinished, not generalized enough to use in Server side yet.
	public sealed class SessionStarter<TSessionType>
		where TSessionType : ManagedSession, IDisposable
	{
		private ILog Logger { get; }

		public SessionStarter(ILog logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task StartAsync(TSessionType session, CancellationToken token = default)
		{
			Task writeTask = Task.Run(async () => await StartSessionNetworkThreadAsync(session.Details, session.StartWritingAsync(token), "Write"), token);
			Task readTask = Task.Run(async () => await StartSessionNetworkThreadAsync(session.Details, session.StartListeningAsync(token), "Read"), token);

			await Task.Run(async () =>
			{
				try
				{
					await Task.WhenAny(writeTask, readTask);
				}
				catch(Exception e)
				{
					//Suppress this exception, we have critical deconstruction code to run.
					if(Logger.IsErrorEnabled)
						Logger.Error($"Session: {session.Details.ConnectionId} encountered critical failure in awaiting network task. Error: {e}");
				}
				finally
				{
					if(Logger.IsDebugEnabled)
						Logger.Debug($"Session Stopping. Read State Error: {readTask.IsFaulted} Write State Error: {writeTask.IsFaulted} Read Completed: {readTask.IsCompleted} Write Completed: {writeTask.IsCompleted} Read Cancelled: {readTask.IsCanceled} Write Cancelled: {writeTask.IsCanceled}");

					if (readTask.IsFaulted)
						if(Logger.IsDebugEnabled)
							Logger.Debug($"Read Fault: {readTask.Exception}");

					if(writeTask.IsFaulted)
						if(Logger.IsDebugEnabled)
							Logger.Debug($"Write Fault: {readTask.Exception}");

					await TryGracefulDisconnectAsync(session);
				}

				if (Logger.IsDebugEnabled)
					Logger.Debug($"Session: {session.Details.ConnectionId} Stopped Network Read/Write.");

				try
				{
					//TODO: Maybe a general disconnection event???
				}
				catch(Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Failed Session: {session.Details.ConnectionId} ended event. Reason: {e}");
				}
				finally
				{
					try
					{
						session.Dispose();
					}
					catch(Exception e)
					{
						if(Logger.IsErrorEnabled)
							Logger.Error($"Encountered error in Client: {session.Details.ConnectionId} session disposal. Error: {e}");
						throw;
					}
				}
			}, token);
		}

		private async Task TryGracefulDisconnectAsync(TSessionType session)
		{
			try
			{
				await session.ConnectionService.DisconnectAsync();
			}
			catch (Exception e)
			{
				if (Logger.IsErrorEnabled)
					Logger.Error($"Session: {session.Details.ConnectionId} was open but failed to disconnect. Reason: {e}");
			}
			finally
			{
				try
				{
					session.Dispose();
				}
				catch (Exception e)
				{
					if (Logger.IsErrorEnabled)
						Logger.Error($"Session: {session.Details.ConnectionId} failed to dispose. Reason: {e}");
				}
			}
		}

		public void Start(TSessionType session, CancellationToken token = default)
		{
			//Don't block/await (fire and forget)
#pragma warning disable 4014
			StartAsync(session, token);
#pragma warning restore 4014
		}

		private async Task StartSessionNetworkThreadAsync(SessionDetails details, Task task, string taskName)
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

		public void Dispose()
		{
			// WARNING: heed warning in Thread.Abort doc, don't do it
			// See: https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=net-8.0
		}
	}
}
