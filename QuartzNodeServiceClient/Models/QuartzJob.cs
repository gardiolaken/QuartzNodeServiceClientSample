namespace QuartzNodeService.Models
{
	public class QuartzJob
	{
		public string Id
		{
			get;set;
		}
		public string JobName
		{
			get; set;
		}
		public string ServerName
		{
			get; set;
		}
		public string AssemblyPath
		{
			get; set;
		}
		public string Schedule
		{
			get; set;
		}
		public string ClassName
		{
			get; set;
		}
		public string Description
		{
			get; set;
		}
		public DateTime CreatedAtDate
		{
			get; set;
		}
		public DateTime LastUpdatedDate
		{
			get; set;
		}
		public DateTime LastRunDate
		{
			get; set;
		}
		public string CreatedBy
		{
			get; set;
		}
		public string ConfigName
		{
			get; set;
		}
	}
}
