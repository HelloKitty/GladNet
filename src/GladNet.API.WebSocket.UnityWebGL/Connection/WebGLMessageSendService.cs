using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GladNet;
using JetBrains.Annotations;

namespace SwanSong
{
	/// <summary>
	/// Mostly copied from GladNet, it's just a direct serializer and send implementation of <see cref="IMessageSendService"/>
	/// that avoids the queueing.
	/// This is needed because the send throughput is poor and if sending many packets at once will cause stalls.
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	public sealed class WebGLMessageSendService<TPayloadWriteType> : IMessageSendService<TPayloadWriteType>
		where TPayloadWriteType : class
	{
		private IWebSocketConnection Connection { get; }

		private NetworkConnectionOptions NetworkOptions { get; }

		private SessionMessageBuildingServiceContext<TPayloadWriteType, TPayloadWriteType> MessageServices { get; }

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
			var buffer = ArrayPool<byte>.Shared.Rent(NetworkOptions.MaximumPacketSize);
			try
			{
				WritePacketToBuffer(message, buffer, out var headerSize, out var payloadSize);
				await Connection.SendAsync(new ArraySegment<byte>(buffer, 0, headerSize + payloadSize), true, token);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			return SendResult.Sent;
		}

		void WritePacketToBuffer(TPayloadWriteType payload, byte[] buffer, out int headerSize, out int payloadSize)
		{
			var bufferSpan = new Span<byte>(buffer);

			//It seems backwards, but we don't know what header to build until the payload is serialized.
			payloadSize = SerializeOutgoingPacketPayload(bufferSpan.Slice(NetworkOptions.MinimumPacketHeaderSize), payload);
			headerSize = SerializeOutgoingHeader(payload, payloadSize, bufferSpan.Slice(0, NetworkOptions.MaximumPacketHeaderSize));

			//TODO: We must eventually support VARIABLE LENGTH packet headers. This is complicated, WoW does this for large packets sent by the server.
			if(headerSize != NetworkOptions.MinimumPacketHeaderSize)
				throw new NotSupportedException($"TODO: Variable length packet header sizes are not yet supported.");
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

		private int SerializeOutgoingHeader(TPayloadWriteType payload, int payloadSize, in Span<byte> buffer)
		{
			int headerOffset = 0;
			MessageServices.HeaderSerializer.Serialize(new PacketHeaderSerializationContext<TPayloadWriteType>(payload, payloadSize), buffer, ref headerOffset);
			return headerOffset;
		}
	}
}