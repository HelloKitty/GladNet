using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Contract for a disconnectable type.
	/// </summary>
	public interface INetworkDisconnectable
	{
		/// <summary>
		/// Disconnects asyncronously to send or recieve remaining data.
		/// </summary>
		/// <returns>A wait.</returns>
		Task DisconnectAsync();
	}
}
