using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.Core.Configuration;

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
                var freeSpaceGb = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var totalSpaceGb = drive.TotalSize / 1024 / 1024 / 1024;
                var isHealthy = freeSpaceGb >= _options.Value.DiskSpaceThresholdGb;

                if (!isHealthy) {
                    anyUnhealthy = true;
                }

                results.Add(new {
                    drive = drive.Name,
                    free_space_gb = freeSpaceGb,
                    total_space_gb = totalSpaceGb,
                    usage_percent = Math.Round((double)(totalSpaceGb - freeSpaceGb) / totalSpaceGb * 100, 2),
                    healthy = isHealthy
                });
            }

            var data = new Dictionary<string, object> {
                ["drives"] = results,
                ["threshold_gb"] = _options.Value.DiskSpaceThresholdGb
            };

            if (anyUnhealthy) {
                return Task.FromResult(HealthCheckResult.Unhealthy($"One or more drives have less than {_options.Value.DiskSpaceThresholdGb} GB free space", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("All drives have sufficient free space", data));
        }
        catch (Exception ex) {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Failed to check disk space: {ex.Message}"));
        }
    }
}