using Microsoft.Data.SqlClient;
using Quartz;
using System.Reflection;

namespace QuartzNodeServiceClient
{
	public abstract class OctJobBase : IJob
	{
		private readonly ILogger _logger;
		protected IConfiguration _configuration;
		public OctJobBase(ILogger logger, IConfiguration config)
		{
			_configuration = config;
			_logger = logger;
		}

		public Task Execute(IJobExecutionContext context)
		{
			Process();
			return Task.CompletedTask;
		}

		protected abstract void Process();

		protected string GetConfig()
		{
			var connectionString = _configuration.GetConnectionString("QuartzConnectionString");
			var settingsKey = _configuration.GetValue<string>("QuartzSettingsKey");

			if (string.IsNullOrWhiteSpace(settingsKey))
			{
				_logger.LogWarning("No settings key provided.");
				return string.Empty;
			}

			var configValue = string.Empty;
			try
			{
				var sql = @"
        SELECT TOP 1 Settings
        FROM [Quartz].[dbo].[Settings]
		WHERE
		[Key] = @SettingsKey
";
				using var conn = new SqlConnection(connectionString);
				using var cmd = new SqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("@SettingsKey", settingsKey);

				conn.Open();
				using var reader = cmd.ExecuteReader();

				if (reader.Read())
				{
					configValue = reader.GetString(reader.GetOrdinal("Settings"));
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error retrieving config for key {settingsKey}: {ex.Message}");
			}

			return configValue;
		}
	}
	
}
