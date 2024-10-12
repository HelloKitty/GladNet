using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;
using Nito.AsyncEx;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	/// <summary>
	/// Base TCP <see cref="SocketConnection"/>-based <see cref="ManagedSession"/>
	/// implementation.
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	/// <typeparam name="TPayloadReadType"></typeparam>
	public abstract class BaseTcpManagedSession<TPayloadReadType, TPayloadWriteType> 
		: ManagedSession<TPayloadReadType, TPayloadWriteType>
		where TPayloadWriteType : class 
		where TPayloadReadType : class
	{
		/// <summary>
		/// The socket connection.
		/// </summary>
		protected SocketConnection Connection { get; }

		protected BaseTcpManagedSession(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices) 
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices,
				BuildMessageInterfaceContext(networkOptions, connection, messageServices))
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		//This overload lets implementer specify a messageInterface.
		protected BaseTcpManagedSession(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices,
			INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> messageInterface)
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices,
				BuildMessageInterfaceContext(messageInterface))
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		//This overload lets implementer specify a message interface context.
		protected BaseTcpManagedSession(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices,
			SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> messageInterfaces)
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices, messageInterfaces)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		private static SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> BuildMessageInterfaceContext(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices)
		{
			INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> messageInterface = new SocketConnectionNetworkMessageInterface<TPayloadReadType, TPayloadWriteType>(networkOptions, connection, messageServices);
			return BuildMessageInterfaceContext(messageInterface);
		}

		private static SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> BuildMessageInterfaceContext(INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> messageInterface)
		{
			return new SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType>(new AsyncExProducerConsumerQueueAsyncMessageQueue<TPayloadWriteType>(), messageInterface);
		}

		/// <inheritdoc />
		public override async Task StartListeningAsync(CancellationToken token = default)
		{
			//We override this to add some SocketConnection handling on finishing.
			try
			{
				await base.StartListeningAsync(token);
			}
			catch (ConnectionResetException e)
			{
				//ConnectionResetException is thrown by Pipeline Socket API when it disconnects
				//and I don't yet know how to suppress it fully.
				//Consider a cancel a graceful complete
				await Connection.Input.CompleteAsync(e);
				return;
			}
			catch (TaskCanceledException e)
			{
				//Consider a cancel a graceful complete
				await Connection.Input.CompleteAsync(e);
				return;
			}
			catch (Exception e)
			{
				await Connection.Input.CompleteAsync(e);
				throw;
			}
			finally
			{
				await Connection.Input.CompleteAsync();
			}
		}

		/// <inheritdoc />
		public override async Task StartWritingAsync(CancellationToken token = default)
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					//Dequeue from the outgoing message queue and send through the send service.
					TPayloadWriteType payload = await MessageService.OutgoingMessageQueue.DequeueAsync(token);
					SendResult result = await MessageService.MessageInterface.SendMessageAsync(payload, token);

					//TODO: Add logging!
					if (result != SendResult.Sent && result != SendResult.Enqueued)
						return;
				}
			}
			catch (ConnectionResetException e)
			{
				//ConnectionResetException is thrown by Pipeline Socket API when it disconnects
				//and I don't yet know how to suppress it fully.
				//Consider a cancel a graceful complete
				await Connection.Output.CompleteAsync(e);
				return;
			}
			catch(TaskCanceledException e)
			{
				//Consider a cancel a graceful complete
				await Connection.Output.CompleteAsync(e);
				return;
			}
			catch(Exception e)
			{
				await Connection.Output.CompleteAsync(e);
				throw;
			}
			finally
			{
				await Connection.Output.CompleteAsync();
			}
		}
	}
}
