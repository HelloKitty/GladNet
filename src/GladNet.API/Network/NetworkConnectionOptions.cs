using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Glader.Essentials;

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

		/// <summary>
		/// Retrieves an array pool specific to the options.
		/// Specifically <see cref="MaximumPacketSize"/>
		/// </summary>
		public ArrayPool<byte> PacketArrayPool => GetPacketArrayPool();

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

		private const int DefaultMaxArrayPoolArrayLength = 1024 * 1024;

		/// <summary>
		/// Determines which array pool to use for the network options.
		/// </summary>
		/// <returns>An array pool to use for the packet buffers.</returns>
		private ArrayPool<byte> GetPacketArrayPool()
		{
			// See: https://github.com/dotnet/runtime/blob/6221ddb3051463309801c9008f332b34361da798/src/libraries/System.Private.CoreLib/src/System/Buffers/ConfigurableArrayPool.cs#L12
			if (MaximumPacketSize >= DefaultMaxArrayPoolArrayLength)
			{
				return LargeArrayPool<byte>.Shared;
			}
			else
			{
				return ArrayPool<byte>.Shared;
			}
		}
	}
}
