using GameGuild.Modules.Tenants;

namespace GameGuild.Source.Core.Tenants;

/// <summary>
/// Represents the application of a role template to a specific tenant
/// Allows customization of template-based roles per tenant
/// </summary>
[Table("TenantRoleApplications")]
[Index(nameof(TenantId), nameof(RoleTemplateId), IsUnique = true)]
public class TenantRoleApplication : EntityBase, ITenantable {
    /// <summary>
    /// Reference to the role template being applied
    /// </summary>
    public Guid RoleTemplateId { get; set; }
    public virtual RoleTemplate RoleTemplate { get; set; } = null!;

    /// <summary>
    /// Tenant where the role is applied
    /// </summary>
    public Guid? TenantId { get; set; }
    public override Tenant? Tenant { get; set; }

    /// <summary>
    /// Custom name for this role within the tenant (overrides template name)
    /// </summary>
    [MaxLength(200)]
    public string? CustomName { get; set; }

    /// <summary>
    /// Custom description for this role within the tenant
    /// </summary>
    [MaxLength(1000)]
    public string? CustomDescription { get; set; }

    /// <summary>
    /// Whether this role application is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON array of permission overrides/additions specific to this tenant
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? PermissionOverrides { get; set; }

    /// <summary>
    /// Indicates whether this resource is accessible across all tenants
    /// </summary>
    public override bool IsGlobal => Tenant == null;

    /// <summary>
    /// Navigation property for user role assignments
    /// </summary>
    public virtual ICollection<UserTenantRole> UserRoles { get; set; } = [];
}