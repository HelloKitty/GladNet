using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Queue-based implementation of <see cref="IMessageSendService{TMessageBaseType}"/>.
	/// Using a queuing mechanism returning immediately after enqueueing.
	/// This strategy should be preferred compared to direct/inplace message send/handling to avoid
	/// delaying/awaiting operations on critical threads.
	/// </summary>
	/// <typeparam name="TMessageBaseType"></typeparam>
	public class QueueBasedMessageSendService<TMessageBaseType> : IMessageSendService<TMessageBaseType> 
		where TMessageBaseType : class
	{
		/// <summary>
		/// Internal message queue for the send service to enqueue into.
		/// </summary>
		private IAsyncMessageQueue<TMessageBaseType> MessageQueue { get; }

		public QueueBasedMessageSendService(IAsyncMessageQueue<TMessageBaseType> messageQueue)
		{
			MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
		}

		/// <inheritdoc />
		public async Task<SendResult> SendMessageAsync(TMessageBaseType message, CancellationToken token = default)
		{
			bool enqueued = await MessageQueue.EnqueueAsync(message, token);

			return enqueued ? SendResult.Enqueued : SendResult.Error;
		}
	}
}
