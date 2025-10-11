using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a delegated administrative scope where a user is granted administrative permissions
/// within a specific tenant scope (e.g., department, team, project)
/// </summary>
[Table("DelegatedAdminScopes")]
[Index(nameof(DelegatorUserId), Name = "IX_DelegatedAdminScopes_DelegatorUserId")]
[Index(nameof(DelegatedUserId), Name = "IX_DelegatedAdminScopes_DelegatedUserId")]
[Index(nameof(TenantId), Name = "IX_DelegatedAdminScopes_TenantId")]
[Index(nameof(ScopeType), Name = "IX_DelegatedAdminScopes_ScopeType")]
[Index(nameof(IsActive), Name = "IX_DelegatedAdminScopes_IsActive")]
[Index(nameof(ExpiresAt), Name = "IX_DelegatedAdminScopes_ExpiresAt")]
public class DelegatedAdminScope : EntityBase
{
    /// <summary>
    /// User who is delegating the administrative privileges
    /// </summary>
    public Guid DelegatorUserId { get; set; }

    /// <summary>
    /// User receiving the delegated administrative privileges
    /// </summary>
    public Guid DelegatedUserId { get; set; }

    /// <summary>
    /// Tenant in which this delegation is effective
    /// </summary>
    // TenantId inherited from EntityBase (no override needed)

    /// <summary>
    /// Type of scope (Department, Team, Project, ContentType, etc.)
    /// </summary>
    [MaxLength(50)]
    public string ScopeType { get; set; } = null!;

    /// <summary>
    /// Identifier of the specific scope (e.g., DepartmentId, TeamId)
    /// </summary>
    public Guid? ScopeId { get; set; }

    /// <summary>
    /// Name of the scope for display purposes
    /// </summary>
    [MaxLength(200)]
    public string ScopeName { get; set; } = null!;

    /// <summary>
    /// Administrative permissions being delegated
    /// </summary>
    public PermissionType[] DelegatedPermissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Whether delegation can be further delegated (chained)
    /// </summary>
    public bool AllowSubDelegation { get; set; }

    /// <summary>
    /// Maximum depth of sub-delegation chain (0 = no sub-delegation)
    /// </summary>
    public int MaxDelegationDepth { get; set; }

    /// <summary>
    /// Current depth in delegation chain (0 = original delegation)
    /// </summary>
    public int CurrentDepth { get; set; }

    /// <summary>
    /// Parent delegation if this is a sub-delegation
    /// </summary>
    public Guid? ParentDelegationId { get; set; }

    /// <summary>
    /// Whether this delegation is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for the delegation
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Optional expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Date when delegation was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// User who revoked the delegation
    /// </summary>
    public Guid? RevokedByUserId { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    [MaxLength(500)]
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Additional constraints or rules (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Constraints { get; set; }

    /// <summary>
    /// Check if delegation is currently valid
    /// </summary>
    public bool IsValid =>
        IsActive &&
        !RevokedAt.HasValue &&
        (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow) &&
        DeletedAt == null;

    /// <summary>
    /// Check if this scope matches a specific resource
    /// </summary>
    public bool MatchesResource(string resourceType, Guid? resourceId)
    {
        if (ScopeType != resourceType) return false;
        if (ScopeId == null) return true; // Wildcard scope
        return ScopeId == resourceId;
    }

    /// <summary>
    /// Activate the delegation
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the delegation
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revoke the delegation
    /// </summary>
    public void Revoke(Guid revokedByUserId, string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if user can sub-delegate
    /// </summary>
    public bool CanSubDelegate()
    {
        return AllowSubDelegation && CurrentDepth < MaxDelegationDepth;
    }
}

/// <summary>
/// Scope types for delegated administration
/// </summary>
public static class DelegatedAdminScopeTypes
{
    public const string Tenant = "Tenant";
    public const string Department = "Department";
    public const string Team = "Team";
    public const string Project = "Project";
    public const string ContentType = "ContentType";
    public const string ResourceGroup = "ResourceGroup";
    public const string All = "*";
}
