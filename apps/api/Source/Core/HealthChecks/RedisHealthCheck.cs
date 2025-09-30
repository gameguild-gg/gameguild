using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Health check for Redis cache
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _connectionMultiplexer;

    private readonly ILogger<RedisHealthCheck> _logger;

    private readonly HealthCheckOptions _options;

    public RedisHealthCheck(IConnectionMultiplexer? connectionMultiplexer, ILogger<RedisHealthCheck> logger, IOptions<HealthCheckOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionMultiplexer == null) { return HealthCheckResult.Unhealthy("Redis connection multiplexer is not configured"); }

            if (!_connectionMultiplexer.IsConnected) { return HealthCheckResult.Unhealthy("Redis is not connected"); }

            var database = _connectionMultiplexer.GetDatabase();
            var stopwatch = Stopwatch.StartNew();

            // Perform a simple ping
            await database.PingAsync();
            stopwatch.Stop();

            var data = new Dictionary<string, object> { ["response_time_ms"] = stopwatch.ElapsedMilliseconds, ["connection_string"] = _options.RedisConnectionString?.Split(';')[0] ?? "unknown" };

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            return HealthCheckResult.Unhealthy($"Redis health check failed: {ex.Message}");
        }
    }
}
