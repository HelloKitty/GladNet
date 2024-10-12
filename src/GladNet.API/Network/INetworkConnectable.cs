using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Contract for a network connectable type.
	/// </summary>
	public interface INetworkConnectable
	{
		/// <summary>
		/// Connects to the provided <see cref="ip"/> with on the given <see cref="port"/>.
		/// </summary>
		/// <param name="ip">The ip.</param>
		/// <param name="port">The port.</param>
		/// <returns>True if connection was successful (WARNING: May not return until connect disconnects).</returns>
		Task<bool> ConnectAsync(string ip, int port);
	}
}
