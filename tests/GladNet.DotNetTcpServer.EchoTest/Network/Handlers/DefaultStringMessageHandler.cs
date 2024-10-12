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
			Console.WriteLine($"Message Content: {message}");

			//echos back the message to the client.
			await context.MessageService.SendMessageAsync(message, token);

			if (message.ToLower() == "quit")
				await context.ConnectionService.DisconnectAsync();
		}
	}
}
