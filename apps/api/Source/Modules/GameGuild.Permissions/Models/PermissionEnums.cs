namespace GameGuild.Permissions.Domain.Models;

/// <summary>
///     Permission layer in the 3-layer DAC (Discretionary Access Control) system
///     Matches Game Guild's authorization model
/// </summary>
public enum PermissionLayer
{
    /// <summary>
    ///     Auto-detect permission layer (try tenant -> content-type -> resource)
    /// </summary>
    Auto = 0,

    /// <summary>
    ///     Tenant-wide permissions - applies to all content types within a tenant
    /// </summary>
    Tenant = 1,

    /// <summary>
    ///     Content-type permissions - applies to all entries of a specific content type
    /// </summary>
    ContentType = 2,

    /// <summary>
    ///     Resource-level permissions - applies to specific content entries
    /// </summary>
    Resource = 3
}

/// <summary>
///     Permission type for standard CRUD + extended operations
///     Matches Game Guild's permission types
/// </summary>
public enum PermissionType
{
    None = 0,

    // Basic CRUD
    Read = 1,

    Write = 2,

    Create = 3,

    Delete = 4,

    // Extended permissions
    Edit = 5,

    Update = 6,

    Share = 7,

    Manage = 8,

    Admin = 9,

    // Administrative permissions
    SystemAdmin = 10,

    TenantAdmin = 11,

    // Special permissions
    Owner = 20,

    Approve = 21,

    Reject = 22,

    Publish = 23,

    Archive = 24
}

/// <summary>
///     Type of access review/certification campaign
/// </summary>
public enum AccessReviewType
{
    None = 0,

    PermissionReview = 1,

    RoleReview = 2,

    ResourceAccessReview = 3,

    UserAccessReview = 4,

    ComplianceAttestation = 5
}

/// <summary>
///     Scope of access review campaign
/// </summary>
public enum AccessReviewScope
{
    None = 0,

    AllUsers = 1,

    Department = 2,

    Team = 3,

    Role = 4,

    Resource = 5,

    HighPrivilege = 6,

    External = 7,

    Custom = 99
}

/// <summary>
///     Status of access review campaign
/// </summary>
public enum AccessReviewStatus
{
    None = 0,

    Draft = 1,

    InProgress = 2,

    Completed = 3,

    Cancelled = 4,

    Expired = 5
}

/// <summary>
///     Status of individual review item
/// </summary>
public enum AccessReviewItemStatus
{
    None = 0,

    Pending = 1,

    Reviewed = 2,

    Approved = 3,

    Revoked = 4,

    Expired = 5
}

/// <summary>
///     Decision made on review item
/// </summary>
public enum AccessReviewDecision { None = 0, Approve = 1, Revoke = 2, ModifyAndApprove = 3 }

/// <summary>
///     Type of policy condition
/// </summary>
public enum PolicyConditionType
{
    None = 0,

    Time = 1,

    Environment = 2,

    Location = 3,

    Device = 4,

    RiskScore = 5,

    Custom = 99
}

/// <summary>
///     Action to take when policy condition matches
/// </summary>
public enum PolicyAction
{
    None = 0,

    Allow = 1,

    Deny = 2,

    Require2FA = 3,

    RequireApproval = 4,

    Log = 5,

    Throttle = 6
}

/// <summary>
///     Type of data masking to apply
/// </summary>
public enum MaskingType
{
    None = 0,

    Full = 1,

    Partial = 2,

    Hash = 3,

    Custom = 4,

    PatternMask = 5,

    Redact = 6
}

/// <summary>
///     Status of JIT elevation request
/// </summary>
public enum ElevationRequestStatus
{
    None = 0,

    Pending = 1,

    Approved = 2,

    Denied = 3,

    Active = 4,

    Expired = 5,

    Revoked = 6
}

/// <summary>
///     Type of SoD rule
/// </summary>
public enum SoDRuleType
{
    None = 0,

    PermissionConflict = 1,

    RoleConflict = 2,

    ResourceConflict = 3,

