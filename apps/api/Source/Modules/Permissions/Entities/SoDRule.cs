using GameGuild.Database;
using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a Separation of Duties (SoD) rule
/// </summary>
public class SoDRule : EntityBase<Guid>
{
    public new Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SoDRuleType RuleType { get; set; }
    public SoDSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string ConflictingPermissions { get; set; } = string.Empty;
    public string? ConflictingRoles { get; set; }
    public string? ConflictingResources { get; set; }
    public string? AllowedExceptions { get; set; }
    public bool RequireApproval { get; set; }
    public string? ApproverRoles { get; set; }
    public string? MitigationStrategy { get; set; }
    public int ViolationCount { get; set; }
    public DateTime? LastViolationDetected { get; set; }
    public Guid CreatedBy { get; set; }
    public ICollection<SoDViolation> Violations { get; set; } = new List<SoDViolation>();
}

/// <summary>
/// Represents a detected SoD violation
/// </summary>
public class SoDViolation : EntityBase<Guid>
{
    public Guid RuleId { get; set; }
    public Guid UserId { get; set; }
    public new Guid? TenantId { get; set; }
    public SoDViolationStatus Status { get; set; }
    public string ViolationDetails { get; set; } = string.Empty;
    public string ConflictingItems { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public Guid? DetectedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public SoDResolutionAction? ResolutionAction { get; set; }
    public bool IsException { get; set; }
    public string? ExceptionJustification { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public SoDRule Rule { get; set; } = null!;
}

public enum SoDRuleType
{
    PermissionConflict = 1,
    RoleConflict = 2,
    ResourceConflict = 3,
    BusinessProcessConflict = 4,
    FunctionalConflict = 5
}

public enum SoDSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum SoDViolationStatus
{
    Active = 1,
    Acknowledged = 2,
    Mitigated = 3,
    Resolved = 4,
    Excepted = 5,
    FalsePositive = 6
}

public enum SoDResolutionAction
{
    RevokePermission = 1,
    RevokeRole = 2,
    GrantException = 3,
    ImplementCompensatingControl = 4,
    TransferOwnership = 5,
    NoAction = 6
}
