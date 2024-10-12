using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Encapsulates Requires session message building services.
	/// Mostly for serialization/building purposes.
	/// </summary>
	/// <typeparam name="TPayloadWriteType"></typeparam>
	/// <typeparam name="TPayloadReadType"></typeparam>
	public sealed class SessionMessageBuildingServiceContext<TPayloadReadType, TPayloadWriteType> 
		where TPayloadReadType : class 
		where TPayloadWriteType : class
	{
		/// <summary>
		/// The factory for building packet headers.
		/// </summary>
		public IPacketHeaderFactory PacketHeaderFactory { get; }

		/// <summary>
		/// The incoming message deserializer.
		/// </summary>
		public IMessageDeserializer<TPayloadReadType> MessageDeserializer { get; }

		/// <summary>
		/// The incoming message deserializer.
		/// </summary>
		public IMessageSerializer<TPayloadWriteType> MessageSerializer { get; }

		/// <summary>
		/// The outgoing message header serializer.
		/// </summary>
		public IMessageSerializer<PacketHeaderSerializationContext<TPayloadWriteType>> HeaderSerializer { get; }

		public SessionMessageBuildingServiceContext(IPacketHeaderFactory packetHeaderFactory, 
			IMessageDeserializer<TPayloadReadType> messageDeserializer, 
			IMessageSerializer<TPayloadWriteType> messageSerializer, 
			IMessageSerializer<PacketHeaderSerializationContext<TPayloadWriteType>> headerSerializer)
		{
			PacketHeaderFactory = packetHeaderFactory ?? throw new ArgumentNullException(nameof(packetHeaderFactory));
			MessageDeserializer = messageDeserializer ?? throw new ArgumentNullException(nameof(messageDeserializer));
			MessageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
			HeaderSerializer = headerSerializer ?? throw new ArgumentNullException(nameof(headerSerializer));
		}
	}
}
