using Microsoft.Extensions.Hosting;
using QuartzNodeService.QuartzNodeGrpcApi;
using QuartzNodeService.QuartzSchedulerService;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace QuartzNodeServiceClient
{
	public static class QuartzNodeServiceClientBuilder
	{
		public static void AddQuartzNodeServiceClient(string serviceName, string serverName, IHostBuilder applicationBuilder, IConfiguration config)
		{

			var serviceID = config.GetValue<string>("QuartzNodeService:ServiceID");
			var apiKey = config.GetValue<string>("QuartzNodeService:ApiKey");
			if (string.IsNullOrEmpty(apiKey))
				throw new Exception("API Key is not configured in appsettings.");

            var hotConfig = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

            var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

						
            applicationBuilder.UseSerilog((context, loggerConfiguration) => loggerConfiguration
                 .MinimumLevel.ControlledBy(logLevelSwitch)
                 .ReadFrom.Configuration(hotConfig)
				 .Enrich.FromLogContext()
				 .WriteTo.Console());

			applicationBuilder.ConfigureServices(services =>
			{

				services.AddSingleton(sp => new ApiKeyProvider(apiKey, serviceID, serviceName, serverName));
				services.AddSingleton<GrpcChannelProvider>();
				services.AddSingleton<IQuartzSchedulerEngine, QuartzSchedulerEngine>();
				services.AddSingleton<IQuartzNodeGrpcApi, QuartzNodeGrpcApi>();
				services.AddHostedService<QuartzNodeGrpcHostedService>();
			});
		}
	}
}