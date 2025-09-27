using System.Text.Json;


namespace GameGuild.Modules.Permissions;

/// <summary> Module-specific role definition that contains a set of permissions for a specific module </summary>
[Table("ModuleRoles")]
public class ModuleRole : EntityBase {
  [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;

  [Required] public ModuleType Module { get; set; }

  [Required] [MaxLength(500)] public string Description { get; set; } = string.Empty;

  /// <summary> JSON-serialized list of ModulePermissionDefinition objects </summary>
  [Column(TypeName = "jsonb")]
  public string PermissionsJson { get; set; } = "[]";

  /// <summary> Permissions defined in this module role </summary>
  [NotMapped]
  public List<ModulePermissionDefinition> Permissions {
    get => string.IsNullOrEmpty(PermissionsJson) ? [] : JsonSerializer.Deserialize<List<ModulePermissionDefinition>>(PermissionsJson) ?? [];
    set => PermissionsJson = JsonSerializer.Serialize(value);
  }

  public int Priority { get; set; }

  public bool IsSystemRole { get; set; }

  // Tenant support (TenantId is in BaseEntity as part of ITenantable)
  public Guid? TenantId { get; set; }

  // Navigation properties
  public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; } = [];
}