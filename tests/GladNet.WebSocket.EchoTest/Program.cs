using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GladNet.WebSocket.EchoTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			HttpListener httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://localhost:4999/");
			httpListener.Start();

			HttpListenerContext context = await httpListener.GetContextAsync();
			if(context.Request.IsWebSocketRequest)
			{
				HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
				System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;
				while(webSocket.State == WebSocketState.Open)
				{
					await Task.Delay(1);
				}
			}
		}
	}
}
