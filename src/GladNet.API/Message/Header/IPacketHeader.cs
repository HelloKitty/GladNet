using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Contract for packet headers.
	/// </summary>
	public interface IPacketHeader
	{
		/// <summary>
		/// The size of the packet.
		/// </summary>
		int PacketSize { get; }

		//The PacketSize contains the whole size of the packet
		//So the payload is just the size minus the size of the packetsize field.
		/// <summary>
		/// Indicates the size of the payload.
		/// </summary>
		int PayloadSize { get; }
	}
}
