using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace GladNet
{
	/// <summary>
	/// WebSocket <see cref="IWebSocketConnection"/> Pipelines-based implementation of <see cref="INetworkMessageInterface{TPayloadReadType,TPayloadWriteType}"/>
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	/// <typeparam name="TPayloadReadType"></typeparam>
	public class SocketConnectionNetworkMessageInterface<TPayloadReadType, TPayloadWriteType> : INetworkMessageInterface<TPayloadReadType, TPayloadWriteType>
		where TPayloadWriteType : class 
		where TPayloadReadType : class
	{
		/// <summary>
		/// The pipelines socket connection.
		/// </summary>
		private IWebSocketConnection Connection { get; }

		/// <summary>
		/// The messages service container.
		/// </summary>
		protected SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> MessageServices { get; }

		/// <summary>
		/// The details of the session.
		/// </summary>
		protected NetworkConnectionOptions NetworkOptions { get; }

		private AsyncLock PayloadWriteLock { get; } = new AsyncLock();

		public SocketConnectionNetworkMessageInterface(NetworkConnectionOptions networkOptions,
			IWebSocketConnection connection, 
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			MessageServices = messageServices ?? throw new ArgumentNullException(nameof(messageServices));
			NetworkOptions = networkOptions ?? throw new ArgumentNullException(nameof(networkOptions));
		}

		/// <inheritdoc />
		public async Task<NetworkIncomingMessage<TPayloadReadType>> ReadMessageAsync(CancellationToken token = default)
		{
			if (NetworkOptions.MaximumPacketHeaderSize != NetworkOptions.MinimumPacketHeaderSize)
				throw new NotSupportedException($"TODO: Support variable size packet header sizes for websockets.");

			while (!token.IsCancellationRequested 
			       && Connection.State == WebSocketState.Open)
			{
				var headerBuffer = ArrayPool<byte>.Shared.Rent(NetworkOptions.MinimumPacketHeaderSize);
				IPacketHeader header;
				int headerBytesRead;
				try
				{
					await ReadUntilBufferFullAsync(headerBuffer, NetworkOptions.MinimumPacketHeaderSize, token);
					header = ReadIncomingPacketHeader(new ReadOnlySequence<byte>(headerBuffer, 0, NetworkOptions.MinimumPacketHeaderSize), out headerBytesRead);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(headerBuffer);
				}

				//TODO: This is the best way to check for 0 length payload?? Seems hacky.
				//There is a special case when a packet is equal to the head size
				//meaning for example in the case of a 4 byte header then the packet is 4 bytes.
				//in this case we SHOULD not read anything. All the data exists already for the packet.
				if (header.PacketSize == headerBytesRead)
				{
					//The header is the entire packet, so empty buffer!
					TPayloadReadType payload = ReadIncomingPacketPayload(ReadOnlySequence<byte>.Empty, header);

					return new NetworkIncomingMessage<TPayloadReadType>(header, payload);
				}

				// Sanity check
				if (header.PayloadSize >= NetworkOptions.MaximumPayloadSize)
					throw new InvalidOperationException($"Encountered Payload with Size: {header.PayloadSize} greater than Max: {NetworkOptions.MaximumPayloadSize}");

				var payloadBuffer = ArrayPool<byte>.Shared.Rent(header.PayloadSize);
				try
				{
					await ReadUntilBufferFullAsync(payloadBuffer, header.PayloadSize, token);

					//TODO: Valid incoming packet lengths to avoid a stack overflow.
					//This point we have a VALID read result that is NOT less than header.PayloadSize
					//therefore it should be safe now to read the incoming packet.
					TPayloadReadType payload = ReadIncomingPacketPayload(new ReadOnlySequence<byte>(payloadBuffer, 0, header.PayloadSize), header);

					return new NetworkIncomingMessage<TPayloadReadType>(header, payload);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(payloadBuffer);
				}
			}

			return null;
		}

		private async Task ReadUntilBufferFullAsync(byte[] buffer, int bufferSize, CancellationToken token)
		{
			await Connection.ReceiveAsync(buffer, bufferSize, token);
		}

		/// <summary>
		/// Reads an incoming packet payload the <see cref="ReadResult"/> result.
		/// Default just reads it from the buffer but special handling for some users may be required.
		/// Therefore it is virtual, and the reading buffer logic can be overriden.
		/// </summary>
		/// <param name="result">The incoming read buffer.</param>
		/// <param name="header">The header that matches the payload type.</param>
		/// <returns></returns>
		protected virtual TPayloadReadType ReadIncomingPacketPayload(in ReadOnlySequence<byte> result, IPacketHeader header)
		{
			//Special case for zero-sized payload buffer
			if (result.IsEmpty)
			{
				int offset = 0;
				return MessageServices.MessageDeserializer.Deserialize(Span<byte>.Empty, ref offset);
			}

			//I opted to do this instead of stack alloc because of HUGE dangers in stack alloc and this is pretty efficient
			//buffer usage anyway.
			byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(header.PayloadSize);
			Span<byte> buffer = new Span<byte>(rentedBuffer, 0, header.PayloadSize);

			try
			{
				//This copy is BAD but it really avoids a lot of API headaches
				result.Slice(0, header.PayloadSize).CopyTo(buffer);

				int offset = 0;
				return MessageServices.MessageDeserializer.Deserialize(buffer, ref offset);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(rentedBuffer);
			}
		}

		private IPacketHeader ReadIncomingPacketHeader(ReadOnlySequence<byte> buffer, out int bytesRead)
		{
			//The implementation MUST be that this can be trusted to be the EXACT size of binary data that will be read.
			bytesRead = MessageServices.PacketHeaderFactory.ComputeHeaderSize(buffer);
			return DeserializePacketHeader(buffer, bytesRead);
		}

		/// <summary>
		/// Method should deserialize a <see cref="IPacketHeader"/> object based on the input buffer.
		/// </summary>
		/// <param name="buffer">The data buffer containing the header.</param>
		/// <param name="exactHeaderByteCount"></param>
		/// <returns></returns>
		protected virtual IPacketHeader DeserializePacketHeader(ReadOnlySequence<byte> buffer, int exactHeaderByteCount)
		{
			IPacketHeader header;
			using (var context = new PacketHeaderCreationContext(buffer, exactHeaderByteCount))
				header = MessageServices.PacketHeaderFactory.Create(context);

			return header;
		}

		/// <inheritdoc />
		public async Task<SendResult> SendMessageAsync(TPayloadWriteType message, CancellationToken token = default)
		{
			if (Connection.State != WebSocketState.Open)
				return SendResult.Disconnected;

			//THIS IS CRITICAL, IT'S NOT SAFE TO SEND MULTIPLE THREADS AT ONCE!!
			using (await PayloadWriteLock.LockAsync(token))
			{
				try
				{
					await WriteOutgoingMessageAsync(message, token);
				}
				catch (Exception e)
				{
					//TODO: Logging!
					return SendResult.Error;
				}

				return SendResult.Sent;
			}
		}

		private async Task WriteOutgoingMessageAsync(TPayloadWriteType payload, CancellationToken token = default)
		{
			if(payload == null) throw new ArgumentNullException(nameof(payload));

			//TODO: We should find a way to predict the size of a payload type.
			var buffer = ArrayPool<byte>.Shared.Rent(NetworkOptions.MaximumPacketSize);
			try
			{
				WritePacketToBuffer(payload, buffer, out var headerSize, out var payloadSize);
				await Connection.SendAsync(new ArraySegment<byte>(buffer, 0, headerSize + payloadSize), true, token);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		void WritePacketToBuffer(TPayloadWriteType payload, byte[] buffer, out int headerSize, out int payloadSize)
		{
			var bufferSpan = new Span<byte>(buffer);

			//It seems backwards, but we don't know what header to build until the payload is serialized.
			payloadSize = SerializeOutgoingPacketPayload(bufferSpan.Slice(NetworkOptions.MinimumPacketHeaderSize), payload);
			headerSize = SerializeOutgoingHeader(payload, payloadSize, bufferSpan.Slice(0, NetworkOptions.MaximumPacketHeaderSize));

			//TODO: We must eventually support VARIABLE LENGTH packet headers. This is complicated, WoW does this for large packets sent by the server.
			if (headerSize != NetworkOptions.MinimumPacketHeaderSize)
				throw new NotSupportedException($"TODO: Variable length packet header sizes are not yet supported.");
		}

		private int SerializeOutgoingHeader(TPayloadWriteType payload, int payloadSize, in Span<byte> buffer)
		{
			int headerOffset = 0;
			MessageServices.HeaderSerializer.Serialize(new PacketHeaderSerializationContext<TPayloadWriteType>(payload, payloadSize), buffer, ref headerOffset);
			return headerOffset;
		}

		/// <summary>
		/// Writes the outgoing packet payload.
		/// Returns the number of bytes the payload was sent as.
		/// </summary>
		/// <param name="buffer">The buffer to write the packet payload to.</param>
		/// <param name="payload">The payload instance.</param>
		/// <returns>The number of bytes the payload was sent as.</returns>
		private int SerializeOutgoingPacketPayload(in Span<byte> buffer, TPayloadWriteType payload)
		{
			//Serializes the payload data to the span buffer and moves the pipe forward by the ref output offset
			//meaning we indicate to the pipeline that we've written bytes
			int offset = 0;
			MessageServices.MessageSerializer.Serialize(payload, buffer, ref offset);
			return offset;
		}
	}
}
