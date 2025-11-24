namespace GameGuild.Permissions.Domain.Models;

/// <summary>
///     Report containing permission usage statistics over a time period
/// </summary>
public class PermissionUsageReport
{
    public Guid? TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalPermissionChanges { get; set; }

    public int GrantCount { get; set; }

    public int RevokeCount { get; set; }

    public List<Guid> MostActiveUsers { get; set; } = new List<Guid>();

    public List<string> MostChangedPermissions { get; set; } = new List<string>();
}

/// <summary>
///     Overall security posture report for a tenant
/// </summary>
public class SecurityPostureReport
{
    public Guid? TenantId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public int TotalUsers { get; set; }

    public int TotalPermissions { get; set; }

    public int ActiveSoDViolations { get; set; }

    public int OverdueAccessReviews { get; set; }

    public int PermissionsWithExpiry { get; set; }

    public int ExpiredPermissions { get; set; }

    public double RiskScore { get; set; }
}

/// <summary>
///     Trend data for permission operations over time
/// </summary>
public class TrendData
{
    public DateTime Date { get; set; }

    public int GrantCount { get; set; }

    public int RevokeCount { get; set; }

    public int CheckCount { get; set; }

    public int TotalOperations { get; set; }
}

/// <summary>
///     Compliance report showing adherence to policies
/// </summary>
public class ComplianceReport
{
    public Guid? TenantId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public int TotalAccessReviews { get; set; }

    public int CompletedAccessReviews { get; set; }

    public double ReviewCompletionRate { get; set; }

    public int ActiveSoDViolations { get; set; }

    public int PermissionsWithoutExpiry { get; set; }

    public int PermissionsNearExpiry { get; set; }

    public double ComplianceScore { get; set; }
}

/// <summary>
///     Alert for detected permission anomalies
/// </summary>
public class PermissionAnomalyAlert
{
    public Guid? TenantId { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; }
}

/// <summary>
///     Comprehensive audit report for a time period
/// </summary>
public class AuditReport
{
    public Guid? TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalOperations { get; set; }

    public int SuccessfulOperations { get; set; }

    public int FailedOperations { get; set; }

    public int UniqueUsers { get; set; }

    public int UniquePerformers { get; set; }

    public Dictionary<string, int> OperationBreakdown { get; set; } = new Dictionary<string, int>();

    public Dictionary<Guid, int> MostActiveUsers { get; set; } = new Dictionary<Guid, int>();

    public Dictionary<Guid, int> MostTargetedUsers { get; set; } = new Dictionary<Guid, int>();

    public Dictionary<string, int> TopPermissions { get; set; } = new Dictionary<string, int>();

    public DateTime GeneratedAt { get; set; }
}

/// <summary>
///     Search criteria for querying audit logs
/// </summary>
public class AuditSearchCriteria
{
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? PerformedBy { get; set; }

    public Guid? TargetUserId { get; set; }

    public PermissionOperationType? OperationType { get; set; }

    public string? Permission { get; set; }

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public bool? SuccessOnly { get; set; }

    public string? IpAddress { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 100;
}
