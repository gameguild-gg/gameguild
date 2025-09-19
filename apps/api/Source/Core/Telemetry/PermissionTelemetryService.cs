using System.Diagnostics;
using GameGuild.Core.Configuration;

namespace GameGuild.Core.Telemetry;

/// <summary>
/// Service for adding telemetry to permission checks
/// </summary>
public interface IPermissionTelemetryService {
    /// <summary>
    /// Records a permission check operation with telemetry
    /// </summary>
    Task<bool> RecordPermissionCheckAsync(
        string permission,
        string? resourceType,
        Guid? resourceId,
        Func<Task<bool>> permissionCheck);
}

/// <summary>
/// Implementation of permission telemetry service
/// </summary>
public class PermissionTelemetryService : IPermissionTelemetryService {
    private readonly ILogger<PermissionTelemetryService> _logger;

    public PermissionTelemetryService(ILogger<PermissionTelemetryService> logger) {
        _logger = logger;
    }

    public async Task<bool> RecordPermissionCheckAsync(
        string permission,
        string? resourceType,
        Guid? resourceId,
        Func<Task<bool>> permissionCheck) {

        ArgumentNullException.ThrowIfNull(permissionCheck);

        using var activity = OpenTelemetryConfiguration.PermissionActivitySource.StartActivity("Permission.Check");
        activity?.SetTag("permission.name", permission);
        activity?.SetTag("permission.resource_type", resourceType);
        activity?.SetTag("permission.resource_id", resourceId?.ToString());

        // Add correlation ID if available
        if (Activity.Current?.GetBaggageItem("CorrelationId") is string correlationId) {
            activity?.SetTag("correlation.id", correlationId);
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;
        Exception? exception = null;

        try {
            _logger.LogDebug("Checking permission {Permission} for resource {ResourceType}:{ResourceId}",
                permission, resourceType, resourceId);

            var result = await permissionCheck().ConfigureAwait(false);
            success = true;

            activity?.SetTag("permission.granted", result);
            return result;
        }
        catch (Exception ex) {
            exception = ex;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
        finally {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            // Record metrics
            OpenTelemetryConfiguration.PermissionCheckCounter.Add(1,
                new KeyValuePair<string, object?>("permission.name", permission),
                new KeyValuePair<string, object?>("permission.resource_type", resourceType ?? "global"),
                new KeyValuePair<string, object?>("success", success));

            OpenTelemetryConfiguration.PermissionCheckDuration.Record(durationMs,
                new KeyValuePair<string, object?>("permission.name", permission),
                new KeyValuePair<string, object?>("permission.resource_type", resourceType ?? "global"),
                new KeyValuePair<string, object?>("success", success));

            activity?.SetTag("permission.duration_ms", durationMs);

            if (success) {
                _logger.LogDebug("Permission check for {Permission} completed in {Duration}ms",
                    permission, durationMs);
            }
            else {
                _logger.LogWarning("Permission check for {Permission} failed in {Duration}ms: {Error}",
                    permission, durationMs, exception?.Message ?? "Check failed");
            }
        }
    }
}
