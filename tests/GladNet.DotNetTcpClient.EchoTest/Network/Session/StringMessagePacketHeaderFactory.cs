using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GladNet
{
	public sealed class StringMessagePacketHeaderFactory : IPacketHeaderFactory
	{
		public IPacketHeader Create(PacketHeaderCreationContext context)
		{
			ushort size = Unsafe.As<byte, ushort>(ref context.GetSpan()[0]);

			return new HeaderlessPacketHeader(size);
		}

		public bool IsHeaderReadable(in Span<byte> buffer)
		{
			return buffer.Length >= 2;
		}

		public bool IsHeaderReadable(in ReadOnlySequence<byte> buffer)
		{
			return buffer.Length >= 2;
		}

		public int ComputeHeaderSize(in ReadOnlySequence<byte> buffer)
		{
			return 2;
		}
	}
}
