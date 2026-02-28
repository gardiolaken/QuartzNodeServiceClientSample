using Microsoft.Extensions.Logging;
using Quartz;

namespace SampleJob
{
	public class FileImporter2 : IJob
	{
		private ILogger<FileImporter2> _logger;

		public FileImporter2(ILogger<FileImporter2> logger)
		{
			_logger = logger;
		}
		public Task Execute(IJobExecutionContext context)
		{
			_logger.LogInformation("SampleJob2 is executing at {time}", DateTimeOffset.Now);
			File.WriteAllBytes(@"C:\DATA\SampleJobExecuted.txt", System.Text.Encoding.UTF8.GetBytes($"SampleJob executed at {DateTimeOffset.Now}"));
			return Task.CompletedTask;
		}
	}
}
