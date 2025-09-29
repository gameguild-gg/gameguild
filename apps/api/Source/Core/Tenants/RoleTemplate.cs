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
  public virtual ICollection<TenantRoleApplication> Applications { get; set; } = [];
}