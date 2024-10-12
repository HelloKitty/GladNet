using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	//TODO: We need better naming. Kinda conflicts with the existing message context for handlers
	/// <summary>
	/// The context for a network message.
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	public class SessionMessageContext<TPayloadWriteType> : IPeerSessionMessageContext<TPayloadWriteType>
		where TPayloadWriteType : class 
	{
		/// <summary>
		/// The session associated with the message.
		/// </summary>
		public SessionDetails Details { get; }

		/// <inheritdoc />
		public INetworkConnectionService ConnectionService { get; }

		/// <inheritdoc />
		public IMessageSendService<TPayloadWriteType> MessageService { get; }

		/// <inheritdoc />
		public SessionMessageContext(SessionDetails details, 
			IMessageSendService<TPayloadWriteType> messageService, 
			INetworkConnectionService connectionService)
		{
			MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
			ConnectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
			Details = details ?? throw new ArgumentNullException(nameof(details));
		}
	}
}
