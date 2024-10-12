using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;
using Nito.AsyncEx;

namespace GladNet
{
	/// <summary>
	/// Base WebSocket <see cref="WebSocket"/>-based <see cref="ManagedSession"/>
	/// implementation.
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	/// <typeparam name="TPayloadReadType"></typeparam>
	public abstract class BaseWebSocketManagedSession<TPayloadReadType, TPayloadWriteType> 
		: ManagedSession<TPayloadReadType, TPayloadWriteType>
		where TPayloadWriteType : class 
		where TPayloadReadType : class
	{
		/// <summary>
		/// The socket connection.
		/// </summary>
		protected IWebSocketConnection Connection { get; }

		protected BaseWebSocketManagedSession(NetworkConnectionOptions networkOptions, IWebSocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices) 
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices,
				BuildMessageInterfaceContext(networkOptions, connection, messageServices))
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		//This overload lets implementer specify a messageInterface.
		protected BaseWebSocketManagedSession(NetworkConnectionOptions networkOptions, IWebSocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices,
			INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> messageInterface)
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices,
				BuildMessageInterfaceContext(messageInterface))
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		//This overload lets implementer specify a message interface context.
		protected BaseWebSocketManagedSession(NetworkConnectionOptions networkOptions, IWebSocketConnection connection, SessionDetails details,
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices,
			SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> messageInterfaces)
			: base(new SocketConnectionConnectionServiceAdapter(connection), details, networkOptions, messageServices, messageInterfaces)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		private static SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> BuildMessageInterfaceContext(NetworkConnectionOptions networkOptions, IWebSocketConnection connection, SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices)
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
			await base.StartListeningAsync(token);
		}

		/// <inheritdoc />
		public override async Task StartWritingAsync(CancellationToken token = default)
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
	}
}
