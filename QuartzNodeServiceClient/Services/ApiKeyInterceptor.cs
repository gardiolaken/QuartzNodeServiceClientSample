using Grpc.Core;
using Grpc.Core.Interceptors;

namespace QuartzNodeService.Services
{
	public class ApiKeyInterceptor : Interceptor
	{
		private readonly string _apiKey;
		public ApiKeyInterceptor(string apiKey)
		{
			_apiKey = apiKey;
		}
		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
			TRequest request,
			ServerCallContext context,
			UnaryServerMethod<TRequest, TResponse> continuation)
		{
			var hasKey = context.RequestHeaders.Any(h =>
				h.Key == "x-api-key" && h.Value == _apiKey);

			if (!hasKey)
				throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key"));

			return await continuation(request, context);
		}
	}

}
