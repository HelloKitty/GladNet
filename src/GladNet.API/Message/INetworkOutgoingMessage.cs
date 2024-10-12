using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// Contract for an outgoing network message.
	/// </summary>
	public interface INetworkOutgoingMessage<out THeaderType>
		where THeaderType : IPacketHeader
	{
		/// <summary>
		/// The header for the outgoing message.
		/// </summary>
		THeaderType Header { get; }

		/// <summary>
		/// The byte representation of the payload.
		/// </summary>
		Span<byte> Payload { get; }
	}
}
