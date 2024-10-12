using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Contract for managed client session
	/// </summary>
	public interface IManagedSession
	{
		//TODO: We should create an event that can be subscribed to for disconnecting
		//TODO: Is this the best way to do this?
		/// <summary>
		/// Service that can be used for disconnecting the session.
		/// </summary>
		INetworkConnectionService ConnectionService { get; }

		/// <summary>
		/// The details of the session.
		/// </summary>
		SessionDetails Details { get; }
	}
}
