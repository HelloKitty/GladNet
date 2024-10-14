using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GladNet;
using JetBrains.Annotations;

namespace GladNet
{
	/// <summary>
	/// Mostly copied from GladNet, it's just a direct serializer and send implementation of <see cref="IMessageSendService{TPayloadWriteType}"/>
	/// that avoids the queueing. This is needed because the send throughput is poor and sending many packets at once will cause stalls.
	/// </summary>
	/// <typeparam name="TPayloadWriteType">The type of the payload to serialize and send.</typeparam>
	public sealed class WebGLMessageSendService<TPayloadWriteType> : IMessageSendService<TPayloadWriteType>
		where TPayloadWriteType : class
	{
		private IWebSocketConnection Connection { get; }

		private NetworkConnectionOptions NetworkOptions { get; }

		private SessionMessageBuildingServiceContext<TPayloadWriteType, TPayloadWriteType> MessageServices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WebGLMessageSendService{TPayloadWriteType}"/> class.
		/// </summary>
		/// <param name="connection">The WebSocket connection used to send messages.</param>
		/// <param name="networkOptions">Network configuration options.</param>
		/// <param name="messageServices">Message and header serialization services.</param>
		public WebGLMessageSendService([NotNull] IWebSocketConnection connection,
			[NotNull] NetworkConnectionOptions networkOptions,
			[NotNull] SessionMessageBuildingServiceContext<TPayloadWriteType, TPayloadWriteType> messageServices)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			NetworkOptions = networkOptions ?? throw new ArgumentNullException(nameof(networkOptions));
			MessageServices = messageServices ?? throw new ArgumentNullException(nameof(messageServices));
		}

		/// <inheritdoc />
		public async Task<SendResult> SendMessageAsync(TPayloadWriteType message, CancellationToken token = default)
		{
			var buffer = NetworkOptions.PacketArrayPool.Rent(NetworkOptions.MaximumPacketSize);
			try
			{
				WritePacketToBuffer(message, buffer, out var headerSize, out var payloadSize);
				await Connection.SendAsync(new ArraySegment<byte>(buffer, 0, headerSize + payloadSize), true, token);
			}
			finally
			{
				NetworkOptions.PacketArrayPool.Return(buffer);
			}

			return SendResult.Sent;
		}

		/// <summary>
		/// Writes the message payload and header to the provided buffer.
		/// </summary>
		/// <param name="payload">The message payload to serialize.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="headerSize">The size of the serialized header.</param>
		/// <param name="payloadSize">The size of the serialized payload.</param>
		private void WritePacketToBuffer(TPayloadWriteType payload, byte[] buffer, out int headerSize, out int payloadSize)
		{
			var bufferSpan = new Span<byte>(buffer);

			// It seems backwards, but we don't know what header to build until the payload is serialized.
			payloadSize = SerializeOutgoingPacketPayload(bufferSpan.Slice(NetworkOptions.MinimumPacketHeaderSize), payload);
			headerSize = SerializeOutgoingHeader(payload, payloadSize, bufferSpan.Slice(0, NetworkOptions.MaximumPacketHeaderSize));

			// TODO: We must eventually support VARIABLE LENGTH packet headers. This is complicated, WoW does this for large packets sent by the server.
			if(headerSize != NetworkOptions.MinimumPacketHeaderSize)
				throw new NotSupportedException($"TODO: Variable length packet header sizes are not yet supported.");
		}

		/// <summary>
		/// Serializes the outgoing packet payload.
		/// </summary>
		/// <param name="buffer">The buffer to write the payload to.</param>
		/// <param name="payload">The payload to serialize.</param>
		/// <returns>The size of the serialized payload.</returns>
		private int SerializeOutgoingPacketPayload(in Span<byte> buffer, TPayloadWriteType payload)
		{
			//Serializes the payload data to the span buffer and moves the pipe forward by the ref output offset
			//meaning we indicate to the pipeline that we've written bytes
			int offset = 0;
			MessageServices.MessageSerializer.Serialize(payload, buffer, ref offset);
			return offset;
		}

		/// <summary>
		/// Serializes the outgoing packet header.
		/// </summary>
		/// <param name="payload">The payload being sent.</param>
		/// <param name="payloadSize">The size of the payload.</param>
		/// <param name="buffer">The buffer to write the header to.</param>
		/// <returns>The size of the serialized header.</returns>
		private int SerializeOutgoingHeader(TPayloadWriteType payload, int payloadSize, in Span<byte> buffer)
		{
			int headerOffset = 0;
			MessageServices.HeaderSerializer.Serialize(new PacketHeaderSerializationContext<TPayloadWriteType>(payload, payloadSize), buffer, ref headerOffset);
			return headerOffset;
		}
	}
}
