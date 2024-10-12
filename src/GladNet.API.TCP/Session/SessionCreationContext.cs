using System;
using System.Collections.Generic;
using System.Text;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	public sealed class SessionCreationContext
	{
		/// <summary>
		/// The underlying connection object for the session.
		/// </summary>
		public SocketConnection Connection { get; }

		/// <summary>
		/// The details of the session.
		/// </summary>
		public SessionDetails Details { get; }

		public SessionCreationContext(SocketConnection connection, SessionDetails details)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			Details = details ?? throw new ArgumentNullException(nameof(details));
		}
	}
}
