using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	public class NetworkConnectionOptions
	{
		/// <summary>
		/// Indicates the configured maximum packet size.
		/// </summary>
		public int MaximumPacketSize => MaximumPacketHeaderSize + MaximumPayloadSize;

		/// <summary>
		/// Indicates the configured minimum required size of a packet header.
		/// Some protocols have variable length headers such as World of Warcraft.
		/// </summary>
		public int MinimumPacketHeaderSize { get; }

		/// <summary>
		/// Indicates the configured maximum required size of a packet header.
		/// Some protocols have variable length headers such as World of Warcraft.
		/// </summary>
		public int MaximumPacketHeaderSize { get; }

		/// <summary>
		/// Indicates the configured maximum packet payload size.
		/// </summary>
		public int MaximumPayloadSize { get; }

		public NetworkConnectionOptions()
		{
			MaximumPayloadSize = NetworkConnectionOptionsConstants.DEFAULT_MAXIMUM_PACKET_PAYLOAD_SIZE;
			MinimumPacketHeaderSize = NetworkConnectionOptionsConstants.DEFAULT_MINIMUM_PACKET_HEADER_SIZE;

			//TODO: Don't use same header size
			MaximumPacketHeaderSize = MinimumPacketHeaderSize;
		}

		public NetworkConnectionOptions(int minimumPacketHeaderSize, int maximumPacketHeaderSize, int maximumPayloadSize)
		{
			MinimumPacketHeaderSize = minimumPacketHeaderSize;
			MaximumPacketHeaderSize = maximumPacketHeaderSize;
			MaximumPayloadSize = maximumPayloadSize;
		}
	}
}
