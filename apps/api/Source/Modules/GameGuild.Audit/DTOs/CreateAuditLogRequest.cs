namespace GameGuild.Audit;

/// <summary>
/// Request to create an audit log entry
/// </summary>
public class CreateAuditLogRequest
{
    public string ActionType { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid? SessionId { get; set; }

    public string? Description { get; set; }

    public object? Metadata { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public AuditRiskLevel RiskLevel { get; set; } = AuditRiskLevel.Low;

    public AuditCategory Category { get; set; } = AuditCategory.General;

    public string? CorrelationId { get; set; }
}
