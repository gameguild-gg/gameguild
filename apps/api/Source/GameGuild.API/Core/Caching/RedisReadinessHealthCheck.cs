using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace GameGuild.API;

internal sealed class RedisReadinessHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!redis.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis is disconnected.");
            }

            var latency = await redis.GetDatabase().PingAsync(CommandFlags.None).ConfigureAwait(false);
            return HealthCheckResult.Healthy(
                "Redis is reachable.",
                new Dictionary<string, object> { ["latencyMilliseconds"] = latency.TotalMilliseconds });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", exception);
        }
    }
}
