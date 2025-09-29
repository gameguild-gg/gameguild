using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.Core.Configuration;

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
        var thresholdBytes = _options.Value.MemoryThresholdMb * 1024 * 1024;

        var data = new Dictionary<string, object> {
            ["gc_memory_mb"] = gc / 1024 / 1024,
            ["working_set_mb"] = workingSet / 1024 / 1024,
            ["threshold_mb"] = _options.Value.MemoryThresholdMb
        };

        if (workingSet > thresholdBytes) {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Memory usage ({workingSet / 1024 / 1024} MB) exceeds threshold ({_options.Value.MemoryThresholdMb} MB)", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Memory usage is within acceptable limits", data));
    }
}