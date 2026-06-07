namespace GameGuild.Identity.Authentication;

public abstract class AbacPolicyUsageDto
{
    public Guid PolicyId { get; set; }

    public string PolicyName { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public long TotalEvaluations { get; set; }

    public long PositiveEvaluations { get; set; }

    public long NegativeEvaluations { get; set; }

    public double AverageEvaluationTime { get; set; }

    public List<PolicyEvaluationMetric> DailyMetrics { get; set; } = new List<PolicyEvaluationMetric>();

    public List<TopPolicyUser> TopUsers { get; set; } = new List<TopPolicyUser>();
}
