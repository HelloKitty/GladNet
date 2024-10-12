using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	//Due to Span constraints we actually cannly use a ref struct as the context due to generics issue.
	//See: https://stackoverflow.com/questions/50871135/why-ref-structs-cannot-be-used-as-type-arguments
	//Below is ideal if C# 10 ever supports it.
	/*public ref struct PacketHeaderCreationContext
	{
		/// <summary>
		/// The packet header buffer.
		/// </summary>
		public Span<byte> Buffer { get; }

		/// <summary>
		/// Creates a new context for header creation.
		/// </summary>
		/// <param name="buffer">The binary buffer the packet is contained within.</param>
		public PacketHeaderCreationContext(Span<byte> buffer)
		{
			Buffer = buffer;
		}
	}*/

	/// <summary>
	/// The creation context for a packet header.
	/// </summary>
	public sealed class PacketHeaderCreationContext : IDisposable
	{
		/// <summary>
		/// Represents the internal buffer wrapper for the header data.
		/// </summary>
		private ReadOnlySequence<byte> InternalBuffer { get; }

		/// <summary>
		/// Internal sync/lock object.
		/// </summary>
		private readonly object SyncObj = new object();

		/// <summary>
		/// Represents the internal temporarily allocated buffer for the header bytes.
		/// </summary>
		private byte[] InternalHeaderByteBuffer { get; set; }

		/// <summary>
		/// We create an internal header pool for allocating and avoiding contention on a general pool.
		/// </summary>
		private static ArrayPool<byte> HeaderCreationPool { get; } = ArrayPool<byte>.Create();

		/// <summary>
		/// Indicates if the header creation context's buffer/memory is disposed.
		/// </summary>
		public bool isDisposed { get; private set; } = false;

		/// <summary>
		/// Indicates the exact binary size of the header.
		/// Must be smaller or equal to <see cref="InternalBuffer"/> length.
		/// </summary>
		public int HeaderBinarySize { get; }

		public PacketHeaderCreationContext(ReadOnlySequence<byte> internalBuffer, int headerBinarySize)
		{
			InternalBuffer = internalBuffer;
			HeaderBinarySize = headerBinarySize;

			if (internalBuffer.Length < headerBinarySize)
				throw new InvalidOperationException($"Specified header length: {headerBinarySize} exceed buffer length: {internalBuffer.Length}");
		}

		/// <summary>
		/// Gets the <see cref="Span{T}"/> that represents the header.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="ObjectDisposedException"></exception>
		public Span<byte> GetSpan()
		{
			lock (SyncObj)
			{
				if (isDisposed)
					throw new ObjectDisposedException(nameof(PacketHeaderCreationContext));
				
				if (InternalHeaderByteBuffer != null)
				{
					//Slice, don't assume array pool gives exact size. IT DOES NOT!
					if(InternalHeaderByteBuffer.Length == HeaderBinarySize)
						return new Span<byte>(InternalHeaderByteBuffer);
					else
						return new Span<byte>(InternalHeaderByteBuffer).Slice(0, HeaderBinarySize);
				}

				//Idea here is to temporarily rent a buffer.
				InternalHeaderByteBuffer = HeaderCreationPool.Rent(HeaderBinarySize);

				//TODO: This may needlessly copy a bunch of data into a buffer that we don't need, above we
				//allocate a buffer of InternalBuffer.Length size. This could be thousands of read bytes
				//but we may only want 
				Span<byte> buffer = new Span<byte>(InternalHeaderByteBuffer, 0, HeaderBinarySize);

				//This may seem unsafe at first but the implementation will not try to copy more bytes than
				//are available in the Span. Therefore Span CAN be much smaller than the available internal buffer
				//without worrying.
				//See: https://github.com/dotnet/corefxlab/blob/master/src/System.Buffers.Experimental/System/Buffers/BufferExtensions.cs#L29
				InternalBuffer.Slice(0, HeaderBinarySize).CopyTo(buffer);

				return buffer;
			}
		}

		/// <summary>
		/// Disposes the internal buffers related to the packet header.
		/// SHOULD ONLY BE CALLED INTERNALLY BY THE LIBRARY!
		/// </summary>
		public void Dispose()
		{
			lock (SyncObj)
			{
				if (isDisposed)
					return;

				if(InternalHeaderByteBuffer != null)
					HeaderCreationPool.Return(InternalHeaderByteBuffer);

				InternalHeaderByteBuffer = null;
				isDisposed = true;
			}
		}
	}
}
