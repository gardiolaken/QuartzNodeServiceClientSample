using Microsoft.Data.SqlClient;
using QuartzNodeService.Models;
using System.Xml.Linq;

namespace QuartzNodeService
{
	public class QuartzDAL
	{
		private string _connectionString;

		public QuartzDAL (string connectionString)
		{
			_connectionString = connectionString;
		}

		public DatabaseResult<List<QuartzJob>> GetEnabledJobs(string serviceID)
		{
			var result = new DatabaseResult<List<QuartzJob>>
			{
				Data = new List<QuartzJob>()
			};

			try
			{
				var sql = @"
        SELECT
            Id,
            JobName,
            AssemblyPath,
            Schedule,
            ClassName,
            Description,
            LastUpdatedDate,
            LastRunDate,
            CreatedAtDate,
            CreatedBy,
            ConfigName
        FROM [dbo].[QuartzJob]
		WHERE
		QuartzNodeServiceId = @ServiceID
		AND
		Enabled = 1
";

				using var conn = new SqlConnection(_connectionString);
				using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ServiceID", serviceID);

                conn.Open();
				using var reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					result.Data.Add(new QuartzJob
					{
						Id = reader.GetGuid(reader.GetOrdinal("Id")).ToString(),
						JobName = reader.GetString(reader.GetOrdinal("JobName")),
						AssemblyPath = reader.GetString(reader.GetOrdinal("AssemblyPath")),
						Schedule = reader.GetString(reader.GetOrdinal("Schedule")),
						ClassName = reader.GetString(reader.GetOrdinal("ClassName")),
						Description = reader.IsDBNull(reader.GetOrdinal("Description"))
										? null
										: reader.GetString(reader.GetOrdinal("Description")),
						LastUpdatedDate = reader.GetDateTime(reader.GetOrdinal("LastUpdatedDate")),
						LastRunDate = reader.IsDBNull(reader.GetOrdinal("LastRunDate"))
										? DateTime.MinValue
										: reader.GetDateTime(reader.GetOrdinal("LastRunDate")),
						CreatedAtDate = reader.GetDateTime(reader.GetOrdinal("CreatedAtDate")),
						CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
						ConfigName = reader.GetString(reader.GetOrdinal("ConfigName"))
					});
				}

			}
			catch (Exception e)
			{
				result.IsError = true;
				result.ErrorMessage = e.Message;
			}
			
			return result;
		}

        public async Task TestConnectionAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to connect to the database.", ex);
            }
        }
    }
}
