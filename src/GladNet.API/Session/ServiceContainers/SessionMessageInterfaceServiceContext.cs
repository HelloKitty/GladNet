using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Service container for a session's network message interfacing services.
	/// Not to be confused with <see cref="INetworkMessageInterface{TPayloadReadType,TPayloadWriteType}"/> which provides
	/// a network interface for messages.
	/// This container contains services such as that for interacting/interfacing with the network.
	/// </summary>
	/// <typeparam name="TPayloadWriteType">The outgoing write type for payloads.</typeparam>
	/// <typeparam name="TPayloadReadType">The incoming read type for payloads.</typeparam>
	public sealed class SessionMessageInterfaceServiceContext<TPayloadReadType, TPayloadWriteType> 
		where TPayloadReadType : class
		where TPayloadWriteType : class
	{
		/// <summary>
		/// The outgoing network payload queue.
		/// This queue should be added into if the session desires a message be outgoing to the peer.
		/// </summary>
		public IAsyncMessageQueue<TPayloadWriteType> OutgoingMessageQueue { get; }

		/// <summary>
		/// The service that provides an interface for communication for reading and writing payloads.
		/// </summary>
		public INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> MessageInterface { get; }

		public SessionMessageInterfaceServiceContext(IAsyncMessageQueue<TPayloadWriteType> outgoingMessageQueue, INetworkMessageInterface<TPayloadReadType, TPayloadWriteType> networkMessageInterface)
		{
			OutgoingMessageQueue = outgoingMessageQueue ?? throw new ArgumentNullException(nameof(outgoingMessageQueue));
			MessageInterface = networkMessageInterface ?? throw new ArgumentNullException(nameof(networkMessageInterface));
		}
	}
}
