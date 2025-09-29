using GameGuild.Modules.Tenants;

namespace GameGuild.Source.Core.Tenants;

/// <summary>
/// Represents the assignment of a tenant role to a specific user
/// </summary>
[Table("UserTenantRoles")]
[Index(nameof(UserId), nameof(TenantRoleApplicationId), IsUnique = true)]
public class UserTenantRole : EntityBase, ITenantable {
    /// <summary>
    /// Reference to the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the tenant role application
    /// </summary>
    public Guid TenantRoleApplicationId { get; set; }
    public virtual TenantRoleApplication TenantRoleApplication { get; set; } = null!;

    /// <summary>
    /// When this role assignment becomes effective
    /// </summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this role assignment expires (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this role assignment is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User who assigned this role
    /// </summary>
    public Guid? AssignedByUserId { get; set; }

    /// <summary>
    /// Additional notes about this role assignment
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Explicitly implement ITenantable interface using inherited properties
    Tenant? ITenantable.Tenant {
        get => Tenant;
        set => Tenant = value;
    }

    bool ITenantable.IsGlobal => IsGlobal;
}