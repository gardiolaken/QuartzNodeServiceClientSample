using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using GrpcService;
using Microsoft.Extensions.Hosting;
using Quartz;
using QuartzNodeService.QuartzSchedulerService;
using System.Threading;

namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public class QuartzNodeGrpcApi : IQuartzNodeGrpcApi
    {
        private readonly GrpcChannelProvider _channelProvider;
        private readonly string _apiKey;
        private readonly string _serviceID;
        private readonly ILogger<QuartzNodeGrpcApi> _logger;
        private readonly IQuartzSchedulerEngine _quartzSchedulerEngine;
        private readonly string _quartzControllerEndPoint;
        private int _attempt;   

        int IQuartzNodeGrpcApi.Attempt { get => _attempt; set => _attempt = value; }

        public QuartzNodeGrpcApi(IConfiguration configuration, GrpcChannelProvider channelProvider, ApiKeyProvider apiKeyProvider, ILogger<QuartzNodeGrpcApi> logger, IQuartzSchedulerEngine quartzSchedulerEngine)
        {
            _channelProvider = channelProvider;
            _apiKey = apiKeyProvider.Key;
            _serviceID = apiKeyProvider.ServiceID;
            _logger = logger;
            _quartzSchedulerEngine = quartzSchedulerEngine;

            _quartzControllerEndPoint = configuration.GetConnectionString("QuartzController");
            if (string.IsNullOrEmpty(_quartzControllerEndPoint))
                throw new Exception("quartzControllerEndPoint string is not configured in appsettings.");

        }
        private GrpcChannel CreateChannelForEndpoint(string endpoint)
        {
            if (_channelProvider == null)
                return GrpcChannel.ForAddress(endpoint);

            return _channelProvider.GetChannel(endpoint);
        }

        private QuartzNode.QuartzNodeClient CreateClientForEndpoint(string endpoint)
        {
            var channel = CreateChannelForEndpoint(endpoint);
            var invoker = channel.Intercept(new HeaderInterceptor(_apiKey, _serviceID));
            return new QuartzNode.QuartzNodeClient(invoker);
        }

        /// <summary>
        /// Attempt a single connection to the controller. Exceptions bubble to the caller (hosted service),
        /// which is responsible for reconnect/backoff policy.
        /// </summary>
        public async Task ConnectToControllerStream(CancellationToken ct)
        {
            _logger.LogInformation("Connecting to controller at {Endpoint}", _quartzControllerEndPoint);

            var client = CreateClientForEndpoint(_quartzControllerEndPoint);
            using var stream = client.QuartzClientStreamBridge();

            // Reader task - exceptions are logged and rethrown so the supervisor can handle reconnect.
            var readerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var msg in stream.ResponseStream.ReadAllAsync(ct))
                    {
                        switch (msg.PayloadCase)
                        {
                            case ServerMessage.PayloadOneofCase.DoWork:
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        var result = await DoWorkAsync(msg.DoWork);
                                        await stream.RequestStream.WriteAsync(new NodeClientMessage
                                        {
                                            RequestId = msg.RequestId,
                                            RequestType = RequestType.DoWorkResponse,
                                            DoWorkResponse = result
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error processing DoWork message");
                                        throw;
                                    }
                                });
                                break;

                            case ServerMessage.PayloadOneofCase.Ack:
                                _logger.LogDebug("Received Ack for RequestId: {RequestId}", msg.RequestId);
                                _attempt = 0;
                                break;
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Response reader canceled by token.");
                    throw;
                }
                catch (Exception)
                {
                    _logger.LogWarning("Response reader faulted; will bubble to supervisor.");
                    throw;
                }
            }, CancellationToken.None);

            // Initial login
            await stream.RequestStream.WriteAsync(new NodeClientMessage
            {
                RequestId = "InitLogin",
                RequestType = RequestType.Login
            }, ct);

            // Heartbeat loop - will exit when ct is canceled or if RequestStream.WriteAsync throws
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                await stream.RequestStream.WriteAsync(new NodeClientMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestType = RequestType.Heartbeat
                }, ct);
            }

            // Wait for reader to finish or propagate its exception
            await readerTask;
        }

        private async Task<QuartzClientResponse_Stream_PROTO> DoWorkAsync(QuartzClientRequest_Stream_PROTO doWork)
        {
            switch (doWork.Action)
            {
                case JobAction.CreateJob:
                    return await _quartzSchedulerEngine.CreateJob(doWork);
                case JobAction.CreateRunNowJob:
                    return await _quartzSchedulerEngine.CreateRunNowJob(doWork);
                case JobAction.GetJob:
                    return await _quartzSchedulerEngine.GetJob(doWork);
                case JobAction.UpdateJob:
                    return await _quartzSchedulerEngine.UpdateJob(doWork);
                case JobAction.DeleteJob:
                    return await _quartzSchedulerEngine.DeleteJob(doWork);
                default:
                    _logger.LogWarning("DoWorkAsync received unsupported JobAction: {Action}", doWork.Action);
                    return new QuartzClientResponse_Stream_PROTO
                    {
                        IsError = true,
                        ErrorMessage = $"Unsupported JobAction: {doWork.Action}"
                    };
            }
        }
    }
}