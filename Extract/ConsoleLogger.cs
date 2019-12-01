using System;
using System.IO;
using uTinyRipper;

namespace Extract
{
	public class ConsoleLogger : ILogger, IDisposable
	{
		StreamWriter writer;
		public ConsoleLogger(string path)
		{
			var dir = Path.GetDirectoryName(path);
			if(dir != "") Directory.CreateDirectory(dir);
			writer = new StreamWriter(path);
			if (!RunetimeUtils.IsRunningOnMono && Console.LargestWindowWidth > 0)
			{
				Console.WindowWidth = (int)(Console.LargestWindowWidth * 0.8f);
				Console.BufferHeight = 2000;
			}
		}

		public void Dispose()
		{
			writer.Dispose();
		}

		public void Log(LogType type, LogCategory category, string message)
		{
#if !DEBUG
			if(category == LogCategory.Debug)
			{
				return;
			}
#endif

			ConsoleColor backColor = Console.BackgroundColor;
			ConsoleColor foreColor = Console.ForegroundColor;

			switch (type)
			{
				case LogType.Info:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;

				case LogType.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;

				case LogType.Warning:
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					break;

				case LogType.Error:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					break;
			}

			Console.WriteLine($"{category}: {message}");
			writer.WriteLine($"{category}: {message}");
			writer.Flush();
			Console.BackgroundColor = backColor;
			Console.ForegroundColor = foreColor;
		}
	}
}
