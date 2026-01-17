using System;
using System.Collections.Concurrent;
using Grpc.Net.Client;

namespace QuartzNodeService.QuartzNodeGrpcApi
{
    // Thread-safe provider that reuses GrpcChannel instances per endpoint
    public class GrpcChannelProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new();
        private bool _disposed;

        public GrpcChannelProvider()
        { }

        public GrpcChannel GetChannel(string endpoint)
        {
            return _channels.GetOrAdd(endpoint, CreateChannel);
        }

        private GrpcChannel CreateChannel(string endpoint)
        {
            // Configure low-level handler for HTTP/2 keep-alive pings (requires .NET 6+)
            var socketsHandler = new SocketsHttpHandler
            {
                // Send periodic HTTP/2 pings so proxies/NAT/firewalls don't consider the connection idle.
                // Adjust delays/timeouts to suit your environment.
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20),

                // Allow multiple HTTP/2 connections if needed
                EnableMultipleHttp2Connections = true,

                // Optional: tune pooled connection lifetime / idle timeout if you want
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
            };

            // If you need custom certificate validation in dev, set SslOptions here (avoid in prod).
            // socketsHandler.SslOptions = new SslClientAuthenticationOptions { ... };

            var httpClient = new HttpClient(socketsHandler)
            {
                // Prevent HttpClient overall timeout from canceling long-lived stream
                Timeout = Timeout.InfiniteTimeSpan
            };

            var options = new GrpcChannelOptions
            {
                HttpClient = httpClient
            };

            return GrpcChannel.ForAddress(endpoint, options);
        }

        /// <summary>
        /// Removes and disposes the cached channel for the endpoint so a subsequent GetChannel will create a fresh channel.
        /// Useful when a channel/connection is suspected to be in a bad state and should be recreated for reconnect attempts.
        /// </summary>
        public void RemoveChannel(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return;

            if (_channels.TryRemove(endpoint, out var channel))
            {
                try
                {
                    channel.Dispose();
                }
                catch
                {
                    // ignore disposal failures - best effort cleanup
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var kv in _channels)
            {
                try
                {
                    kv.Value.Dispose();
                }
                catch { }
            }

            _channels.Clear();
        }
    }
}