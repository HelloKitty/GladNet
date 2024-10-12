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
	/// Full message for an incoming packet.
	/// </summary>
	public sealed class NetworkIncomingMessage<TPayloadType>
		where TPayloadType : class
	{
		/// <summary>
		/// The header for the incoming message.
		/// </summary>
		public IPacketHeader Header { get; }

		/// <summary>
		/// The deserialized payload for the incoming header.
		/// </summary>
		public TPayloadType Payload { get; }

		/// <summary>
		/// Creates a new incoming message envelope. Represents a complete
		/// logical packet including the header and the deserialized payload.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="payload">The payload.</param>
		public NetworkIncomingMessage(IPacketHeader header, TPayloadType payload)
		{
			if(header == null) throw new ArgumentNullException(nameof(header));
			if(payload == null) throw new ArgumentNullException(nameof(payload));

			Header = header;
			Payload = payload;
		}

		//Serializer ctor
		private NetworkIncomingMessage()
		{

		}
	}
}
