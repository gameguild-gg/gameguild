namespace GameGuild.Modules.Permissions;

/// <summary>
/// Module types that can have granular permissions
/// </summary>
public enum ModuleType
{
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
public enum ModuleAction
{
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
public enum TestingLabRole
{
    Admin = 1,      // Full access to everything
    Manager = 2,    // Can create/edit but not delete sessions, manage testers
    Coordinator = 3, // Can create sessions and manage testers 
    Tester = 4, // Can only participate in sessions
}

/// <summary>
/// Permission constraint for conditional permissions
/// </summary>
public class PermissionConstraint
{
    public string Type { get; set; } = string.Empty; // "resource_owner", "same_tenant", "time_based", etc.
    public string Value { get; set; } = string.Empty; // Constraint value
    public DateTime? ExpiresAt { get; set; } // Optional expiration
}

/// <summary>
/// Module permission definition
/// </summary>
public class ModulePermission
{
    public ModuleType Module { get; set; }
    public ModuleAction Action { get; set; }
    public List<PermissionConstraint> Constraints { get; set; } = new();
    public bool IsGranted { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// User role assignment for a specific module
/// </summary>
public class UserRoleAssignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; } // Null for global assignments
    public ModuleType Module { get; set; }
    public string RoleName { get; set; } = string.Empty; // "TestingLabAdmin", "TestingLabManager", etc.
    public List<PermissionConstraint> Constraints { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Module role definition
/// </summary>
public class ModuleRole
{
    public string Name { get; set; } = string.Empty;
    public ModuleType Module { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ModulePermission> Permissions { get; set; } = new();
    public int Priority { get; set; } // Higher priority roles override lower ones
    public bool IsSystemRole { get; set; } = false; // System roles cannot be modified
}
