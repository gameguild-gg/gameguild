using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Health check for Redis cache
/// </summary>
public class RedisHealthCheck : IHealthCheck {
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly HealthCheckOptions _options;

    public RedisHealthCheck(
        IConnectionMultiplexer? connectionMultiplexer,
        ILogger<RedisHealthCheck> logger,
        IOptions<HealthCheckOptions> options) {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            if (_connectionMultiplexer == null) {
                return HealthCheckResult.Unhealthy("Redis connection multiplexer is not configured");
            }

            if (!_connectionMultiplexer.IsConnected) {
                return HealthCheckResult.Unhealthy("Redis is not connected");
            }

            var database = _connectionMultiplexer.GetDatabase();
            var stopwatch = Stopwatch.StartNew();

            // Perform a simple ping
            await database.PingAsync();
            stopwatch.Stop();

            var data = new Dictionary<string, object> {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["connection_string"] = _options.RedisConnectionString?.Split(';')[0] ?? "unknown"
            };

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy($"Redis health check failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check for payment providers
/// </summary>
public class PaymentProviderHealthCheck : IHealthCheck {
    private readonly ILogger<PaymentProviderHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public PaymentProviderHealthCheck(ILogger<PaymentProviderHealthCheck> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var results = new List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)>();

        // Check Stripe (example)
        await CheckStripeHealth(results, cancellationToken);

        // Check other payment providers as needed
        // await CheckPayPalHealth(results, cancellationToken);

        var healthyCount = results.Count(r => r.IsHealthy);
        var totalCount = results.Count;

        var data = new Dictionary<string, object> {
            ["healthy_providers"] = healthyCount,
            ["total_providers"] = totalCount,
            ["providers"] = results.Select(r => new {
                name = r.Provider,
                healthy = r.IsHealthy,
                error = r.Error,
                response_time_ms = r.ResponseTime
            }).ToArray()
        };

        if (healthyCount == 0) {
            return HealthCheckResult.Unhealthy("No payment providers are healthy", null, data);
        }
        else if (healthyCount < totalCount) {
            return HealthCheckResult.Degraded($"Only {healthyCount}/{totalCount} payment providers are healthy", null, data);
        }
        else {
            return HealthCheckResult.Healthy("All payment providers are healthy", data);
        }
    }

    private async Task CheckStripeHealth(List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)> results, CancellationToken cancellationToken) {
        try {
            var stopwatch = Stopwatch.StartNew();

            // Simple HTTP check to Stripe's status page or API
            using var response = await _httpClient.GetAsync("https://status.stripe.com/api/v2/status.json", cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode) {
                results.Add(("Stripe", true, null, stopwatch.ElapsedMilliseconds));
            }
            else {
                results.Add(("Stripe", false, $"HTTP {response.StatusCode}", stopwatch.ElapsedMilliseconds));
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Stripe health check failed");
            results.Add(("Stripe", false, ex.Message, 0));
        }
    }
}

/// <summary>
/// Health check for KYC providers
/// </summary>
public class KycProviderHealthCheck : IHealthCheck {
    private readonly ILogger<KycProviderHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public KycProviderHealthCheck(ILogger<KycProviderHealthCheck> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var results = new List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)>();

        // Check various KYC providers
        // For now, we'll simulate checks - replace with actual provider health endpoints
        await CheckKycProviderHealth("Provider1", "https://example.com/health", results, cancellationToken);

        var healthyCount = results.Count(r => r.IsHealthy);
        var totalCount = results.Count;

        var data = new Dictionary<string, object> {
            ["healthy_providers"] = healthyCount,
            ["total_providers"] = totalCount,
            ["providers"] = results.Select(r => new {
                name = r.Provider,
                healthy = r.IsHealthy,
                error = r.Error,
                response_time_ms = r.ResponseTime
            }).ToArray()
        };

        if (totalCount == 0) {
            return HealthCheckResult.Healthy("No KYC providers configured", data);
        }
        else if (healthyCount == 0) {
            return HealthCheckResult.Unhealthy("No KYC providers are healthy", null, data);
        }
        else if (healthyCount < totalCount) {
            return HealthCheckResult.Degraded($"Only {healthyCount}/{totalCount} KYC providers are healthy", null, data);
        }
        else {
            return HealthCheckResult.Healthy("All KYC providers are healthy", data);
        }
    }

    private async Task CheckKycProviderHealth(string providerName, string healthEndpoint, List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)> results, CancellationToken cancellationToken) {
        try {
            var stopwatch = Stopwatch.StartNew();

            using var response = await _httpClient.GetAsync(healthEndpoint, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode) {
                results.Add((providerName, true, null, stopwatch.ElapsedMilliseconds));
            }
            else {
                results.Add((providerName, false, $"HTTP {response.StatusCode}", stopwatch.ElapsedMilliseconds));
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "KYC provider {Provider} health check failed", providerName);
            results.Add((providerName, false, ex.Message, 0));
        }
    }
}

/// <summary>
/// Health check for system memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck {
    private readonly IOptions<HealthCheckOptions> _options;

    public MemoryHealthCheck(IOptions<HealthCheckOptions> options) {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var gc = GC.GetTotalMemory(false);
        var workingSet = Environment.WorkingSet;
        var thresholdBytes = _options.Value.MemoryThresholdMB * 1024 * 1024;

        var data = new Dictionary<string, object> {
            ["gc_memory_mb"] = gc / 1024 / 1024,
            ["working_set_mb"] = workingSet / 1024 / 1024,
            ["threshold_mb"] = _options.Value.MemoryThresholdMB
        };

        if (workingSet > thresholdBytes) {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Memory usage ({workingSet / 1024 / 1024} MB) exceeds threshold ({_options.Value.MemoryThresholdMB} MB)", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Memory usage is within acceptable limits", data));
    }
}

/// <summary>
/// Health check for disk space availability
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck {
    private readonly IOptions<HealthCheckOptions> _options;

    public DiskSpaceHealthCheck(IOptions<HealthCheckOptions> options) {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            var results = new List<object>();
            var anyUnhealthy = false;

            foreach (var drive in drives) {
                var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var totalSpaceGB = drive.TotalSize / 1024 / 1024 / 1024;
                var isHealthy = freeSpaceGB >= _options.Value.DiskSpaceThresholdGB;

                if (!isHealthy) {
                    anyUnhealthy = true;
                }

                results.Add(new {
                    drive = drive.Name,
                    free_space_gb = freeSpaceGB,
                    total_space_gb = totalSpaceGB,
                    usage_percent = Math.Round((double)(totalSpaceGB - freeSpaceGB) / totalSpaceGB * 100, 2),
                    healthy = isHealthy
                });
            }

            var data = new Dictionary<string, object> {
                ["drives"] = results,
                ["threshold_gb"] = _options.Value.DiskSpaceThresholdGB
            };

            if (anyUnhealthy) {
                return Task.FromResult(HealthCheckResult.Unhealthy($"One or more drives have less than {_options.Value.DiskSpaceThresholdGB} GB free space", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("All drives have sufficient free space", data));
        }
        catch (Exception ex) {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Failed to check disk space: {ex.Message}"));
        }
    }
}
