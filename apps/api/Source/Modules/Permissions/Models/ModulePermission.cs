using System.Text.Json;
using GameGuild.Common;

namespace GameGuild.Modules.Permissions;

/// <summary>
/// Module types that can have granular permissions
/// </summary>
public enum ModuleType {
  None = 0,
  TestingLab = 1,
  Projects = 2,
  Programs = 3,
  Courses = 4,
  Analytics = 5,
  UserManagement = 6,
  TenantManagement = 7,
  ContentManagement = 8,
  ApiManagement = 9,
  SystemAdministration = 10,
}

/// <summary>
/// Actions that can be performed within a module
/// </summary>
public enum ModuleAction {
  None = 0,
  // Basic CRUD operations
  Create = 1,
  Read = 2,
  Edit = 3,
  Delete = 4,

  // Administrative operations
  Manage = 5,
  Administer = 6,

  // Specific operations
  Execute = 7,
  Review = 8,
  Approve = 9,
  Publish = 10,
  Archive = 11,
  Restore = 12,

  // Testing Lab specific
  CreateSession = 20,
  DeleteSession = 21,
  ManageTesters = 22,
  ViewReports = 23,
  ExportData = 24,

  // Project specific
  ManageCollaborators = 30,
  SetPermissions = 31,
  ManageReleases = 32,

  // System specific
  ManageUsers = 40,
  ManageRoles = 41,
  ViewAuditLogs = 42,
  SystemConfiguration = 43,
}

/// <summary>
/// Predefined roles for Testing Lab module
/// </summary>
public enum TestingLabRole {
  None = 0,
  Admin = 1,      // Full access to everything
  Manager = 2,    // Can create/edit but not delete sessions, manage testers
  Coordinator = 3, // Can create sessions and manage testers 
  Tester = 4, // Can only participate in sessions
}

/// <summary>
/// Permission constraint for conditional permissions
/// </summary>
public class PermissionConstraint {
  public string Type { get; set; } = string.Empty; // "resource_owner", "same_tenant", "time_based", etc.
  public string Value { get; set; } = string.Empty; // Constraint value
  public DateTime? ExpiresAt { get; set; } // Optional expiration
}

/// <summary>
/// Simple permission template - defines what action can be performed on what resource type
/// </summary>
public class PermissionTemplate {
  public string Action { get; set; } = string.Empty; // "read", "create", "edit", "delete", etc.
  public string ResourceType { get; set; } = string.Empty; // "TestingSession", "Project", etc.
  public List<PermissionConstraint> Constraints { get; set; } = new();
}

/// <summary>
/// User permission assignment - links user to specific resources with specific actions
/// </summary>
[Table("UserPermissions")]
public class UserPermission : Entity {
  [Required]
  public Guid UserId { get; set; }

  public Guid? TenantId { get; set; }

  [Required]
  [MaxLength(50)]
  public string Action { get; set; } = string.Empty; // "read", "create", "edit", "delete"

  [Required]
  [MaxLength(100)]
  public string ResourceType { get; set; } = string.Empty; // "TestingSession", "Project", etc.

  public Guid? ResourceId { get; set; } // Specific resource ID, null for type-level permissions

  [MaxLength(100)]
  public string? GrantedByRole { get; set; } // Which role template granted this permission

  /// <summary>
  /// Serialized constraints as JSON
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string ConstraintsJson { get; set; } = "[]";

  /// <summary>
  /// Constraints for this permission
  /// </summary>
  [NotMapped]
  public List<PermissionConstraint> Constraints {
    get => string.IsNullOrEmpty(ConstraintsJson)
        ? new List<PermissionConstraint>()
        : JsonSerializer.Deserialize<List<PermissionConstraint>>(ConstraintsJson) ?? new List<PermissionConstraint>();
    set => ConstraintsJson = JsonSerializer.Serialize(value);
  }

  public DateTime? ExpiresAt { get; set; }
  public bool IsActive { get; set; } = true;
}

/// <summary>
/// Module permission definition
/// </summary>
public class ModulePermissionDefinition {
  public ModuleType Module { get; set; }
  public ModuleAction Action { get; set; }
  public List<PermissionConstraint> Constraints { get; set; } = new();
  public bool IsGranted { get; set; } = true;
  public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// User role assignment - simple mapping of user to role template
/// </summary>
[Table("SimpleUserRoleAssignments")]
public class SimpleUserRoleAssignment : Entity {
  [Required]
  public Guid UserId { get; set; }

  public Guid? TenantId { get; set; }

  [Required]
  [MaxLength(100)]
  public string RoleTemplateName { get; set; } = string.Empty; // "TestingLabAdmin", "ProjectManager", etc.

  public DateTime? ExpiresAt { get; set; }
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Navigation property to the role template
  /// </summary>
  public virtual RoleTemplate? RoleTemplate { get; set; }
}

/// <summary>
/// Simple role template that defines what permissions a user gets
/// </summary>
[Table("RoleTemplates")]
public class RoleTemplate : Entity {
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [MaxLength(500)]
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Serialized permission templates as JSON
  /// Example: [{"action": "read", "resourceType": "TestingSession"}, {"action": "create", "resourceType": "TestingSession"}]
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string PermissionTemplatesJson { get; set; } = "[]";

  /// <summary>
  /// Permission templates for this role
  /// </summary>
  [NotMapped]
  public List<PermissionTemplate> PermissionTemplates {
    get => string.IsNullOrEmpty(PermissionTemplatesJson)
        ? new List<PermissionTemplate>()
        : JsonSerializer.Deserialize<List<PermissionTemplate>>(PermissionTemplatesJson) ?? new List<PermissionTemplate>();
    set => PermissionTemplatesJson = JsonSerializer.Serialize(value);
  }

  public bool IsSystemRole { get; set; } // System roles cannot be modified

  /// <summary>
  /// Navigation property to user assignments
  /// </summary>
  public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
