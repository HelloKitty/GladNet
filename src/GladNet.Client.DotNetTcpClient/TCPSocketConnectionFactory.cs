using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Glader.Essentials;
using Pipelines.Sockets.Unofficial;

namespace GladNet
{
	/// <summary>
	/// Factory that creates <see cref="SocketConnection"/>.
	/// Warning: You are responsible for calling Dispose.
	/// </summary>
	public sealed class TCPSocketConnectionFactory : IFactoryCreatable<SocketConnection, EmptyFactoryContext>
	{
		/// <inheritdoc />
		public SocketConnection Create(EmptyFactoryContext context)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			Socket client = new Socket(SocketType.Stream, ProtocolType.Tcp);

			// We cannot allow default memory pool usage, see: https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeReader.cs#L596
			// It can be seen that this will opt to use default array pool which will be TlsOverPerCoreLockedStacksArrayPool which can have significant contention
			//Let the caller connect if they want to.
			//await client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969));
			PipeOptions sendOptions = new PipeOptions(new GladNetPipeMemoryPool());

			return SocketConnection.Create(client, sendOptions, PipeOptions.Default, SocketConnectionOptions.ZeroLengthReads);
		}
	}
}
