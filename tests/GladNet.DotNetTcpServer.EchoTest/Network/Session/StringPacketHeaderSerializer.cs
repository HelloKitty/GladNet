using System;
using System.Collections.Generic;
using System.Text;
using Reinterpret.Net;

namespace GladNet
{
	public sealed class StringPacketHeaderSerializer : IMessageSerializer<PacketHeaderSerializationContext<string>>
	{
		public void Serialize(PacketHeaderSerializationContext<string> value, Span<byte> buffer, ref int offset)
		{
			//2 byte size
			short size = (short)value.PayloadSize;

			size.Reinterpret()
				.CopyTo(buffer.Slice(offset));

			offset += 2;
		}
	}
}
