using GameGuild.Modules.Resources;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents a tenant in a multi-tenant system
/// Inherits from Resource to provide UUID IDs, version control, timestamps, and soft delete functionality
/// Enhanced with modu-state patterns for comprehensive tenant management
/// </summary>
[Table("Tenants")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
public class Tenant : Resource {
  /// <summary>
  /// Name of the tenant
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [MaxLength(500)]
  public new string? Description { get; set; }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Whether this is the default tenant (for null tenant scenarios)
  /// </summary>
  public bool IsDefault { get; set; } = false;

  /// <summary>
  /// Slug for the tenant (URL-friendly unique identifier)
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string Slug { get; set; } = string.Empty;

  /// <summary>
  /// Administrative email for the tenant
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string AdminEmail { get; set; } = string.Empty;

  /// <summary>
  /// Navigation property to tenant permissions and user memberships
  /// </summary>
  public virtual ICollection<TenantPermission> TenantPermissions { get; set; } = new List<TenantPermission>();

  /// <summary>
  /// Navigation property to tenant settings
  /// </summary>
  public virtual TenantSettings? Settings { get; set; }

  /// <summary>
  /// Default constructor
  /// </summary>
  public Tenant() { }

  /// <summary>
  /// Activate the tenant
  /// </summary>
  public void Activate() {
    IsActive = true;
    Touch();
  }

  /// <summary>
  /// Deactivate the tenant
  /// </summary>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }

  /// <summary>
  /// Update tenant information
  /// </summary>
  public void Update(string name, string? description = null) {
    Name = name;
    Description = description;
    Touch();
  }
}
