
namespace GameGuild.Resources;

/// <summary>
///     SLA compliance metrics
/// </summary>
public class SlaComplianceMetrics
{
    public Guid TenantId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public int TotalViolations { get; set; }

    public int CriticalViolations { get; set; }

    public int ResolvedViolations { get; set; }

    public int UnresolvedViolations { get; set; }

    public decimal CompliancePercentage { get; set; }

    public TimeSpan AverageResolutionTime { get; set; }

    public Dictionary<SlaViolationType, int> ViolationsByType { get; init; } = new Dictionary<SlaViolationType, int>();
}
