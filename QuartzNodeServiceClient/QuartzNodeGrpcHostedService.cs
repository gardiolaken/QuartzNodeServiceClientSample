using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public class QuartzNodeGrpcHostedService : IHostedService, IDisposable
    {
        private readonly IQuartzNodeGrpcApi _grpcApi;
        private readonly GrpcChannelProvider _channelProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QuartzNodeGrpcHostedService> _logger;
        private Task? _supervisorTask;
        private CancellationTokenSource? _supervisorCts;
        private string? _controllerEndpoint;

        // Supervision config
        private const int MaxBackoffSeconds = 30; // if cant connect for RapidFailureWindow(1minute), retry every 5 minutes.
        private const int MaxRapidFailureCount = 5;      // cut to slower retries if many quick failures
        private static readonly TimeSpan RapidFailureWindow = TimeSpan.FromMinutes(1);

        // track recent failures to apply a protective backoff
        private int _recentFailureCount;
        private DateTime _firstFailureTime;

        public QuartzNodeGrpcHostedService(IQuartzNodeGrpcApi grpcApi, GrpcChannelProvider channelProvider, IConfiguration configuration, ILogger<QuartzNodeGrpcHostedService> logger)
        {
            _grpcApi = grpcApi;
            _channelProvider = channelProvider;
            _configuration = configuration;
            _logger = logger;
            _controllerEndpoint = _configuration.GetConnectionString("QuartzController");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting QuartzNodeGrpcHostedService.");

            _supervisorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start a supervisor task that will ensure ConnectToControllerStream runs and is restarted on failure.
            _supervisorTask = Task.Run(() => SupervisorLoopAsync(_supervisorCts.Token), CancellationToken.None);

            return Task.CompletedTask;
        }

        private async Task SupervisorLoopAsync(CancellationToken token)
        {
            var rnd = new Random();
            _grpcApi.Attempt = 0;

		    while (!token.IsCancellationRequested)
            {
                try
                {
                    _grpcApi.Attempt++;
                    _logger.LogInformation("Supervisor starting ConnectToControllerStream (attempt {Attempt}).", _grpcApi.Attempt);
                    // Run the API connection. This method should observe the provided token and only complete on cancellation or unrecoverable error.

                    await _grpcApi.ConnectToControllerStream(token);

                    // If ConnectToControllerStream returned without throwing and cancellation wasn't requested,
                    // treat that as a clean exit and break the loop.
                    if (!token.IsCancellationRequested)
                    {
                        _logger.LogInformation("ConnectToControllerStream exited normally; supervisor will stop restarting.");
                        break;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    _logger.LogInformation("Supervisor cancel requested; stopping supervisor loop.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("ConnectToControllerStream faulted. Supervisor will attempt restart. Turn on Debug Level Log for more info");
                    _logger.LogDebug($"ConnectToControllerStream faulted {ex}");
                    // track rapid failures
                    var now = DateTime.UtcNow;
                    if (_recentFailureCount == 0)
                    {
                        _firstFailureTime = now;
                        _recentFailureCount = 1;
                    }
                    else
                    {
                        // So RapidFailureWindow and MaxRapidFailureCount works together.
                        // Basically if retries within RapidFailureWindow time exceeds MaxRapidFailureCount,
                        // then apply max delay before next retry. Good for controller server shutdown
                        if (now - _firstFailureTime <= RapidFailureWindow) 
                        {
                            _recentFailureCount++;
                        }
                        else
                        {
                            // window expired - reset
                            _firstFailureTime = now;
                            _recentFailureCount = 1;
                        }
                    }

                    // Remove cached channel so next GetChannel will create a fresh channel
                    try
                    {
                        if (!string.IsNullOrEmpty(_controllerEndpoint))
                        {
                            _logger.LogDebug("Removing cached gRPC channel for endpoint {Endpoint} before retry.", _controllerEndpoint);
                            _channelProvider.RemoveChannel(_controllerEndpoint);
                        }
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogDebug(ex2, "Failed to remove cached channel - continuing to retry.");
                    }

                    // Exponential backoff with jitter; if many rapid failures, escalate to a larger backoff
                    var baseBackoffSeconds = Math.Min(Math.Pow(2, Math.Min(_grpcApi.Attempt, 6)), MaxBackoffSeconds);
                    if (_recentFailureCount >= MaxRapidFailureCount)
                    {
                        // protective longer backoff when many failures occur quickly
                        baseBackoffSeconds = MaxBackoffSeconds;
                        _logger.LogWarning("Detected {Count} failures within {Window}. Applying protective backoff.", _recentFailureCount, RapidFailureWindow);
                    }

                    var jitterMs = rnd.Next(0, 500);
                    var delay = TimeSpan.FromSeconds(baseBackoffSeconds) + TimeSpan.FromMilliseconds(jitterMs);

                    _logger.LogInformation("Supervisor will retry ConnectToControllerStream in {Delay}. (attempt {Attempt})", delay, _grpcApi.Attempt);

                    try
                    {
                        await Task.Delay(delay, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // continue to next attempt
                }
            }

            _logger.LogInformation("Supervisor loop exiting.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping QuartzNodeGrpcHostedService.");

            if (_supervisorCts == null)
            {
                _logger.LogInformation("Supervisor not running.");
                return;
            }

            try
            {
                _supervisorCts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error cancelling supervisor token.");
            }

            if (_supervisorTask != null)
            {
                try
                {
                    // Wait for the supervisor to complete or timeout with the host cancellation token
                    await Task.WhenAny(_supervisorTask, Task.Delay(Timeout.Infinite, cancellationToken));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception while waiting for supervisor to stop.");
                }
            }

            _logger.LogInformation("QuartzNodeGrpcHostedService stopped.");
        }

        public void Dispose()
        {
            try
            {
                _supervisorCts?.Cancel();
                _supervisorCts?.Dispose();
            }
            catch { }
        }
    }
}