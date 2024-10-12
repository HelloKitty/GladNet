using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Glader.Essentials;

namespace GladNet
{
	/// <summary>
	/// Contract for factories that can build packet headers from <see cref="Span{T}"/>
	/// </summary>
	public interface IPacketHeaderFactory : IFactoryCreatable<IPacketHeader, PacketHeaderCreationContext>
	{
		/// <summary>
		/// Indicates if a header is readable from the provided buffer.
		/// (Ex. not enough bytes, header is not readable.)
		/// </summary>
		/// <param name="buffer">The buffer to inspect.</param>
		/// <returns>True if a header is readable.</returns>
		bool IsHeaderReadable(in Span<byte> buffer);

		/// <summary>
		/// Indicates if a header is readable from the provided buffer.
		/// (Ex. not enough bytes, header is not readable.)
		/// </summary>
		/// <param name="buffer">The buffer to inspect.</param>
		/// <returns>True if a header is readable.</returns>
		bool IsHeaderReadable(in ReadOnlySequence<byte> buffer);

		/// <summary>
		/// Computes the binary size of a header given the buffer.
		/// This exists so that <see cref="Create"/> does not need to indicate
		/// how many bytes were read via an out parameter.
		/// </summary>
		/// <param name="buffer">The buffer to inspect.</param>
		/// <returns>The binary size of the header.</returns>
		int ComputeHeaderSize(in ReadOnlySequence<byte> buffer);
	}
}
