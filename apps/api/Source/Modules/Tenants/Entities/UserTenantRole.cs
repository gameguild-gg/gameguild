namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents the assignment of a user to a role within a specific tenant
/// Implements explicit ITenantable interface for tenant isolation
/// </summary>
[Table("UserTenantRoles")]
[Index(nameof(UserId), nameof(TenantId), nameof(TenantRoleApplicationId), IsUnique = true, Name = "IX_UserTenantRoles_User_Tenant_Role")]
[Index(nameof(TenantId), Name = "IX_UserTenantRoles_TenantId")]
[Index(nameof(UserId), Name = "IX_UserTenantRoles_UserId")]
public class UserTenantRole : EntityBase, ITenantable
{
    /// <summary>
    /// The user being assigned the role
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    /// <summary>
    /// The tenant context for this role assignment
    /// </summary>
    [Required]
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// The role application being assigned to the user
    /// </summary>
    [Required]
    public Guid TenantRoleApplicationId { get; set; }

    /// <summary>
    /// Navigation property to the tenant role application
    /// </summary>
    [ForeignKey(nameof(TenantRoleApplicationId))]
    public virtual TenantRoleApplication? TenantRoleApplication { get; set; }

    /// <summary>
    /// Date when the role was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who assigned this role
    /// </summary>
    public Guid? AssignedByUserId { get; set; }

    /// <summary>
    /// Whether this role assignment is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional expiration date for the role assignment
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Custom permissions for this specific user-role assignment (overrides role permissions)
    /// </summary>
    public PermissionType[]? CustomPermissions { get; set; }

    /// <summary>
    /// Additional metadata for the role assignment
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Explicit implementation of ITenantable.TenantId
    /// This pattern allows for explicit interface implementation while maintaining
    /// a strongly-typed TenantId property
    /// </summary>
    Guid ITenantable.TenantId => TenantId;

    /// <summary>
    /// Checks if the role assignment is currently valid
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
        return true;
    }

    /// <summary>
    /// Gets the effective permissions for this user-role assignment
    /// Returns custom permissions if set, otherwise returns role application permissions
    /// </summary>
    public PermissionType[] GetEffectivePermissions()
    {
        return CustomPermissions ?? TenantRoleApplication?.GetEffectivePermissions() ?? Array.Empty<PermissionType>();
    }

    /// <summary>
    /// Extends the expiration date of the role assignment
    /// </summary>
    public void ExtendExpiration(DateTime newExpirationDate)
    {
        if (newExpirationDate <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(newExpirationDate));

        ExpiresAt = newExpirationDate;
        Touch();
    }

    /// <summary>
    /// Revokes the role assignment
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Activates the role assignment
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }
}
