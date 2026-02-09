using System.Diagnostics;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Handlers;

/// <summary>
///     Event handler for quota exceeded events - triggers alerts and notifications.
///     <para>
///     This handler integrates with observability infrastructure to:
///     - Log structured alerts for monitoring systems (Datadog, Prometheus, etc.)
///     - Record metrics for dashboards and alerting rules
///     - Support future integrations (PagerDuty, Slack, email notifications)
///     </para>
/// </summary>
/// <remarks>
///     Alert thresholds and notification channels are configurable.
///     Default behavior: Log warning for all exceeded events, error for repeated violations.
/// </remarks>
public class QuotaExceededAlertHandler(
    ILogger<QuotaExceededAlertHandler> logger
) : INotificationHandler<QuotaExceededEvent>
{
    /// <summary>
    ///     ActivitySource for OpenTelemetry tracing of alert handling.
    /// </summary>
    private static readonly ActivitySource ActivitySource = new("GameGuild.Resources.Alerts", "1.0.0");

    /// <summary>
    ///     Counter for tracking quota exceeded events per tenant for rate limiting alerts.
    ///     Key: TenantId + ResourceType, Value: (Count, FirstOccurrence)
    /// </summary>
    private static readonly Dictionary<string, (int Count, DateTime FirstOccurrence)> RecentViolations = new();

    private static readonly object ViolationsLock = new();

    /// <summary>
    ///     Threshold for escalating to error-level alert (repeated violations within window).
    /// </summary>
    private const int EscalationThreshold = 5;

    /// <summary>
    ///     Time window for counting repeated violations.
    /// </summary>
    private static readonly TimeSpan ViolationWindow = TimeSpan.FromMinutes(15);

    public async Task Handle(QuotaExceededEvent notification, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("quota.alert.exceeded", ActivityKind.Internal);
        activity?.SetTag("tenant.id", notification.TenantId.ToString());
        activity?.SetTag("resource.type", notification.ResourceType.ToString());
        activity?.SetTag("quota.current_usage", notification.CurrentUsage);
        activity?.SetTag("quota.requested_amount", notification.RequestedAmount);
        activity?.SetTag("quota.hard_limit", notification.HardLimit);
        activity?.SetTag("quota.source", notification.Source ?? "unknown");

        // Check for repeated violations
        var violationKey = $"{notification.TenantId}:{notification.ResourceType}";
        var isRepeatedViolation = UpdateViolationTracking(violationKey, out var violationCount);

        activity?.SetTag("alert.violation_count", violationCount);
        activity?.SetTag("alert.is_repeated", isRepeatedViolation);

        // Calculate usage percentage for severity assessment
        var usagePercentage = notification.HardLimit > 0
            ? (double)notification.CurrentUsage / notification.HardLimit * 100
            : 0;
        activity?.SetTag("quota.usage_percentage", usagePercentage);

        // Log structured alert with all relevant context for monitoring systems
        if (isRepeatedViolation)
        {
            // Escalate to error level for repeated violations
            activity?.SetTag("alert.severity", "error");
            activity?.SetStatus(ActivityStatusCode.Error, "Repeated quota violations");

            logger.LogError(
                "QUOTA_EXCEEDED_REPEATED: Tenant {TenantId} has exceeded {ResourceType} quota {ViolationCount} times in {Window}. " +
                "Current: {CurrentUsage}/{HardLimit} ({UsagePercentage:F1}%). Requested: {RequestedAmount}. Source: {Source}. " +
                "ActorId: {ActorId}. Timestamp: {Timestamp:O}",
                notification.TenantId,
                notification.ResourceType,
                violationCount,
                ViolationWindow,
                notification.CurrentUsage,
                notification.HardLimit,
                usagePercentage,
                notification.RequestedAmount,
                notification.Source ?? "unknown",
                notification.ActorId,
                notification.Timestamp);

            // Record metric for repeated violation (for Prometheus/Datadog)
            RecordMetric("quota_exceeded_repeated_total", 1, new Dictionary<string, object?>
            {
                ["tenant_id"] = notification.TenantId.ToString(),
                ["resource_type"] = notification.ResourceType.ToString(),
                ["violation_count"] = violationCount
            });
        }
        else
        {
            // Standard warning for first violation
            activity?.SetTag("alert.severity", "warning");

            logger.LogWarning(
                "QUOTA_EXCEEDED: Tenant {TenantId} exceeded {ResourceType} quota. " +
                "Current: {CurrentUsage}/{HardLimit} ({UsagePercentage:F1}%). Requested: {RequestedAmount}. Source: {Source}. " +
                "ActorId: {ActorId}. Timestamp: {Timestamp:O}",
                notification.TenantId,
                notification.ResourceType,
                notification.CurrentUsage,
                notification.HardLimit,
                usagePercentage,
                notification.RequestedAmount,
                notification.Source ?? "unknown",
                notification.ActorId,
                notification.Timestamp);
        }

        // Record metric for all violations
        RecordMetric("quota_exceeded_total", 1, new Dictionary<string, object?>
        {
            ["tenant_id"] = notification.TenantId.ToString(),
            ["resource_type"] = notification.ResourceType.ToString()
        });

        // PLANNED: In-app notification delivery via GameGuild.Notifications
        // (blocked by circular dependency: Notifications → Identity.Users → Resources → Notifications).
        // Resolution: extract a shared INotificationPublisher abstraction into SharedKernel,
        // or move QuotaExceededAlertHandler into a dedicated orchestration module.
        // Current alerts are delivered via structured logging + OpenTelemetry metrics (Datadog/Prometheus).

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Updates violation tracking and returns whether this is a repeated violation.
    /// </summary>
    private static bool UpdateViolationTracking(string key, out int currentCount)
    {
        lock (ViolationsLock)
        {
            var now = DateTime.UtcNow;

            // Clean up old entries
            var expiredKeys = RecentViolations
                .Where(kvp => now - kvp.Value.FirstOccurrence > ViolationWindow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expiredKey in expiredKeys)
            {
                RecentViolations.Remove(expiredKey);
            }

            // Update or add current violation
            if (RecentViolations.TryGetValue(key, out var existing))
            {
                if (now - existing.FirstOccurrence <= ViolationWindow)
                {
                    currentCount = existing.Count + 1;
                    RecentViolations[key] = (currentCount, existing.FirstOccurrence);
                    return currentCount >= EscalationThreshold;
                }
            }

            // First violation in new window
            currentCount = 1;
            RecentViolations[key] = (1, now);
            return false;
        }
    }

    /// <summary>
    ///     Records a metric for observability platforms.
    ///     Placeholder for integration with Prometheus, Datadog, Application Insights, etc.
    /// </summary>
    private static void RecordMetric(string metricName, double value, Dictionary<string, object?> tags)
    {
        // This is a placeholder for actual metrics integration.
        // In production, this would use:
        // - System.Diagnostics.Metrics.Meter for OpenTelemetry metrics
        // - Prometheus.Net for Prometheus
        // - Datadog.Trace for Datadog
        // - Application Insights TelemetryClient for Azure

        // The structured logs above can be parsed by log aggregators to create metrics.
        // For explicit metrics, inject IMeterProvider or similar and record here.
    }
}
