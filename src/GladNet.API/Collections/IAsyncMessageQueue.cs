using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Common interface for an async message queue for GladNet.
	/// Implementers should provide a THREADSAFE queueing and dequeue functionality
	/// for the provided generic message type.
	/// </summary>
	/// <typeparam name="TMessageType">The generic message type the queue supports.</typeparam>
	public interface IAsyncMessageQueue<TMessageType>
	{
		/// <summary>
		/// Attempts to dequeue a message from the async message queue.
		/// Will not return until cancelled or a message is available.
		/// </summary>
		/// <param name="token">Cancel token indicating if the operation should be cancelled.</param>
		/// <returns></returns>
		Task<TMessageType> DequeueAsync(CancellationToken token = default);

		/// <summary>
		/// Queues a message into the async message queue.
		/// </summary>
		/// <param name="message">The message instance to queue.</param>
		/// <param name="token">Cancel token indicating if the operation should be cancelled.</param>
		/// <returns>True if the enqueue operation was successful. If false, then <see cref="message"/> was NOT queued.</returns>
		Task<bool> EnqueueAsync(TMessageType message, CancellationToken token = default);
	}
}
