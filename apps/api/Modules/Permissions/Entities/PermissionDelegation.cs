namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Permission delegation allows users to delegate their permissions to other users
/// </summary>
[Table("PermissionDelegations")]
[Index(nameof(DelegatorUserId), Name = "IX_PermissionDelegations_DelegatorUserId")]
[Index(nameof(DelegateUserId), Name = "IX_PermissionDelegations_DelegateUserId")]
[Index(nameof(TenantId), Name = "IX_PermissionDelegations_TenantId")]
[Index(nameof(ResourceId), Name = "IX_PermissionDelegations_ResourceId")]
[Index(nameof(ExpiresAt), Name = "IX_PermissionDelegations_ExpiresAt")]
[Index(nameof(IsActive), Name = "IX_PermissionDelegations_IsActive")]
public class PermissionDelegation : EntityBase
{
    /// <summary>
    /// User who is delegating their permissions
    /// </summary>
    public Guid DelegatorUserId { get; set; }

    /// <summary>
    /// User who is receiving the delegated permissions
    /// </summary>
    public Guid DelegateUserId { get; set; }

    /// <summary>
    /// Tenant where the delegation applies (null for global delegation)
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Specific resource the delegation applies to (null for tenant-wide delegation)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Permissions being delegated
    /// </summary>
    public PermissionType[] DelegatedPermissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// When the delegation becomes active 
    /// </summary>
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the delegation expires (null for no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the delegate can further delegate these permissions
    /// </summary>
    public bool CanSubDelegate { get; set; } = false;

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
    /// Additional conditions or restrictions for the delegation
    /// </summary>
    public Dictionary<string, object>? Conditions { get; set; }

    /// <summary>
    /// Maximum number of times delegated permissions can be used
    /// </summary>
    public int? UsageLimit { get; set; }

    /// <summary>
    /// Number of times the delegated permissions have been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Check if delegation is currently valid and active
    /// </summary>
    public bool IsValidNow => IsActive
        && StartsAt <= DateTime.UtcNow
        && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow)
        && (UsageLimit == null || UsageCount < UsageLimit);

    /// <summary>
    /// Check if delegation allows a specific permission
    /// </summary>
    public bool AllowsPermission(PermissionType permission)
    {
        return IsValidNow && DelegatedPermissions.Contains(permission);
    }

    /// <summary>
    /// Record usage of the delegation
    /// </summary>
    public void RecordUsage()
    {
        UsageCount++;

        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
        {
            IsActive = false;
        }
    }
}