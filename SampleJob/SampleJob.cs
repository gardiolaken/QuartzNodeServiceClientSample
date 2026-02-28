using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzNodeServiceClient;

namespace SampleJob
{
	public class FileImporter : OctJobBase
	{
		private ILogger<FileImporter> _logger;

		public FileImporter(ILogger<FileImporter> logger, IConfiguration config) : base(logger, config)
		{
			_logger = logger;
		}
		public Task Execute(IJobExecutionContext context)
		{
			_logger.LogInformation("SampleJob is executing at {time}", DateTimeOffset.Now);
			File.WriteAllBytes(@"C:\DATA\SampleJobExecuted.txt", System.Text.Encoding.UTF8.GetBytes($"SampleJob executed at {DateTimeOffset.Now}"));
			return Task.CompletedTask;
		}

		protected override void Process()
		{
			throw new NotImplementedException();
		}
	}
}
