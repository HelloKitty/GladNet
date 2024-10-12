using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	/// <summary>
	/// Contract for types that can deserialize an instance of <see cref="TMessageType"/>
	/// from the provided binary buffers.
	/// </summary>
	/// <typeparam name="TMessageType"></typeparam>
	public interface IMessageDeserializer<out TMessageType>
		where TMessageType : class
	{
		/// <summary>
		/// Deserializes an instance of <typeparamref name="TMessageType"/> from the provided buffer.
		/// Offset should indicate how far offset the buffer now is.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="offset">The offset into the buffer to start with (and should represent the ending position too).</param>
		/// <returns></returns>
		TMessageType Deserialize(Span<byte> buffer, ref int offset);
	}
}
