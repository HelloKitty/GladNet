using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Context for header serialization/creation from a <see cref="Payload"/>.
	/// For creation from binary see <see cref="IPacketHeaderFactory"/>
	/// </summary>
	/// <typeparam name="TPayloadType"></typeparam>
	public sealed class PacketHeaderSerializationContext<TPayloadType>
		where TPayloadType : class
	{
		/// <summary>
		/// The packet payload instance/object.
		/// </summary>
		public TPayloadType Payload { get; }

		/// <summary>
		/// The binary size of the <see cref="Payload"/>.
		/// </summary>
		public int PayloadSize { get; }

		public PacketHeaderSerializationContext(TPayloadType payload, int payloadSize)
		{
			if (payloadSize < 0) throw new ArgumentOutOfRangeException(nameof(payloadSize));
			Payload = payload ?? throw new ArgumentNullException(nameof(payload));
			PayloadSize = payloadSize;
		}
	}
}
