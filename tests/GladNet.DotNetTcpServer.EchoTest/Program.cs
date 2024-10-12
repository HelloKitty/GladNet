using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;

namespace GladNet
{
	class Program
	{
		public static IPAddress Address { get; } = IPAddress.Parse("127.0.0.1");

		static async Task Main(string[] args)
		{
			ILog logger = new ConsoleLogger(LogLevel.All, true);
			logger.Info($"Starting server.");

			await new TCPEchoGladNetServerApplication(new NetworkAddressInfo(Address, 6969), logger)
				.BeginListeningAsync();
		}
	}
}
