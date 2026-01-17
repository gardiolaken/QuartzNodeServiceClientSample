using Microsoft.Extensions.Hosting;
using QuartzNodeService.QuartzNodeGrpcApi;
using QuartzNodeService.QuartzSchedulerService;

namespace QuartzNodeServiceClient
{
	public static class QuartzNodeServiceClientBuilder
	{
		public static void AddQuartzNodeServiceClient(WebApplicationBuilder webApplicationBuilder, IConfiguration config)
		{

			var serviceID = config.GetValue<string>("ServiceID");
			var connectionString = webApplicationBuilder.Configuration.GetConnectionString("QuartzConnectionString");
			if (string.IsNullOrEmpty(connectionString))
				throw new Exception("Quartz database connection string is not configured in appsettings.");

			var apiKey = webApplicationBuilder.Configuration.GetValue<string>("ApiKey");
			if (string.IsNullOrEmpty(apiKey))
				throw new Exception("API Key is not configured in appsettings.");

			webApplicationBuilder.Services.AddSingleton(sp => new ApiKeyProvider(apiKey, serviceID));
			webApplicationBuilder.Services.AddSingleton<GrpcChannelProvider>();
			webApplicationBuilder.Services.AddSingleton<IQuartzSchedulerEngine, QuartzSchedulerEngine>();
			webApplicationBuilder.Services.AddSingleton<IQuartzNodeGrpcApi, QuartzNodeGrpcApi>();
			webApplicationBuilder.Services.AddHostedService<QuartzNodeGrpcHostedService>();
		}
	}
}