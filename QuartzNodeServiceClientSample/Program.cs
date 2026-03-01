using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Logging;
using QuartzNodeServiceClient;
using SampleJob;
using Serilog;
using Serilog.Core;
using Serilog.Events;

public class Program
{

	public static async Task Main(string[] args)
	{
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		LogProvider.IsDisabled = !config.GetValue<bool?>("Quartz:EnableDebugMode") ?? true;

		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.CreateLogger();

		var hostBuilder = Host.CreateDefaultBuilder(args);
		hostBuilder
			 .UseWindowsService()
			 .UseSerilog((context, loggerConfiguration) => loggerConfiguration
				 .ReadFrom.Configuration(config)
				 .Enrich.FromLogContext()
				 .WriteTo.Console()				 )
			 .ConfigureServices((hostContext, services) =>
			 {
				 var config = hostContext.Configuration;

				 var cron = config.GetValue<string>("Quartz:JobSchedule");
				 if (string.IsNullOrEmpty(cron))
					 throw new Exception($"Invalid cron schedule provided for QC.");

				 services.AddSingleton(config);
				 services.AddQuartz(x =>
				 {
					 x.AddTrigger(trigger => trigger
					 .ForJob("SampleJob#1")
					 .WithIdentity("SampleJob#1")
					 .WithCronSchedule(cron)
					 .WithDescription($"Trigger created for SampleJob#1"));
					 x.AddJob<FileImporter>(opts => opts.WithIdentity("SampleJob#1"));

                     x.AddTrigger(trigger => trigger
                     .ForJob("SampleJob#2")
                     .WithIdentity("SampleJob#2")
                     .WithCronSchedule(cron)
                     .WithDescription($"Trigger created for SampleJob#2"));
                     x.AddJob<FileImporter2>(opts => opts.WithIdentity("SampleJob#2"));
                 });

				 services.AddQuartzHostedService(x => x.WaitForJobsToComplete = true);
			 });		
		try
		{
			QuartzNodeServiceClientBuilder.AddQuartzNodeServiceClient("SampleService#1", "UAT-01", hostBuilder, config);
			var app = hostBuilder.Build();

			app.Run();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		finally
		{

		}
	}
}