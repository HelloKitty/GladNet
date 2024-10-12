using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Unified contract for <see cref="INetworkConnectable"/> and
	/// <see cref="INetworkDisconnectable"/> making it easier to consume.
	/// </summary>
	public interface INetworkConnectionService : INetworkDisconnectable, INetworkConnectable
	{
		/// <summary>
		/// Indicates if a connection is established.
		/// </summary>
		bool isConnected { get; }
	}
}
