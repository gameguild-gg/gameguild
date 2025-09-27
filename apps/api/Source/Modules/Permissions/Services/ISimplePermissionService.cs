using GameGuild.Modules.Permissions;

namespace GameGuild.Services;

/// <summary> Simple permission service - roles are just templates that create individual user permissions </summary>
public interface ISimplePermissionService {
    // Role Template Management - TEMPORARILY DISABLED DUE TO TYPE CONFLICTS
    // Task<CoreRoleTemplate> CreateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions);
    // Task<CoreRoleTemplate> UpdateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions);
    // Task<CoreRoleTemplate> UpdateRoleTemplateAsync(string currentName, string newName, string description, List<PermissionTemplate> permissions);
    // Task<CoreRoleTemplate> UpdateRoleTemplateAsync(Guid id, string name, string description, List<PermissionTemplate> permissions);

    Task<bool> DeleteRoleTemplateAsync(string name);
    Task<bool> DeleteRoleTemplateAsync(Guid id);

    // Task<List<CoreRoleTemplate>> GetRoleTemplatesAsync();
    // Task<CoreRoleTemplate?> GetRoleTemplateAsync(string name);
    // Task<CoreRoleTemplate?> GetRoleTemplateAsync(Guid id);

    // User Role Assignment
    Task AssignRoleToUserAsync(Guid userId, Guid? tenantId, string roleName, DateTime? expiresAt = null);

    Task RevokeRoleFromUserAsync(Guid userId, Guid? tenantId, string roleName);

    Task<List<UserRoleAssignment>> GetUserRolesAsync(Guid userId, Guid? tenantId);

    // User Permission Management
    Task GrantPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null, string? grantedByRole = null, DateTime? expiresAt = null);

    Task RevokePermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null);

    Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null);

    Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId, Guid? tenantId, string? resourceType = null);

    // Convenience Methods for Testing Lab
    Task<bool> CanCreateTestingSessionsAsync(Guid userId, Guid? tenantId);

    Task<bool> CanEditTestingSessionAsync(Guid userId, Guid? tenantId, Guid sessionId);

    Task<bool> CanDeleteTestingSessionAsync(Guid userId, Guid? tenantId, Guid sessionId);
}