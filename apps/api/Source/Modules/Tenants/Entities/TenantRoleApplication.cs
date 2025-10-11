namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents the application of a role template to a specific tenant
/// This allows tenants to use predefined role templates and optionally customize them
/// </summary>
[Table("TenantRoleApplications")]
[Index(nameof(TenantId), nameof(RoleTemplateId), IsUnique = true, Name = "IX_TenantRoleApplications_TenantId_RoleTemplateId")]
[Index(nameof(RoleName), Name = "IX_TenantRoleApplications_RoleName")]
public class TenantRoleApplication : EntityBase, ITenantable
{
    /// <summary>
    /// The tenant this role application belongs to
    /// </summary>
    [Required]
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// The role template being applied
    /// </summary>
    [Required]
    public Guid RoleTemplateId { get; set; }

    /// <summary>
    /// Navigation property to the role template
    /// </summary>
    [ForeignKey(nameof(RoleTemplateId))]
    public virtual RoleTemplate? RoleTemplate { get; set; }

    /// <summary>
    /// Custom name for this role within the tenant (overrides template name)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Custom description for this role within the tenant (overrides template description)
    /// </summary>
    [MaxLength(500)]
    public string? CustomDescription { get; set; }

    /// <summary>
    /// Whether this role is active within the tenant
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Custom permissions for this role (overrides template permissions if set)
    /// If null, uses the template's permissions
    /// </summary>
    public PermissionType[]? CustomPermissions { get; set; }

    /// <summary>
    /// Date when this role was applied to the tenant
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who applied this role to the tenant
    /// </summary>
    public Guid? AppliedByUserId { get; set; }

    /// <summary>
    /// Navigation property to user-tenant role assignments
    /// </summary>
    public virtual ICollection<UserTenantRole> UserAssignments { get; set; } = new List<UserTenantRole>();

    /// <summary>
    /// Additional metadata for the role application
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets the effective permissions for this role application
    /// Returns custom permissions if set, otherwise returns template permissions
    /// </summary>
    public PermissionType[] GetEffectivePermissions()
    {
        return CustomPermissions ?? RoleTemplate?.Permissions ?? Array.Empty<PermissionType>();
    }

    /// <summary>
    /// Gets the effective description for this role application
    /// </summary>
    public string GetEffectiveDescription()
    {
        return CustomDescription ?? RoleTemplate?.Description ?? string.Empty;
    }
}
