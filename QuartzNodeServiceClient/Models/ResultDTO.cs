namespace QuartzNodeService.Models
{
    public class ResultDTO<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; } = default;
        public string? ErrorMessage { get; set; } = null;

        public static ResultDTO<T> Success(T data) => new ResultDTO<T> { IsSuccess = true, Data = data };
        public static ResultDTO<T> Failure(string errorMessage) => new ResultDTO<T> { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
