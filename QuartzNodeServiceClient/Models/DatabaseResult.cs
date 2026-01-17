namespace QuartzNodeService.Models
{
	public class DatabaseResult<T>
	{
		public bool IsError { get; set; } = false;
		public T Data {get;set; }
		public string ErrorMessage { get; set; }

	}
}
