using System;
using System.Collections.Generic;
using System.Text;

namespace GladNet
{
	public sealed class StringMessageSerializer : IMessageSerializer<string>, IMessageDeserializer<string>
	{
		public void Serialize(string value, Span<byte> buffer, ref int offset)
		{
			offset += Encoding.ASCII.GetBytes(value, buffer);
		}

		public string Deserialize(Span<byte> buffer, ref int offset)
		{
			if (offset != 0)
			{
				if(offset > buffer.Length)
					throw new InvalidOperationException($"Offset: {offset} outside of buffer Size: {buffer.Length}");

				buffer = buffer.Slice(offset);
			}

			string value = Encoding.ASCII.GetString(buffer);
			offset += buffer.Length * 1;
			return value;
		}
	}
}
