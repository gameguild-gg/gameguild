using System.Security.Cryptography;
using System.Text;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Represents an Attribute-Based Access Control (ABAC) policy
/// </summary>
public class AbacPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AbacPolicyEffect Effect { get; set; } = AbacPolicyEffect.Allow;

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; }

    // Attribute conditions stored as JSON
    public string? SubjectConditions { get; set; } // User attributes

    public string? ResourceConditions { get; set; } // Resource attributes

    public string? EnvironmentConditions { get; set; } // Environmental attributes

    public string? ActionConditions { get; set; } // Action/permission constraints

    public string? TargetResources { get; set; } // JSON array of resource types

    public string? TargetActions { get; set; } // JSON array of actions

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Check if policy is active
    /// </summary>
    public bool IsActive() { return IsEnabled; }

    /// <summary>
    ///     Enable the policy
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Disable the policy
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Set priority
    /// </summary>
    public void SetPriority(int priority)
    {
        if (priority < 0) throw new ArgumentException("Priority must be non-negative", nameof(priority));

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if policy is a deny policy
    /// </summary>
    public bool IsDenyPolicy() { return Effect == AbacPolicyEffect.Deny; }

    /// <summary>
    ///     Check if policy is an allow policy
    /// </summary>
    public bool IsAllowPolicy() { return Effect == AbacPolicyEffect.Allow; }
}

/// <summary>
///     Delegated administration scope definition
/// </summary>
public class DelegatedAdminScope
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public Guid AdminUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DelegatedAdminScopeType ScopeType { get; set; }

    // Scope definitions (JSON arrays)
    public string? AllowedResourceTypes { get; set; }

    public string? AllowedResourceIds { get; set; }

    public string? AllowedUserIds { get; set; }

    public string? AllowedDepartments { get; set; }

    public string? AllowedTeams { get; set; }

    public string? AllowedRoles { get; set; }

    // Permission constraints
    public string? GrantablePermissions { get; set; }

    public string? DeniedPermissions { get; set; }

    public bool CanManageUsers { get; set; }

    public bool CanManagePermissions { get; set; }

    public bool CanManageResources { get; set; }

    public bool CanViewAuditLogs { get; set; }

    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Check if scope is currently valid
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive) return false;

        var now = DateTime.UtcNow;

        return now >= StartsAt && (ExpiresAt == null || now < ExpiresAt);
    }

    /// <summary>
    ///     Check if admin can manage a specific user
    /// </summary>
    public bool CanManageUser(Guid userId)
    {
        if (!IsValid() || !CanManageUsers) return false;

        // TODO: Parse AllowedUserIds JSON and check
        return AllowedUserIds?.Contains(userId.ToString()) ?? false;
    }

    /// <summary>
    ///     Check if admin can manage a specific resource type
    /// </summary>
    public bool CanManageResourceType(string resourceType)
    {
        if (!IsValid() || !CanManageResources) return false;

        // TODO: Parse AllowedResourceTypes JSON and check
        return AllowedResourceTypes?.Contains(resourceType) ?? false;
    }

    /// <summary>
    ///     Activate the scope
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Deactivate the scope
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
///     Permission audit log entry
/// </summary>
public class PermissionAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public PermissionOperationType OperationType { get; set; }

    public Guid? UserId { get; set; } // User whose permissions were affected

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string? PermissionType { get; set; } // TODO: Link to PermissionType enum

    public string? PermissionDetails { get; set; } // JSON with permission details

    public string? OldValue { get; set; } // JSON with previous state

    public string? NewValue { get; set; } // JSON with new state

    public Guid PerformedBy { get; set; } // User who performed the action

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Reason { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Check if operation was successful
    /// </summary>
    public bool IsSuccessful() { return Success; }

    /// <summary>
    ///     Check if operation failed
    /// </summary>
    public bool IsFailed() { return !Success; }
}

