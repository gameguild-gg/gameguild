using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Database;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Source.Core.Tenants;

/// <summary>
/// Represents a role template that can be applied across tenants
/// Provides standardized permission sets for common organizational roles
/// </summary>
[Table("RoleTemplates")]
[Index(nameof(Name), IsUnique = true)]
public class RoleTemplate : EntityBase {
  /// <summary>
  /// Unique name of the role template
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// URL-friendly slug for the role template
  /// </summary>
  [Required]
  [MaxLength(100)]
  [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
  public string Slug { get; set; } = string.Empty;

  /// <summary>
  /// Human-readable display name
  /// </summary>
  [Required]
  [MaxLength(200)]
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// Detailed description of the role and its responsibilities
  /// </summary>
  [MaxLength(1000)]
  public string? Description { get; set; }

  /// <summary>
  /// Category of the role template (e.g., "Administrative", "Content", "Technical")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Category { get; set; } = string.Empty;

  /// <summary>
  /// Priority level for role hierarchy (higher number = higher priority)
  /// </summary>
  public int Priority { get; set; } = 0;

  /// <summary>
  /// Whether this role template is available for use
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Whether this is a system-defined role template that cannot be deleted
  /// </summary>
  public bool IsSystemRole { get; set; } = false;

  /// <summary>
  /// JSON array of permission definitions included in this role
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string PermissionDefinitions { get; set; } = "[]";

  /// <summary>
  /// Navigation property for role template applications
  /// </summary>
  public virtual ICollection<TenantRoleApplication> Applications { get; set; } = new List<TenantRoleApplication>();
}

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
  public virtual ICollection<UserTenantRole> UserRoles { get; set; } = new List<UserTenantRole>();
}

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
    get => this.Tenant;
    set => this.Tenant = value;
  }

  bool ITenantable.IsGlobal => this.IsGlobal;
}/// <summary>
/// Data transfer object for permission definitions
/// </summary>
public class PermissionDefinition {
  public string Resource { get; set; } = string.Empty;
  public string Action { get; set; } = string.Empty;
  public string? Scope { get; set; }
  public Dictionary<string, object>? Conditions { get; set; }
}