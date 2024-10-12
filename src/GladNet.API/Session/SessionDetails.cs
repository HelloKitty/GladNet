using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Contains information about the session.
	/// </summary>
	public sealed class SessionDetails
	{
		/// <summary>
		/// Address for the session.
		/// </summary>
		public NetworkAddressInfo Address { get; }

		/// <summary>
		/// Application specific identifier for the connection instance.
		/// </summary>
		public int ConnectionId { get; }

		public SessionDetails(NetworkAddressInfo address, int connectionId)
		{
			if(connectionId < 0) throw new ArgumentOutOfRangeException(nameof(connectionId));

			Address = address ?? throw new ArgumentNullException(nameof(address));
			ConnectionId = connectionId;
		}
	}
}
