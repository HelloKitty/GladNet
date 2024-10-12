using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glader.Essentials;

namespace GladNet
{
	public sealed class DefaultStringMessageHandler : BaseDefaultMessageHandler<string, SessionMessageContext<string>>
	{
		public override async Task HandleMessageAsync(SessionMessageContext<string> context, string message, CancellationToken token = default)
		{
			Console.WriteLine($"Echoed Content: {message}");
		}
	}
}
