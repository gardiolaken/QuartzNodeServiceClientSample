using Grpc.Core;
using Grpc.Core.Interceptors;
using static Grpc.Core.Interceptors.Interceptor;

namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public class HeaderInterceptor : Interceptor
    {
        private string _apiKey;
        private string _serviceID;
        public HeaderInterceptor(string apiKey, string serviceId)
        {
            _apiKey = apiKey;
            _serviceID = serviceId;
        }
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers ?? new Metadata();
            headers.Add("x-api-key", _apiKey);
            headers.Add("x-service-id", _serviceID);

            var options = context.Options.WithHeaders(headers);
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                options);

            return continuation(request, newContext);
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers ?? new Metadata();
            headers.Add("x-api-key", _apiKey);
            headers.Add("x-service-id", _serviceID); // put serviceid here

            var options = context.Options.WithHeaders(headers);
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, options);

            return continuation(newContext);
        }
    }

}
