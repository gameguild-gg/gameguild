namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyStatisticsDto
{
    public Guid? TenantId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int TotalPolicies { get; set; }

    public int ActivePolicies { get; set; }

    public long TotalEvaluations { get; set; }

    public double AverageEvaluationTime { get; set; }

    public Dictionary<string, int> PoliciesByConditionType { get; set; } = new Dictionary<string, int>();

    public List<PolicyUsageMetric> UsageMetrics { get; set; } = new List<PolicyUsageMetric>();
}
