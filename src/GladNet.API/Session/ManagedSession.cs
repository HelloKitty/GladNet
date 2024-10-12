using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// Base type for managed sessions.
	/// </summary>
	public abstract class ManagedSession : IManagedSession, ISingleDisposable, IDisposableAttachable
	{
		/// <summary>
		/// Internal lock object.
		/// </summary>
		protected readonly object SyncObj = new object();

		/// <summary>
		/// Represents all the disposable dependencies of a <see cref="ManagedSession"/>.
		/// This will be disposed when the session is disposed.
		/// </summary>
		private List<IDisposable> InternalDisposables { get; } = new List<IDisposable>();

		/// <inheritdoc />
		public INetworkConnectionService ConnectionService { get; }

		/// <inheritdoc />
		public SessionDetails Details { get; }

		/// <summary>
		/// Represents the network configuration options for the session.
		/// </summary>
		protected NetworkConnectionOptions NetworkOptions { get; }

		/// <inheritdoc />
		public bool isDisposed { get; private set; }

		internal ManagedSession(INetworkConnectionService connectionService, SessionDetails details, NetworkConnectionOptions networkOptions)
		{
			ConnectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
			Details = details ?? throw new ArgumentNullException(nameof(details));
			NetworkOptions = networkOptions ?? throw new ArgumentNullException(nameof(networkOptions));
		}

		/// <summary>
		/// Implementers should start the internal network listening async algorithm.
		/// </summary>
		/// <returns>Awaitable that completes when listening is finished.</returns>
		public abstract Task StartListeningAsync(CancellationToken token = default);

		/// <summary>
		/// Implementers should start the internal network writing async algorithm.
		/// </summary>
		/// <returns>Awaitable that completes when writing is finished.</returns>
		public abstract Task StartWritingAsync(CancellationToken token = default);

		//See: https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
		/// <inheritdoc />
		public void Dispose()
		{
			lock (SyncObj)
			{
				if (isDisposed)
					return;

				try
				{
					//Foreach but make sure to guard against exceptions
					//caused by disposal because we need to dispose of ALL resources first or else we
					//may leak.
					Exception optionalException = null;
					foreach(var disposable in InternalDisposables)
						try
						{
							disposable.Dispose();
						}
						catch(Exception e)
						{
							optionalException = e;
						}

					//We throw so we don't silently supress the error.
					//We could have multiple exceptions from this operation!! We only get the last though.
					if(optionalException != null)
						throw new InvalidOperationException($"Failed to dispose of all resources gracefully. Error: {optionalException}", optionalException);
				}
				finally
				{
					isDisposed = true;

					// Suppress finalization.
					GC.SuppressFinalize(this);

					// Dispose of unmanaged resources.
					Dispose(true);
				}
			}
		}

		//See: https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
		/// <summary>
		/// Implements can additionally dispose of resources.
		/// This is called via <see cref="Dispose()"/> or the runtime finalizer.
		/// Implementers should always call the base.
		/// </summary>
		/// <param name="disposing">indicates whether the method call comes from a Dispose method (its value is true) or from a finalizer (its value is false).</param>
		protected virtual void Dispose(bool disposing)
		{

		}

		/// <summary>
		/// Attaches a disposable <see cref="IDisposable"/> dependency/resource to the <see cref="ManagedSession"/>.
		/// This will be disposed of alongside when <see cref="ManagedSession"/>'s <see cref="Dispose()"/> is called.
		/// </summary>
		/// <param name="disposable"></param>
		public void AttachDisposable(IDisposable disposable)
		{
			if (disposable == null) throw new ArgumentNullException(nameof(disposable));

			lock (SyncObj)
			{
				if (isDisposed)
					throw new ObjectDisposedException($"Cannot attach {disposable.GetType().Name} as an attached disposable if the session is already disposed.");

				InternalDisposables.Add(disposable);
			}
		}

		/// <summary>
		/// Called internally by GladNet when the session is considered
		/// initialized. It's safer to do things here than in the constructor.
		/// (Ex. Sending a network message to the client).
		/// WARNING: If this method throws then the session may be closed.
		/// </summary>
		protected internal virtual void OnSessionInitialized()
		{

		}
	}
}
