using ILogger = Serilog.ILogger;

namespace QuartzNodeService.Logger
{
	public class LoggingService
	{
		private ILogger Logger;

		public LoggingService(ILogger logger)
		{
			Logger = logger;
		}

		public void LogInformation(string message)
		{
            Logger.Information(message);
		}

		public void LogWarning(string message)
		{
			Logger.Warning(message);
		}

		public void LogError(string message)
		{
			Logger.Error(message);
		}

		public void LogDebug(string message)
		{
			Logger.Debug(message);
		}

		public void LogCritical(string message)
		{
			Logger.Fatal(message);
		}
	}

}
