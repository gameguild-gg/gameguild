using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// Audit log entry for tracking security-sensitive operations
/// </summary>
public class AuditLog : EntityBase {
    /// <summary>
    /// Type of action being audited
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Resource type being acted upon (User, Permission, Role, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the resource
    /// </summary>
    [MaxLength(100)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tenant context for the action
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session ID associated with the action
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Detailed description of the action
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Risk level of the action
    /// </summary>
    public AuditRiskLevel RiskLevel { get; set; } = AuditRiskLevel.Low;

    /// <summary>
    /// Category of the audit event
    /// </summary>
    public AuditCategory Category { get; set; } = AuditCategory.General;

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Risk level for audit events
/// </summary>
public enum AuditRiskLevel {
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Category of audit events
/// </summary>
public enum AuditCategory {
    General = 0,
    Authentication = 1,
    Authorization = 2,
    Permission = 3,
    User = 4,
    Admin = 5,
    Security = 6,
    Data = 7,
    System = 8,
    Tenant = 9,
    Privacy = 10,
    RoleTemplate = 11
}

/// <summary>
/// Common audit action types
/// </summary>
public static class AuditActionTypes {
    // Authentication
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    public const string MfaEnabled = "MfaEnabled";
    public const string MfaDisabled = "MfaDisabled";
    public const string MfaVerified = "MfaVerified";
    public const string MfaFailed = "MfaFailed";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string PasswordReset = "PasswordReset";

    // Authorization & Permissions
    public const string PermissionGranted = "PermissionGranted";
    public const string PermissionDenied = "PermissionDenied";
    public const string PermissionRevoked = "PermissionRevoked";
    public const string RoleAssigned = "RoleAssigned";
    public const string RoleRevoked = "RoleRevoked";
    public const string AccessDenied = "AccessDenied";

    // User Management
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserDeleted = "UserDeleted";
    public const string UserSuspended = "UserSuspended";
    public const string UserReactivated = "UserReactivated";
    public const string UserProfileUpdated = "UserProfileUpdated";

    // Admin Actions
    public const string AdminAction = "AdminAction";
    public const string SystemConfigChanged = "SystemConfigChanged";
    public const string DataExported = "DataExported";
    public const string DataImported = "DataImported";
    public const string BulkOperation = "BulkOperation";

    // Session Management
    public const string SessionCreated = "SessionCreated";
    public const string SessionTerminated = "SessionTerminated";
    public const string DeviceTrusted = "DeviceTrusted";
    public const string DeviceRevoked = "DeviceRevoked";

    // Security Events
    public const string SecurityViolation = "SecurityViolation";
    public const string RateLimitExceeded = "RateLimitExceeded";
    public const string SuspiciousActivity = "SuspiciousActivity";
    public const string PolicyViolation = "PolicyViolation";

    // Tenant Operations
    public const string TenantCreated = "TenantCreated";
    public const string TenantUpdated = "TenantUpdated";
    public const string TenantDeleted = "TenantDeleted";
    public const string TenantIsolationBypassed = "TenantIsolationBypassed";
    public const string TenantUserAdded = "TenantUserAdded";
    public const string TenantUserRemoved = "TenantUserRemoved";

    // Role Template Operations
    public const string RoleTemplateCreated = "RoleTemplateCreated";
    public const string RoleTemplateUpdated = "RoleTemplateUpdated";
    public const string RoleTemplateDeleted = "RoleTemplateDeleted";
    public const string RoleTemplateApplied = "RoleTemplateApplied";
    public const string TenantRoleAssigned = "TenantRoleAssigned";
    public const string TenantRoleRevoked = "TenantRoleRevoked";

    // Privacy Operations
    public const string PrivacySettingsUpdated = "PrivacySettingsUpdated";
    public const string PrivacyTemplateApplied = "PrivacyTemplateApplied";
    public const string PrivacyFieldViewed = "PrivacyFieldViewed";
    public const string PrivacyViolationAttempt = "PrivacyViolationAttempt";
    public const string PrivacyBulkOperationApplied = "PrivacyBulkOperationApplied";

    // Username Operations
    public const string UsernameNormalized = "UsernameNormalized";
    public const string UsernameCollisionResolved = "UsernameCollisionResolved";
}

/// <summary>
/// Request to create an audit log entry
/// </summary>
public class CreateAuditLogRequest {
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
