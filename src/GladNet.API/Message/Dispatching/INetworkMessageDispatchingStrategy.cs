using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	//You may wonder why this exists. It exists because you may want to async route messages to other services/servers.
	//The responses may be awaited asyncronouosly and then sent back to the client. This could be for microserivces or for
	//load balancing reasons. This only exists on the server for that reason. Handler API is for clients too though, but this
	//exists before the handler API for the reasoning above.
	/// <summary>
	/// Contract for strategies for dispatching network messages.
	/// </summary>
	public interface INetworkMessageDispatchingStrategy<TPayloadReadType, TPayloadWriteType>
		where TPayloadWriteType : class 
		where TPayloadReadType : class
	{
		/// <summary>
		/// Message handling method.
		/// The task should complete not when the message is done being handled but when the
		/// session should continue reading messages. This means that if desired you should be able to read more
		/// messages even while other session's messages are being handled.
		/// </summary>
		/// <param name="context">The context for the incoming message.</param>
		/// <param name="message">The incoming message.</param>
		/// <param name="token">The cancel token for the operation.</param>
		/// <returns>A task that completes when the session should begin reading more messages.</returns>
		Task DispatchNetworkMessageAsync(SessionMessageContext<TPayloadWriteType> context, NetworkIncomingMessage<TPayloadReadType> message, CancellationToken token = default);
	}
}
