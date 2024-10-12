namespace GladNet
{
	/// <summary>
	/// Enumeration of results of trying to send
	/// a message.
	/// </summary>
	public enum SendResult
	{
		//TODO: Implement
		/// <summary>
		/// Indicates that the message has been enqueued for sending.
		/// The result can't be known, and would be only determined in the future.
		/// </summary>
		Enqueued = 0,

		/// <summary>
		/// Indicates that the message has been sent.
		/// </summary>
		Sent = 1,

		/// <summary>
		/// Indicates that the network is disconnected
		/// and the message cannot be sent.
		/// </summary>
		Disconnected = 2,

		/// <summary>
		/// Indicates an error was encountered.
		/// </summary>
		Error = 3
	}
}