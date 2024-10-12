using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	/// <summary>
	/// TCP <see cref="SocketConnection"/> Pipelines-based implementation of <see cref="INetworkMessageInterface{TPayloadReadType,TPayloadWriteType}"/>
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
		private SocketConnection Connection { get; }

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
			SocketConnection connection, 
			SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> messageServices)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			MessageServices = messageServices ?? throw new ArgumentNullException(nameof(messageServices));
			NetworkOptions = networkOptions ?? throw new ArgumentNullException(nameof(networkOptions));
		}

		/// <inheritdoc />
		public async Task<NetworkIncomingMessage<TPayloadReadType>> ReadMessageAsync(CancellationToken token = default)
		{
			while (!token.IsCancellationRequested)
			{
				ReadResult result;

				//The reason we wrap this in a try/catch is because the Pipelines may abort ungracefully
				//inbetween our state checks. Therefore only calling ReadAsyc can truly indicate the state of a connection
				//and if it has been aborted then we should pretend as if we read nothing.
				try
				{
					result = await Connection.Input.ReadAsync(token);
				}
				catch(ConnectionAbortedException abortException)
				{
					return null;
				}

				if (!IsReadResultValid(in result))
					return null;

				ReadOnlySequence<byte> buffer = result.Buffer;

				//So we have a valid result, let's check if we have enough data.
				//If we don't have enough to read a packet header then we need to wait until we have enough bytes.
				if(buffer.Length < NetworkOptions.MinimumPacketHeaderSize)
				{
					//If buffer isn't large enough we need to tell Pipeline we didn't consume anything
					//but we DID inspect/examine all the way to the end of the buffer.
					Connection.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
					continue;
				}

				if(!MessageServices.PacketHeaderFactory.IsHeaderReadable(in buffer))
				{
					//If buffer isn't large enough we need to tell Pipeline we didn't consume anything
					//but we DID inspect/examine all the way to the end of the buffer.
					Connection.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
					continue;
				}

				IPacketHeader header = ReadIncomingPacketHeader(Connection.Input, in result, out int headerBytesRead);

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

				//TODO: Add header validation.
				while(!token.IsCancellationRequested)
				{
					//The reason we wrap this in a try/catch is because the Pipelines may abort ungracefully
					//inbetween our state checks. Therefore only calling ReadAsyc can truly indicate the state of a connection
					//and if it has been aborted then we should pretend as if we read nothing.
					try
					{
						//Now with the header we know how much data we must now read for the payload.
						result = await Connection.Input.ReadAsync(token);
					}
					catch(ConnectionAbortedException abortException)
					{
						return null;
					}

					//This call will also use the cancel token so we don't need to check it in the nested-loop.
					if (!IsReadResultValid(in result))
						return null;

					//This is dumb and hacky, but we need to know if we shall need to read again
					//This means we're still at the START of the buffer (haven't read anything)
					//but have technically aware/inspected to the END position.
					if (!IsPayloadReadable(result, header))
						Connection.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
					else
						break;
				}

				try
				{
					//TODO: Valid incoming packet lengths to avoid a stack overflow.
					//This point we have a VALID read result that is NOT less than header.PayloadSize
					//therefore it should be safe now to read the incoming packet.
					TPayloadReadType payload = ReadIncomingPacketPayload(result.Buffer, header);

					return new NetworkIncomingMessage<TPayloadReadType>(header, payload);
				}
				catch(Exception)
				{
					//TODO: We should log WHY but basically we should no longer continue the network listener.
					return null;
				}
				finally
				{
					//So serialization should output an offset read. However, we should not use
					//that as the basis for the bytes read. Serialization can be WRONG and conflict with
					//the packet header's defined size. Therefore, we should trust packet header over serialization
					//logic ALWAYS and this advance should be in a finally block for sure.
					Connection.Input.AdvanceTo(result.Buffer.GetPosition(ComputeIncomingPayloadBytesRead(header)));
				}
			}

			return null;
		}

		/// <summary>
		/// Computes how many bytes will have been read for the payload given the <see cref="IPacketHeader"/>.
		/// Simple implementation is Payload Size. However some implementations use blocks so some additional discarded
		/// block buffer data may be required.
		/// Implementers can override this can adjust the calculation.
		/// </summary>
		/// <param name="header">The packet header instance.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual int ComputeIncomingPayloadBytesRead(IPacketHeader header)
		{
			if (header == null) throw new ArgumentNullException(nameof(header));

			return header.PayloadSize;
		}

		/// <summary>
		/// Indicates if a payload is readable from the <see cref="ReadResult"/> result.
		/// Default is to indicate it's readable if there is enough bytes available to read the payload.
		/// Some protocols required BLOCK/CHUNKs so this is virtual and implementers can specify how much is required.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="header"></param>
		/// <returns></returns>
		protected virtual bool IsPayloadReadable(ReadResult result, IPacketHeader header)
		{
			return result.Buffer.Length >= header.PayloadSize;
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

		private IPacketHeader ReadIncomingPacketHeader(PipeReader reader, in ReadResult result, out int bytesRead)
		{
			int exactHeaderByteCount = 0;
			try
			{
				//The implementation MUST be that this can be trusted to be the EXACT size of binary data that will be read.
				bytesRead = exactHeaderByteCount = MessageServices.PacketHeaderFactory.ComputeHeaderSize(result.Buffer);
				return DeserializePacketHeader(result.Buffer, exactHeaderByteCount);
			}
			finally
			{
				//Advance to only the exact header bytes read, consumed and examined. Do not use buffer lengths ever!
				reader.AdvanceTo(result.Buffer.GetPosition(exactHeaderByteCount));
			}
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

		private bool IsReadResultValid(in ReadResult result)
		{
			//TODO: Does this mean it's DONE??
			if(result.IsCanceled || result.IsCompleted)
			{
				//This means we CONSUMED to end of buffer and INSPECTED to end of buffer
				//We're DONE with all read buffer data.
				Connection.Input.AdvanceTo(result.Buffer.End);
				return false;
			}

			return true;
		}

		/// <inheritdoc />
		public async Task<SendResult> SendMessageAsync(TPayloadWriteType message, CancellationToken token = default)
		{
			if(!Connection.Socket.Connected)
				return SendResult.Disconnected;

			//THIS IS CRITICAL, IT'S NOT SAFE TO SEND MULTIPLE THREADS AT ONCE!!
			using (await PayloadWriteLock.LockAsync(token))
			{
				try
				{
					WriteOutgoingMessage(message);

					//To understand the purpose of Flush when pipelines is using sockets see Marc's comments here: https://stackoverflow.com/questions/56481746/does-pipelines-sockets-unofficial-socketconnection-ever-flush-without-a-request
					//Basically, "it makes sure that a consumer is awakened (if it isn't already)" and "if there is back-pressure, it delays the producer until the consumer has cleared some of the back-pressure"
					FlushResult result = await Connection.Output.FlushAsync(token);

					if(!IsFlushResultValid(in result))
						return SendResult.Error;
				}
				catch (Exception e)
				{
					//TODO: Logging!
					return SendResult.Error;
				}

				return SendResult.Sent;
			}
		}

		private void WriteOutgoingMessage(TPayloadWriteType payload)
		{
			if(payload == null) throw new ArgumentNullException(nameof(payload));

			//TODO: We should find a way to predict the size of a payload type.
			Span<byte> buffer = Connection.Output.GetSpan(NetworkOptions.MaximumPacketSize);

			//It seems backwards, but we don't know what header to build until the payload is serialized.
			int payloadSize = SerializeOutgoingPacketPayload(buffer.Slice(NetworkOptions.MinimumPacketHeaderSize), payload);
			int headerSize = SerializeOutgoingHeader(payload, payloadSize, buffer.Slice(0, NetworkOptions.MaximumPacketHeaderSize));

			//TODO: We must eventually support VARIABLE LENGTH packet headers. This is complicated, WoW does this for large packets sent by the server.
			if(headerSize != NetworkOptions.MinimumPacketHeaderSize)
				throw new NotSupportedException($"TODO: Variable length packet header sizes are not yet supported.");

			int length = OnBeforePacketBufferSend(buffer, payloadSize + headerSize);

			Connection.Output.Advance(length);
		}

		/// <summary>
		/// Allows implementers to override length handling for an outgoing buffer.
		/// Additionally allows them to mutate the contents of the buffer right before it is sent.
		/// Especially helpful for dealing with block-based networking.
		/// </summary>
		/// <param name="buffer">The outgoing buffer.</param>
		/// <param name="length">The true outgoing length.</param>
		/// <returns>The length of the buffer to write.</returns>
		protected virtual int OnBeforePacketBufferSend(in Span<byte> buffer, int length)
		{
			return length;
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

		private bool IsFlushResultValid(in FlushResult result)
		{
			//TODO: Does this mean it's DONE??
			if(result.IsCanceled || result.IsCompleted)
				return false;

			return true;
		}
	}
}
