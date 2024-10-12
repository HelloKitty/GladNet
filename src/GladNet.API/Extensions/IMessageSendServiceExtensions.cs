using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// <see cref="IMessageSendService{TMessageBaseType}"/> extension methods.
	/// </summary>
	public static class IMessageSendServiceExtensions
	{
		/// <summary>
		/// Sends a <typeparamref name="TMessageType"/> asyncronously.
		/// The task will complete when the network message is sent or when the token is cancelled.
		/// This special overload requires the message type to be newable (contain a public parameterless ctor).
		/// </summary>
		/// <param name="service">The send service.</param>
		/// <param name="token">The cancel token for the send operation.</param>
		/// <returns>Returns an awaitable when the send operation is completed.</returns>
		public static Task<SendResult> SendMessageAsync<TMessageType>(this IMessageSendService<TMessageType> service, CancellationToken token = default) 
			where TMessageType : class, new()
		{
			//TODO: We could do some polling with TMessageType at some point probably.
			return service.SendMessageAsync(new TMessageType(), token);
		}
	}
}
