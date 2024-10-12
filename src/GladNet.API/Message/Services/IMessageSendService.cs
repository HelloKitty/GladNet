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
	/// Contract for services that provide the ability to send messages.
	/// </summary>
	/// <typeparam name="TMessageBaseType">The message's base type that is sent.</typeparam>
	public interface IMessageSendService<in TMessageBaseType>
		where TMessageBaseType : class
	{
		//Implementer of this method should assume multi callers may call this method at one time
		//Therefore threadsafety must be implemented an assured by the implementer of this method.
		/// <summary>
		/// Sends a <typeparamref name="TMessageBaseType"/> asyncronously.
		/// The task will complete when the network message is sent or when the token is cancelled.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="token">The cancel token for the send operation.</param>
		/// <returns>Returns an awaitable when the send operation is completed.</returns>
		Task<SendResult> SendMessageAsync(TMessageBaseType message, CancellationToken token = default);
	}
}