    BusinessProcessConflict = 4,

    FunctionalConflict = 5
}

/// <summary>
///     Severity of SoD rule
/// </summary>
public enum SoDSeverity { None = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }

/// <summary>
///     Status of SoD violation
/// </summary>
public enum SoDViolationStatus
{
    None = 0,

    Active = 1,

    Acknowledged = 2,

    Mitigated = 3,

    Resolved = 4,

    Excepted = 5,

    FalsePositive = 6
}

/// <summary>
///     Action taken to resolve SoD violation
/// </summary>
public enum SoDResolutionAction
{
    None = 0,

    RevokePermission = 1,

    RevokeRole = 2,

    GrantException = 3,

    ImplementCompensatingControl = 4,

    TransferOwnership = 5,

    NoAction = 6
}

/// <summary>
///     Type of delegated admin scope
/// </summary>
public enum DelegatedAdminScopeType
{
    None = 0,

    Department = 1,

    Team = 2,

    Role = 3,

    Resource = 4,

    Custom = 5
}

/// <summary>
///     Type of permission operation for audit
/// </summary>
public enum PermissionOperationType
{
    None = 0,

    Grant = 1,

    Revoke = 2,

    Update = 3,

    Delete = 4,

    Delegate = 5,

    ElevateJIT = 6,

    Review = 7
}

/// <summary>
///     ABAC policy effect
/// </summary>
public enum AbacPolicyEffect { None = 0, Allow = 1, Deny = 2 }

/// <summary>
///     Template change type for versioning
/// </summary>
public enum TemplateChangeType
{
    None = 0,

    Major = 1, // Breaking changes, incompatible with previous

    Minor = 2, // New features, backwards compatible

    Patch = 3, // Bug fixes, no new features

    Hotfix = 4 // Critical security/bug fixes
}

/// <summary>
///     Migration status
/// </summary>
public enum MigrationStatus
{
    None = 0,

    Planned = 1,

    Scheduled = 2,

    InProgress = 3,

    Completed = 4,

    Failed = 5,

    RolledBack = 6,

    Cancelled = 7
}

/// <summary>
///     Migration strategy
/// </summary>
public enum MigrationStrategy
{
    None = 0,

    Immediate = 1, // Execute immediately

    Phased = 2, // Execute in phases

    Manual = 3, // Require manual approval per user

    Scheduled = 4 // Execute at scheduled time
}

/// <summary>
///     Policy bundle type
/// </summary>
public enum PolicyBundleType
{
    None = 0,

    Permission = 1,

    Conditional = 2,

    DataMasking = 3,

    SoD = 4,

    AccessReview = 5,

    Composite = 6 // Combination of multiple types
}

/// <summary>
///     Policy bundle status
/// </summary>
public enum PolicyBundleStatus
{
    None = 0,

    Draft = 1,

    PendingApproval = 2,

    Approved = 3,

    Active = 4,

    Deprecated = 5,

    Revoked = 6
}

/// <summary>
///     Policy deployment status
/// </summary>
public enum PolicyDeploymentStatus
{
    None = 0,

    Pending = 1,

    Deploying = 2,

    Active = 3,

    Failed = 4,

    RolledBack = 5
}

/// <summary>
///     Policy registry action types
/// </summary>
public enum PolicyRegistryAction
{
    None = 0,

    Create = 1,

    Update = 2,

    Sign = 3,

    Approve = 4,

    Deploy = 5,

    Activate = 6,

    Deprecate = 7,

    Revoke = 8,

    Rollback = 9,

    Verify = 10,

    Export = 11,

    Import = 12
}

/// <summary>
///     Graph export formats
/// </summary>
public enum GraphExportFormat
{
    None = 0,

    DOT = 1, // Graphviz DOT format

    JSON = 2, // JSON graph format

    GraphML = 3 // GraphML XML format
}

/// <summary>
///     Impact severity level
/// </summary>
public enum ImpactSeverity { Low = 0, Medium = 1, High = 2, Critical = 3 }
