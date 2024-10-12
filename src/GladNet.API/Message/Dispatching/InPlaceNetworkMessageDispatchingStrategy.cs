using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;

namespace GladNet
{
	/// <summary>
	/// The default in place dispatching strategy for network messages which
	/// handles them immediately without any enqueueing and will yield back when
	/// the message handling has been completed.
	/// </summary>
	/// <typeparam name="TPayloadWriteType">The type of payload that is writeable by the application..</typeparam>
	/// <typeparam name="TPayloadReadType">The type of payload that is handled.</typeparam>
	public sealed class InPlaceNetworkMessageDispatchingStrategy<TPayloadReadType, TPayloadWriteType> : INetworkMessageDispatchingStrategy<TPayloadReadType, TPayloadWriteType>
		where TPayloadWriteType : class
		where TPayloadReadType : class
	{
		/// <summary>
		/// Handler service that can handle the incoming message.
		/// </summary>
		private IMessageHandlerService<TPayloadReadType, SessionMessageContext<TPayloadWriteType>> HandlerService { get; }

		public InPlaceNetworkMessageDispatchingStrategy(IMessageHandlerService<TPayloadReadType, SessionMessageContext<TPayloadWriteType>> handlerService)
		{
			HandlerService = handlerService ?? throw new ArgumentNullException(nameof(handlerService));
		}

		/// <inheritdoc />
		public async Task DispatchNetworkMessageAsync(SessionMessageContext<TPayloadWriteType> context, NetworkIncomingMessage<TPayloadReadType> message, CancellationToken token = default)
		{
			await HandlerService.HandleMessageAsync(context, message.Payload, token);
		}
	}
}