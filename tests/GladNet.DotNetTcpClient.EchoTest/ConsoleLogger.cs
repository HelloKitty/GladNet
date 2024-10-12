using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using Common.Logging.Simple;

namespace GladNet
{
	public sealed class ConsoleLogger : AbstractSimpleLogger
	{
		public ConsoleLogger(LogLevel logLevel, bool showlevel) 
			: base(nameof(ConsoleLogger), logLevel, showlevel, false, false, String.Empty)
		{

		}

		protected override void WriteInternal(LogLevel level, object message, Exception exception)
		{
			if (ShowLevel)
			{
				if(exception != null)
					Console.WriteLine($"{level}: {message.ToString()}. Error: {exception}");
				else
					Console.WriteLine($"{level}: {message.ToString()}.");
			}
			else
			{
				if(exception != null)
					Console.WriteLine($"{message.ToString()}. Error: {exception}");
				else
					Console.WriteLine($"{message.ToString()}.");
			}
		}
	}
}
