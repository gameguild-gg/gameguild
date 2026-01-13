namespace GameGuild.Compliance.Audit;

/// <summary>
///     Request for unified security audit logs.
/// </summary>
public class UnifiedSecurityAuditRequest
{
    /// <summary>
    ///     Filter by audit source type (Authentication, Permission, General).
    /// </summary>
    public SecurityAuditSourceType? SourceType { get; set; }

    /// <summary>
    ///     Filter by specific user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Filter by tenant ID.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Filter by action type (e.g., Login, Logout, PermissionGrant).
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    ///     Filter by success/failure status.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    ///     Filter by risk level.
    /// </summary>
    public AuditRiskLevel? RiskLevel { get; set; }

    /// <summary>
    ///     Filter events starting from this date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    ///     Filter events until this date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Filter by IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     Search in description/reason fields.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    ///     Number of records to skip (for pagination).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    ///     Number of records to take (max 1000).
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    ///     Sort field.
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    ///     Sort direction (asc/desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
///     Response containing unified security audit logs.
/// </summary>
public class UnifiedSecurityAuditResponse
{
    /// <summary>
    ///     The audit log entries.
    /// </summary>
    public List<UnifiedSecurityAuditEntry> Entries { get; set; } = [];

    /// <summary>
    ///     Total count of matching records.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     Number of records skipped.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    ///     Number of records returned.
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    ///     Breakdown by source type.
    /// </summary>
    public Dictionary<SecurityAuditSourceType, int> SourceBreakdown { get; set; } = new();
}

/// <summary>
///     A unified security audit log entry combining data from multiple sources.
/// </summary>
public class UnifiedSecurityAuditEntry
{
    /// <summary>
    ///     Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Source of the audit entry.
    /// </summary>
    public SecurityAuditSourceType SourceType { get; set; }

    /// <summary>
    ///     Original source table/entity name.
    /// </summary>
    public string SourceEntity { get; set; } = string.Empty;

    /// <summary>
    ///     Type of action performed.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    ///     User ID associated with the event.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     User email (if available).
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    ///     Tenant ID associated with the event.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Resource type affected (if applicable).
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Resource ID affected (if applicable).
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    ///     IP address of the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Description or reason for the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Risk level of the event.
    /// </summary>
    public AuditRiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
///     Source type for security audit entries.
/// </summary>
public enum SecurityAuditSourceType
{
    /// <summary>
    ///     Authentication-related events (login, logout, MFA).
    /// </summary>
    Authentication,

    /// <summary>
    ///     Permission-related events (grant, revoke, check).
    /// </summary>
    Permission,

    /// <summary>
    ///     General audit events (admin actions, security violations).
    /// </summary>
    General,

    /// <summary>
    ///     All source types.
    /// </summary>
    All
}

/// <summary>
///     Request for authentication-specific audit logs.
/// </summary>
public class AuthenticationAuditRequest
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? IpAddress { get; set; }
    public bool? Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
///     Response for authentication audit logs.
/// </summary>
public class AuthenticationAuditResponse
{
    public List<AuthenticationAuditEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public int UniqueIpAddresses { get; set; }
}

/// <summary>
///     Authentication audit entry.
/// </summary>
public class AuthenticationAuditEntry
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? GeoLocation { get; set; }
    public bool IsSuspicious { get; set; }
}

/// <summary>
///     Request for permission-specific audit logs.
/// </summary>
public class PermissionAuditRequest
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? PermissionType { get; set; }
    public string? OperationType { get; set; }
    public string? ResourceType { get; set; }
    public bool? Success { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
///     Response for permission audit logs.
/// </summary>
public class PermissionAuditResponse
{
    public List<PermissionAuditEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int GrantOperations { get; set; }
    public int RevokeOperations { get; set; }
    public int DenyOperations { get; set; }
}

/// <summary>
///     Permission audit entry.
/// </summary>
public class PermissionAuditEntry
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string? PermissionType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? Reason { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
///     Security audit dashboard with aggregated statistics.
/// </summary>
public class SecurityAuditDashboard
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? TenantId { get; set; }

    // Authentication stats
    public int TotalAuthenticationAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public double LoginSuccessRate { get; set; }
    public int UniqueUsersAuthenticated { get; set; }
    public int SuspiciousLoginAttempts { get; set; }

    // Permission stats
    public int TotalPermissionChanges { get; set; }
    public int PermissionsGranted { get; set; }
    public int PermissionsRevoked { get; set; }
    public int PermissionDenials { get; set; }

    // Security events
    public int TotalSecurityViolations { get; set; }
    public int HighRiskEvents { get; set; }
    public int CrossTenantAttempts { get; set; }

    // Top lists
    public List<TopUserActivity> TopActiveUsers { get; set; } = [];
    public List<TopIpActivity> TopIpAddresses { get; set; } = [];
    public List<FailureReasonCount> TopFailureReasons { get; set; } = [];

    // Trends
    public List<DailyActivityTrend> DailyTrends { get; set; } = [];
}

public class TopUserActivity
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public int EventCount { get; set; }
    public int FailedAttempts { get; set; }
}

public class TopIpActivity
{
    public string IpAddress { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int FailedAttempts { get; set; }
    public int UniqueUsers { get; set; }
}

public class FailureReasonCount
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyActivityTrend
{
    public DateTime Date { get; set; }
    public int TotalEvents { get; set; }
    public int AuthenticationEvents { get; set; }
    public int PermissionEvents { get; set; }
    public int SecurityViolations { get; set; }
}
