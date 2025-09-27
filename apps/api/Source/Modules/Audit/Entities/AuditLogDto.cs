namespace GameGuild.Modules.Audit;

public class AuditLogDto
{
    public Guid Id { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid? SessionId { get; set; }

    public string? Description { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public AuditRiskLevel RiskLevel { get; set; }

    public AuditCategory Category { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }
}