using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Contract for types that can serializes an instance of <see cref="TMessageType"/>
	/// into the provided binary buffers.
	/// </summary>
	/// <typeparam name="TMessageType">The serializable message type supported.</typeparam>
	public interface IMessageSerializer<in TMessageType>
		where TMessageType : class
	{
		/// <summary>
		/// Serializes an instance of <typeparamref name="TMessageType"/> into the provided buffer.
		/// Offset should indicate how far offset the buffer now is.
		/// </summary>
		/// <param name="value">The value to serialize.</param>
		/// <param name="buffer">The buffer.</param>
		/// <param name="offset">The offset into the buffer to start with (and should represent the ending position too).</param>
		/// <returns></returns>
		void Serialize(TMessageType value, Span<byte> buffer, ref int offset);
	}
}
