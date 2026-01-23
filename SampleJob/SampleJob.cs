using Microsoft.Extensions.Logging;
using Quartz;

namespace SampleJob
{
	public class FileImporter : IJob
	{
		private ILogger<FileImporter> _logger;

		public FileImporter(ILogger<FileImporter> logger)
		{
			_logger = logger;
		}
		public Task Execute(IJobExecutionContext context)
		{
            _logger.LogInformation("SampleJob1 is executing at {time}", DateTimeOffset.Now);
            File.WriteAllBytes(@"C:\DATA\SampleJobExecuted.txt", System.Text.Encoding.UTF8.GetBytes($"SampleJob executed at {DateTimeOffset.Now}"));
			return Task.CompletedTask;
		}
	}
}
