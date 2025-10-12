using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get tenant audit trail </summary>
public class GetTenantAuditTrailQuery : IQuery<Result<PagedResult<TenantAuditLogDto>>>
{
    public Guid TenantId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? ActionType { get; init; }
    public Guid? UserId { get; init; }
    public string? EntityType { get; init; }
}

/// <summary> Query to get system-wide audit trail for tenants </summary>
public class GetSystemTenantAuditQuery : IQuery<Result<PagedResult<TenantAuditLogDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? ActionType { get; init; }
    public Guid? UserId { get; init; }
    public string? TenantName { get; init; }
    public AuditSeverity? Severity { get; init; }
}

/// <summary> DTO for tenant audit log entries </summary>
public class TenantAuditLogDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public AuditSeverity Severity { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary> Audit severity levels </summary>
public enum AuditSeverity
{
    Info,
    Warning,
    Error,
    Critical
}