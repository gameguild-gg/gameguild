namespace GameGuild.Modules.Tenants;

/// <summary>
/// Role template definition for creating reusable role patterns across tenants
/// A role template is a blueprint that defines a set of permissions and behaviors
/// </summary>
[Table("RoleTemplates")]
[Index(nameof(Name), IsUnique = true, Name = "IX_RoleTemplates_Name")]
[Index(nameof(IsSystemTemplate), Name = "IX_RoleTemplates_IsSystemTemplate")]
public class RoleTemplate : EntityBase
{
    /// <summary>
    /// Unique name of the role template
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role template
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display name for the role (user-friendly)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system-defined template (cannot be modified/deleted)
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Whether this template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Permission types included in this role template
    /// </summary>
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Category for organizing role templates
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Priority/level of the role (higher = more privileged)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Maximum number of users that can have this role per tenant (null = unlimited)
    /// </summary>
    public int? MaxUsersPerTenant { get; set; }

    /// <summary>
    /// Whether this role can be assigned by tenant admins
    /// </summary>
    public bool CanBeAssignedByTenantAdmin { get; set; } = true;

    /// <summary>
    /// Additional metadata for the template
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Navigation property to tenant role applications
    /// </summary>
    public virtual ICollection<TenantRoleApplication> TenantApplications { get; set; } = new List<TenantRoleApplication>();

    /// <summary>
    /// Predefined system role templates
    /// </summary>
    public static class SystemRoleTemplates
    {
        public static readonly RoleTemplate TenantOwner = new()
        {
            Name = "TenantOwner",
            DisplayName = "Tenant Owner",
            Description = "Full administrative control over the tenant",
            IsSystemTemplate = true,
            Category = "Administrative",
            Priority = 1000,
            MaxUsersPerTenant = null,
            CanBeAssignedByTenantAdmin = false,
            Permissions = new[]
            {
                PermissionType.SystemAdmin,
                PermissionType.TenantAdmin,
                PermissionType.Create,
                PermissionType.Edit,
                PermissionType.Delete,
                PermissionType.HardDelete,
                PermissionType.Manage,
                PermissionType.Configure
            }
        };

        public static readonly RoleTemplate TenantAdmin = new()
        {
            Name = "TenantAdmin",
            DisplayName = "Tenant Administrator",
            Description = "Administrative access to tenant resources",
            IsSystemTemplate = true,
            Category = "Administrative",
            Priority = 900,
            MaxUsersPerTenant = null,
            CanBeAssignedByTenantAdmin = true,
            Permissions = new[]
            {
                PermissionType.TenantAdmin,
                PermissionType.Create,
                PermissionType.Edit,
                PermissionType.Delete,
                PermissionType.Manage,
                PermissionType.Configure
            }
        };

        public static readonly RoleTemplate TenantModerator = new()
        {
            Name = "TenantModerator",
            DisplayName = "Tenant Moderator",
            Description = "Moderation capabilities within tenant",
            IsSystemTemplate = true,
            Category = "Moderation",
            Priority = 700,
            MaxUsersPerTenant = null,
            CanBeAssignedByTenantAdmin = true,
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Edit,
                PermissionType.Delete,
                PermissionType.Review,
                PermissionType.Flag,
                PermissionType.Hide,
                PermissionType.Ban
            }
        };

        public static readonly RoleTemplate TenantMember = new()
        {
            Name = "TenantMember",
            DisplayName = "Tenant Member",
            Description = "Standard member access to tenant resources",
            IsSystemTemplate = true,
            Category = "Standard",
            Priority = 500,
            MaxUsersPerTenant = null,
            CanBeAssignedByTenantAdmin = true,
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Reply,
                PermissionType.Vote,
                PermissionType.Share,
                PermissionType.Create
            }
        };

        public static readonly RoleTemplate TenantGuest = new()
        {
            Name = "TenantGuest",
            DisplayName = "Tenant Guest",
            Description = "Limited guest access to tenant resources",
            IsSystemTemplate = true,
            Category = "Standard",
            Priority = 100,
            MaxUsersPerTenant = null,
            CanBeAssignedByTenantAdmin = true,
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Reply
            }
        };
    }
}