/// <summary>
///     Represents a versioned snapshot of a permission template
/// </summary>
public class PermissionTemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TemplateId { get; set; }

    public int VersionNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string[ ] Permissions { get; set; } = Array.Empty<string>();

    public bool IsActive { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string? ChangeNotes { get; set; }

    public TemplateChangeType ChangeType { get; set; }

    public string[ ]? AddedPermissions { get; set; }

    public string[ ]? RemovedPermissions { get; set; }

    public string[ ]? UnchangedPermissions { get; set; }

    public int? PreviousVersion { get; set; }

    public string? Metadata { get; set; } // JSON

    public string? PermissionHash { get; set; }

    public string[ ]? Tags { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Calculate checksum of permission set
    /// </summary>
    public string CalculateHash()
    {
        var permString = string.Join(",", Permissions.OrderBy(p => p));
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(permString);
        var hash = sha256.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }
}

/// <summary>
///     Represents a migration plan for template version upgrades
/// </summary>
public class PermissionTemplateMigration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TemplateId { get; set; }

    public int FromVersion { get; set; }

    public int ToVersion { get; set; }

    public MigrationStatus Status { get; set; } = MigrationStatus.Planned;

    public MigrationStrategy Strategy { get; set; }

    public DateTime? ScheduledFor { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid InitiatedByUserId { get; set; }

    public Guid[ ]? AffectedTenantIds { get; set; }

    public Guid[ ]? AffectedUserIds { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public int SkippedCount { get; set; }

    public int TotalCount { get; set; }

    public string? Errors { get; set; } // JSON

    public string? Log { get; set; } // JSON

    public string? RollbackPlan { get; set; } // JSON

    public string? DryRunResult { get; set; } // JSON

    public bool IsDryRun { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Calculate progress percentage
    /// </summary>
    public double GetProgressPercentage() { return TotalCount > 0 ? (SuccessCount + FailureCount + SkippedCount) / (double) TotalCount * 100 : 0; }

    /// <summary>
    ///     Check if migration is complete
    /// </summary>
    public bool IsComplete() { return Status == MigrationStatus.Completed || Status == MigrationStatus.Failed || Status == MigrationStatus.RolledBack; }
}

/// <summary>
///     Represents a policy bundle in the central registry
/// </summary>
public class PolicyBundle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Version { get; set; } = "1.0.0";

    public PolicyBundleType BundleType { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public string? DigitalSignature { get; set; }

    public string? SignedBy { get; set; }

    public DateTime? SignedAt { get; set; }

    public PolicyBundleStatus Status { get; set; } = PolicyBundleStatus.Draft;

    public string PolicyData { get; set; } = string.Empty; // JSON

    public string? Metadata { get; set; } // JSON

    public bool IsGlobal { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveUntil { get; set; }

    public Guid? PreviousVersionId { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int DeploymentCount { get; set; }

    public DateTime? LastDeployedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Check if bundle is approved
    /// </summary>
    public bool IsApproved() { return Status == PolicyBundleStatus.Approved || Status == PolicyBundleStatus.Active; }

    /// <summary>
    ///     Check if bundle is active
    /// </summary>
    public bool IsActive() { return Status == PolicyBundleStatus.Active; }
}

/// <summary>
///     Tracks deployment of policy bundles
/// </summary>
public class PolicyBundleDeployment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BundleId { get; set; }

    public TenantId? TenantId { get; set; }

    public string Environment { get; set; } = string.Empty;

    public PolicyDeploymentStatus Status { get; set; } = PolicyDeploymentStatus.Pending;

    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;

    public Guid DeployedBy { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? RolledBackAt { get; set; }

    public Guid? RolledBackBy { get; set; }

    public string? RollbackReason { get; set; }

    public bool VerificationPassed { get; set; }

    public string? VerificationDetails { get; set; }

    public string? DeploymentNotes { get; set; }

    public PolicyBundle? Bundle { get; set; }

    /// <summary>
    ///     Check if deployment is active
    /// </summary>
    public bool IsActive() { return Status == PolicyDeploymentStatus.Active; }

    /// <summary>
    ///     Activate the deployment
    /// </summary>
    public void Activate()
    {
        Status = PolicyDeploymentStatus.Active;
        ActivatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Rollback the deployment
    /// </summary>
    public void Rollback(Guid userId, string reason)
    {
        Status = PolicyDeploymentStatus.RolledBack;
        RolledBackAt = DateTime.UtcNow;
        RolledBackBy = userId;
        RollbackReason = reason;
    }
}

/// <summary>
///     Audit log for policy registry operations
/// </summary>
public class PolicyRegistryAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? BundleId { get; set; }

    public PolicyRegistryAction Action { get; set; }

    public Guid PerformedBy { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }
}
