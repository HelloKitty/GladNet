using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// <see cref="EventArgs"/> for an event involving a managed session.
	/// </summary>
	/// <typeparam name="TManagedSessionType">The managed session type.</typeparam>
	public class ManagedSessionContextualEventArgs<TManagedSessionType> : EventArgs
		where TManagedSessionType : ManagedSession
	{
		/// <summary>
		/// The managed session this event is associated with.
		/// </summary>
		public TManagedSessionType Session { get; }

		public ManagedSessionContextualEventArgs(TManagedSessionType session)
		{
			Session = session ?? throw new ArgumentNullException(nameof(session));
		}
	}
}
