using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GladNet
{
	/// <summary>
	/// Contract for server application types that are listenable.
	/// </summary>
	public interface IServerApplicationListenable
	{
		/// <summary>
		/// Begins the listening process of a server application.
		/// This is a long running method that likely won't return until application shutdown.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>Awaitable</returns>
		Task BeginListeningAsync(CancellationToken token = default);
	}
}
