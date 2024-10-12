using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GladNet;

namespace GladNet
{
	/// <summary>
	/// Contract for the context of a common GladNet message.
	/// </summary>
	public interface IPeerMessageContext<in TPayloadWriteType>
		where TPayloadWriteType : class
	{
		//Below you'll see a bunch of interfaces that the client
		//actually implements. However we don't have to generic to hell
		//all of the code. It becomes too cumbersome to understand and consume in that
		//case and leads to generic type parameter carrying, a design fault.

		/// <summary>
		/// The connection service that provides ways to disconnect and connect
		/// the client associated with this message connect.
		/// </summary>
		INetworkConnectionService ConnectionService { get; }

		/// <summary>
		/// The sending service that allows clients to send messages.
		/// </summary>
		IMessageSendService<TPayloadWriteType> MessageService { get; }
	}
}