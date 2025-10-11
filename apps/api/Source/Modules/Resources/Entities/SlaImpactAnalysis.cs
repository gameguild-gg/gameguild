namespace GameGuild.Modules.Resources.Entities;

/// <summary>
/// SLA impact analysis for resource limits and performance correlation
/// </summary>
[Table("sla_impact_analysis")]
public class SlaImpactAnalysis : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Resource type being analyzed
    /// </summary>
    public ResourceUsageType ResourceType { get; set; }

    /// <summary>
    /// Analysis period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Analysis period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Average response time (ms)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// P95 response time (ms)
    /// </summary>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// P99 response time (ms)
    /// </summary>
    public double P99ResponseTime { get; set; }

    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Resource utilization percentage
    /// </summary>
    public double ResourceUtilization { get; set; }

    /// <summary>
    /// Number of throttling events
    /// </summary>
    public int ThrottlingEvents { get; set; }

    /// <summary>
    /// Number of SLA violations
    /// </summary>
    public int SlaViolations { get; set; }

    /// <summary>
    /// SLA target (ms)
    /// </summary>
    public double SlaTarget { get; set; }

    /// <summary>
    /// SLA compliance percentage
    /// </summary>
    public double SlaCompliance { get; set; }

    /// <summary>
    /// Impact severity (Low, Medium, High, Critical)
    /// </summary>
    [MaxLength(50)]
    public string ImpactSeverity { get; set; } = "Low";

    /// <summary>
    /// Root cause analysis
    /// </summary>
    [MaxLength(1000)]
    public string? RootCause { get; set; }

    /// <summary>
    /// Recommended actions (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? RecommendedActions { get; set; }

    /// <summary>
    /// Performance metrics (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? PerformanceMetrics { get; set; }

    /// <summary>
    /// Whether analysis is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Completed at timestamp
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
