using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// Contract for a type that provided read/write interface for network messages
	/// and payloads.
	/// </summary>
	/// <typeparam name="TPayloadReadType">The incoming/read payload type.</typeparam>
	/// <typeparam name="TPayloadWriteType">The outgoing/written payload type.</typeparam>
	public interface INetworkMessageInterface<TPayloadReadType, in TPayloadWriteType> : IMessageSendService<TPayloadWriteType>
		where TPayloadReadType : class
		where TPayloadWriteType : class
	{
		/// <summary>
		/// Produces a <see cref="NetworkIncomingMessage{TPayloadType}"/> asyncronously.
		/// The task will complete when a network message is available or when the token is cancelled.
		/// </summary>
		/// <returns>Returns a future that will complete when a message is available.</returns>
		Task<NetworkIncomingMessage<TPayloadReadType>> ReadMessageAsync(CancellationToken token = default(CancellationToken));
	}
}
