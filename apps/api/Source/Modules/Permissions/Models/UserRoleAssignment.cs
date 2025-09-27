using System.Collections.ObjectModel;
using System.Text.Json;


namespace GameGuild.Modules.Permissions;

/// <summary> User role assignment for module-specific roles - matches the AddModulePermissionTables migration </summary>
[Table("UserRoleAssignments")]
public class UserRoleAssignment : EntityBase {
  [Required] public Guid UserId { get; set; }

  // ITenantable implementation
  public Guid? TenantId { get; set; }

  [Required] public ModuleType Module { get; set; }

  [Required] [MaxLength(100)] public string RoleName { get; set; } = string.Empty;

  /// <summary> JSON-serialized list of PermissionConstraint objects </summary>
  [Column(TypeName = "jsonb")]
  public string ConstraintsJson { get; set; } = "[]";

  /// <summary> Constraints that apply to this role assignment </summary>
  [NotMapped]
  public ReadOnlyCollection<PermissionConstraint> Constraints {
    get => new ReadOnlyCollection<PermissionConstraint>(string.IsNullOrEmpty(ConstraintsJson) ? [] : JsonSerializer.Deserialize<List<PermissionConstraint>>(ConstraintsJson) ?? []);
  }

  public DateTime? ExpiresAt { get; set; }

  public bool IsActive { get; set; } = true;

  // Foreign key to ModuleRole
  public Guid? RoleId { get; set; }

  public virtual ModuleRole? Role { get; set; }

  /// <summary> Sets the constraints for this role assignment </summary>
  public void SetConstraints(IEnumerable<PermissionConstraint> constraints) { ConstraintsJson = JsonSerializer.Serialize(constraints); }
}