using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace GladNet
{
	/// <summary>
	/// <see cref="AsyncProducerConsumerQueue{T}"/> implementation of the GladNet <see cref="IAsyncMessageQueue{TMessageType}"/>
	/// </summary>
	/// <typeparam name="TMessageType"></typeparam>
	public sealed class AsyncExProducerConsumerQueueAsyncMessageQueue<TMessageType> 
		: IAsyncMessageQueue<TMessageType>, IDisposable
	{
		private AsyncProducerConsumerQueue<TMessageType> InternalQueue { get; } = new AsyncProducerConsumerQueue<TMessageType>();

		/// <inheritdoc />
		public async Task<TMessageType> DequeueAsync(CancellationToken token = default)
		{
			return await InternalQueue.DequeueAsync(token);
		}

		/// <inheritdoc />
		public async Task<bool> EnqueueAsync(TMessageType message, CancellationToken token = default)
		{
			await InternalQueue.EnqueueAsync(message, token);
			return true;
		}

		public void Dispose()
		{
			InternalQueue.CompleteAdding();
		}
	}
}
