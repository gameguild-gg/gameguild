namespace GameGuild.Modules.Audit;

public class AuditLogQueryRequest
{
    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string? ActionType { get; set; }

    public string? ResourceType { get; set; }

    public AuditCategory? Category { get; set; }

    public AuditRiskLevel? RiskLevel { get; set; }

    public bool? Success { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? IpAddress { get; set; }

    public int Skip { get; set; } = 0;

    public int Take { get; set; } = 100;
}