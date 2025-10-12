namespace GameGuild.Modules.Common.Deployment;

/// <summary>
/// Health check service for deployment gating.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Checks if a specific version is healthy and ready to accept traffic.
    /// </summary>
    Task<bool> CheckVersionHealthAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive health checks including dependencies.
    /// </summary>
    Task<HealthCheckResult> PerformDetailedHealthCheckAsync(string version, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default health check service implementation.
/// </summary>
public sealed class HealthCheckService : IHealthCheckService
{
    private readonly IHealthCheckProvider[] _providers;

    public HealthCheckService(IEnumerable<IHealthCheckProvider> providers)
    {
        _providers = providers?.ToArray() ?? throw new ArgumentNullException(nameof(providers));
    }

    public async Task<bool> CheckVersionHealthAsync(string version, CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            var isHealthy = await provider.CheckHealthAsync(version, cancellationToken);
            if (!isHealthy)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<HealthCheckResult> PerformDetailedHealthCheckAsync(string version, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var metrics = new Dictionary<string, object>();

        try
        {
            foreach (var provider in _providers)
            {
                var providerStart = DateTime.UtcNow;
                var isHealthy = await provider.CheckHealthAsync(version, cancellationToken);
                var providerDuration = DateTime.UtcNow - providerStart;

                metrics[$"{provider.GetType().Name}_healthy"] = isHealthy;
                metrics[$"{provider.GetType().Name}_duration_ms"] = providerDuration.TotalMilliseconds;

                if (!isHealthy)
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Version = version,
                        CheckedAt = DateTime.UtcNow,
                        ResponseTime = DateTime.UtcNow - startTime,
                        ErrorMessage = $"Health check failed: {provider.GetType().Name}",
                        Metrics = metrics
                    };
                }
            }

            return new HealthCheckResult
            {
                IsHealthy = true,
                Version = version,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime,
                Metrics = metrics
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                IsHealthy = false,
                Version = version,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime,
                ErrorMessage = ex.Message,
                Metrics = metrics
            };
        }
    }
}

/// <summary>
/// Health check provider interface for custom checks.
/// </summary>
public interface IHealthCheckProvider
{
    /// <summary>
    /// Performs a health check for a specific version.
    /// </summary>
    Task<bool> CheckHealthAsync(string version, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default health check provider (always returns true).
/// </summary>
public sealed class DefaultHealthCheckProvider : IHealthCheckProvider
{
    public Task<bool> CheckHealthAsync(string version, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
