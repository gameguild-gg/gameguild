using GameGuild.Modules.Permissions;

namespace GameGuild.Common.Services;

/// <summary>
/// Service for managing module-based permissions
/// Provides granular permission management for different modules like TestingLab, Projects, etc.
/// </summary>
public interface IModulePermissionService
{
    // ===== ROLE MANAGEMENT =====
    
    /// <summary>
    /// Assign a role to a user for a specific module
    /// </summary>
    Task<UserRoleAssignment> AssignRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName, List<PermissionConstraint>? constraints = null, DateTime? expiresAt = null);
    
    /// <summary>
    /// Revoke a role from a user for a specific module
    /// </summary>
    Task<bool> RevokeRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName);
    
    /// <summary>
    /// Get all roles assigned to a user for a specific module
    /// </summary>
    Task<List<UserRoleAssignment>> GetUserRolesAsync(Guid userId, Guid? tenantId, ModuleType module);
    
    /// <summary>
    /// Get all users with a specific role in a module
    /// </summary>
    Task<List<UserRoleAssignment>> GetUsersWithRoleAsync(Guid? tenantId, ModuleType module, string roleName);
    
    // ===== PERMISSION CHECKING =====
    
    /// <summary>
    /// Check if a user has a specific permission in a module
    /// </summary>
    Task<bool> HasModulePermissionAsync(Guid userId, Guid? tenantId, ModuleType module, ModuleAction action, Guid? resourceId = null);
    
    /// <summary>
    /// Get all effective permissions for a user in a module
    /// </summary>
    Task<List<ModulePermission>> GetUserModulePermissionsAsync(Guid userId, Guid? tenantId, ModuleType module);
    
    /// <summary>
    /// Get users who have a specific permission in a module
    /// </summary>
    Task<List<Guid>> GetUsersWithPermissionAsync(Guid? tenantId, ModuleType module, ModuleAction action);
    
    // ===== TESTING LAB SPECIFIC PERMISSIONS =====
    
    /// <summary>
    /// Check if user can create testing sessions
    /// </summary>
    Task<bool> CanCreateTestingSessionsAsync(Guid userId, Guid? tenantId);
    
    /// <summary>
    /// Check if user can delete testing sessions
    /// </summary>
    Task<bool> CanDeleteTestingSessionsAsync(Guid userId, Guid? tenantId);
    
    /// <summary>
    /// Check if user can manage testers
    /// </summary>
    Task<bool> CanManageTestersAsync(Guid userId, Guid? tenantId);
    
    /// <summary>
    /// Check if user can view testing reports
    /// </summary>
    Task<bool> CanViewTestingReportsAsync(Guid userId, Guid? tenantId);
    
    /// <summary>
    /// Check if user can export testing data
    /// </summary>
    Task<bool> CanExportTestingDataAsync(Guid userId, Guid? tenantId);
    
    /// <summary>
    /// Get all Testing Lab permissions for a user
    /// </summary>
    Task<TestingLabPermissions> GetUserTestingLabPermissionsAsync(Guid userId, Guid? tenantId);
    
    // ===== ROLE DEFINITION MANAGEMENT =====
    
    /// <summary>
    /// Create a new module role definition
    /// </summary>
    Task<ModuleRole> CreateRoleDefinitionAsync(string roleName, ModuleType module, string description, List<ModulePermission> permissions, int priority = 0);
    
    /// <summary>
    /// Update an existing role definition
    /// </summary>
    Task<ModuleRole> UpdateRoleDefinitionAsync(string roleName, ModuleType module, List<ModulePermission> permissions);
    
    /// <summary>
    /// Get all role definitions for a module
    /// </summary>
    Task<List<ModuleRole>> GetModuleRoleDefinitionsAsync(ModuleType module);
    
    /// <summary>
    /// Delete a role definition (only if no users are assigned to it)
    /// </summary>
    Task<bool> DeleteRoleDefinitionAsync(string roleName, ModuleType module);
}

/// <summary>
/// Testing Lab specific permissions result
/// </summary>
public class TestingLabPermissions
{
    public bool CanCreateSessions { get; set; }
    public bool CanEditSessions { get; set; }
    public bool CanDeleteSessions { get; set; }
    public bool CanManageTesters { get; set; }
    public bool CanViewReports { get; set; }
    public bool CanExportData { get; set; }
    public bool CanAdminister { get; set; }
    public List<string> AssignedRoles { get; set; } = new();
    public List<PermissionConstraint> Constraints { get; set; } = new();
}
