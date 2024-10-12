using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Encapsulated object for a network address.
	/// </summary>
	public sealed class NetworkAddressInfo
	{
		/// <summary>
		/// The IPAddress of the network.
		/// </summary>
		public IPAddress AddressEndpoint { get; }

		/// <summary>
		/// The port of the network.
		/// </summary>
		public int Port { get; }

		/// <inheritdoc />
		public NetworkAddressInfo(IPAddress addressEndpoint, int port)
		{
			if(addressEndpoint == null) throw new ArgumentNullException(nameof(addressEndpoint));
			if(port <= 0) throw new ArgumentOutOfRangeException(nameof(port));

			AddressEndpoint = addressEndpoint;
			Port = port;
		}
	}
}
