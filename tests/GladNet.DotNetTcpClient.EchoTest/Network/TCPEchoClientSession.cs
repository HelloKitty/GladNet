using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	public sealed class TCPEchoClientSession : BaseTcpManagedSession<string, string>
	{
		private INetworkMessageDispatchingStrategy<string, string> MessageDispatcher { get; }

		private SessionMessageContext<string> CachedSessionContext { get; }

		public TCPEchoClientSession(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionDetails details, SessionMessageBuildingServiceContext<string, string> messageServices,
			INetworkMessageDispatchingStrategy<string, string> messageDispatcher)
			: base(networkOptions, connection, details, messageServices)
		{
			MessageDispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));

			//We build the session context 1 time because it should not change
			//Rather than inject as a dependency, we can build it in here. Really optional to inject
			//or build it in CTOR
			//Either way we build a send service and session context that captures it to provide to handling.
			IMessageSendService<string> sendService = new QueueBasedMessageSendService<string>(this.MessageService.OutgoingMessageQueue);
			CachedSessionContext = new SessionMessageContext<string>(details, sendService, ConnectionService);
		}

		public TCPEchoClientSession(NetworkConnectionOptions networkOptions, SocketConnection connection, SessionDetails details, SessionMessageBuildingServiceContext<string, string> messageServices,
			INetworkMessageDispatchingStrategy<string, string> messageDispatcher, INetworkMessageInterface<string, string> messageInterface)
			: base(networkOptions, connection, details, messageServices, messageInterface)
		{
			MessageDispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));

			//We build the session context 1 time because it should not change
			//Rather than inject as a dependency, we can build it in here. Really optional to inject
			//or build it in CTOR
			//Either way we build a send service and session context that captures it to provide to handling.
			IMessageSendService<string> sendService = new QueueBasedMessageSendService<string>(this.MessageService.OutgoingMessageQueue);
			CachedSessionContext = new SessionMessageContext<string>(details, sendService, ConnectionService);
		}

		/// <inheritdoc />
		public override async Task OnNetworkMessageReceivedAsync(NetworkIncomingMessage<string> message, CancellationToken token = default)
		{
			await MessageDispatcher.DispatchNetworkMessageAsync(CachedSessionContext, message, token);
		}
	}
}
