using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Contract for types that handle a received network message.
	/// </summary>
	/// <typeparam name="TPayloadType"></typeparam>
	public interface INetworkMessageReceivable<TPayloadType>
		where TPayloadType : class
	{
		/// <summary>
		/// Called when a network message is received.
		/// Can also be called to simulate the receiving a network message.
		/// </summary>
		/// <param name="message">The network message recieved.</param>
		/// <param name="token">Cancel token that can be used to indicate if a session is cancelled.</param>
		/// <returns></returns>
		Task OnNetworkMessageReceivedAsync(NetworkIncomingMessage<TPayloadType> message, CancellationToken token = default(CancellationToken));
	}
}