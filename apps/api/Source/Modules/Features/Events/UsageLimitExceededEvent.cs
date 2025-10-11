using GameGuild.CQRS;

namespace GameGuild.Modules.Features.Events;

/// <summary>
///     Domain event raised when usage limits are exceeded
/// </summary>
public class UsageLimitExceededEvent : DomainEvent {
    public UsageLimitExceededEvent(
      Guid tenantId,
      string metricName,
      long currentUsage,
      long limit,
      double utilizationPercentage)
      : base(tenantId, "UsageMetric") {
        TenantId = tenantId;
        MetricName = metricName;
        CurrentUsage = currentUsage;
        Limit = limit;
        UtilizationPercentage = utilizationPercentage;
    }

    public Guid TenantId { get; }

    public string MetricName { get; }

    public long CurrentUsage { get; }

    public long Limit { get; }

    public double UtilizationPercentage { get; }
}

