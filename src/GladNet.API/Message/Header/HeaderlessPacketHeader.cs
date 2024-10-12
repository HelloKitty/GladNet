using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Header for a packet that doesn't contain an actual header.
	/// </summary>
	public sealed class HeaderlessPacketHeader : IPacketHeader
	{
		/// <inheritdoc />
		public int PacketSize { get; }

		//Packet size is the same as payload size in a headerless message.
		/// <inheritdoc />
		public int PayloadSize => PacketSize;

		/// <inheritdoc />
		public HeaderlessPacketHeader(int packetSize)
		{
			if(packetSize < 0) throw new ArgumentOutOfRangeException(nameof(packetSize));

			PacketSize = packetSize;
		}
	}
}
