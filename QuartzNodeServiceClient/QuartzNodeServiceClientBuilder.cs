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
		public static void AddQuartzNodeServiceClient(string serviceName, string serverName, WebApplicationBuilder webApplicationBuilder, IConfiguration config)
		{

			var serviceID = config.GetValue<string>("QuartzNodeService:ServiceID");
			var apiKey = webApplicationBuilder.Configuration.GetValue<string>("QuartzNodeService:ApiKey");
			if (string.IsNullOrEmpty(apiKey))
				throw new Exception("API Key is not configured in appsettings.");

            var hotConfig = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

            var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            webApplicationBuilder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
                 .MinimumLevel.ControlledBy(logLevelSwitch)
                 .ReadFrom.Configuration(hotConfig)
				 .Enrich.FromLogContext()
				 .WriteTo.Console());

            webApplicationBuilder.Services.AddSingleton(sp => new ApiKeyProvider(apiKey, serviceID, serviceName, serverName));
			webApplicationBuilder.Services.AddSingleton<GrpcChannelProvider>();
			webApplicationBuilder.Services.AddSingleton<IQuartzSchedulerEngine, QuartzSchedulerEngine>();
			webApplicationBuilder.Services.AddSingleton<IQuartzNodeGrpcApi, QuartzNodeGrpcApi>();
			webApplicationBuilder.Services.AddHostedService<QuartzNodeGrpcHostedService>();
		}
	}
}