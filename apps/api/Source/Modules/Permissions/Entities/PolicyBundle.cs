using GameGuild.Database;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a policy bundle in the central registry
/// </summary>
public class PolicyBundle : EntityBase<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public PolicyBundleType BundleType { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string? DigitalSignature { get; set; }
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public PolicyBundleStatus Status { get; set; }
    public string PolicyData { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int DeploymentCount { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public ICollection<PolicyBundleDeployment> Deployments { get; set; } = new List<PolicyBundleDeployment>();
}

/// <summary>
/// Tracks deployment of policy bundles
/// </summary>
public class PolicyBundleDeployment : EntityBase<Guid>
{
    public Guid BundleId { get; set; }
    public Guid? TenantId { get; set; }
    public string Environment { get; set; } = string.Empty;
    public PolicyDeploymentStatus Status { get; set; }
    public DateTime DeployedAt { get; set; }
    public Guid DeployedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
    public Guid? RolledBackBy { get; set; }
    public string? RollbackReason { get; set; }
    public bool VerificationPassed { get; set; }
    public string? VerificationDetails { get; set; }
    public string? DeploymentNotes { get; set; }
    public PolicyBundle Bundle { get; set; } = null!;
}

/// <summary>
/// Audit log for policy registry operations
/// </summary>
public class PolicyRegistryAuditLog : EntityBase<Guid>
{
    public Guid? BundleId { get; set; }
    public PolicyRegistryAction Action { get; set; }
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum PolicyBundleType
{
    Permission = 1,
    Conditional = 2,
    DataMasking = 3,
    SoD = 4,
    AccessReview = 5,
    Composite = 6
}

public enum PolicyBundleStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Active = 4,
    Deprecated = 5,
    Revoked = 6
}

public enum PolicyDeploymentStatus
{
    Pending = 1,
    Deploying = 2,
    Active = 3,
    Failed = 4,
    RolledBack = 5
}

public enum PolicyRegistryAction
{
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
