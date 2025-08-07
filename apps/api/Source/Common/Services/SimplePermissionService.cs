using GameGuild.Database;
using GameGuild.Modules.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Common.Services;

/// <summary>
/// Simple permission service - roles are just templates that create individual user permissions
/// </summary>
public interface ISimplePermissionService
{
    // Role Template Management
    Task<RoleTemplate> CreateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions);
    Task<RoleTemplate> UpdateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions);
    Task<bool> DeleteRoleTemplateAsync(string name);
    Task<List<RoleTemplate>> GetRoleTemplatesAsync();
    Task<RoleTemplate?> GetRoleTemplateAsync(string name);
    
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

public class SimplePermissionService(ApplicationDbContext context, ILogger<SimplePermissionService> logger) : ISimplePermissionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<SimplePermissionService> _logger = logger;

    // ===== ROLE TEMPLATE MANAGEMENT =====
    
    public async Task<RoleTemplate> CreateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions)
    {
        var existingRole = await _context.RoleTemplates.FirstOrDefaultAsync(r => r.Name == name);
        if (existingRole != null)
        {
            throw new InvalidOperationException($"Role template '{name}' already exists");
        }

        var roleTemplate = new RoleTemplate
        {
            Name = name,
            Description = description,
            PermissionTemplates = permissions,
            IsSystemRole = false
        };

        _context.RoleTemplates.Add(roleTemplate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created role template '{RoleName}' with {PermissionCount} permissions", name, permissions.Count);
        return roleTemplate;
    }

    public async Task<RoleTemplate> UpdateRoleTemplateAsync(string name, string description, List<PermissionTemplate> permissions)
    {
        var roleTemplate = await _context.RoleTemplates.FirstOrDefaultAsync(r => r.Name == name);
        if (roleTemplate == null)
        {
            throw new InvalidOperationException($"Role template '{name}' not found");
        }

        if (roleTemplate.IsSystemRole)
        {
            throw new InvalidOperationException($"Cannot modify system role '{name}'");
        }

        roleTemplate.Description = description;
        roleTemplate.PermissionTemplates = permissions;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated role template '{RoleName}' with {PermissionCount} permissions", name, permissions.Count);
        return roleTemplate;
    }

    public async Task<bool> DeleteRoleTemplateAsync(string name)
    {
        var roleTemplate = await _context.RoleTemplates.FirstOrDefaultAsync(r => r.Name == name);
        if (roleTemplate == null) return false;

        if (roleTemplate.IsSystemRole)
        {
            throw new InvalidOperationException($"Cannot delete system role '{name}'");
        }

        // Check if any users are assigned to this role
        var hasAssignments = await _context.UserRoleAssignments.AnyAsync(r => r.RoleName == name && r.IsActive);
        if (hasAssignments)
        {
            throw new InvalidOperationException($"Cannot delete role '{name}' because users are still assigned to it");
        }

        _context.RoleTemplates.Remove(roleTemplate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted role template '{RoleName}'", name);
        return true;
    }

    public async Task<List<RoleTemplate>> GetRoleTemplatesAsync()
    {
        return await _context.RoleTemplates.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<RoleTemplate?> GetRoleTemplateAsync(string name)
    {
        return await _context.RoleTemplates.FirstOrDefaultAsync(r => r.Name == name);
    }

    // ===== USER ROLE ASSIGNMENT =====

    public async Task AssignRoleToUserAsync(Guid userId, Guid? tenantId, string roleName, DateTime? expiresAt = null)
    {
        // Get the role template
        var roleTemplate = await GetRoleTemplateAsync(roleName);
        if (roleTemplate == null)
        {
            throw new InvalidOperationException($"Role template '{roleName}' not found");
        }

        // Remove existing assignment if it exists
        var existingAssignment = await _context.UserRoleAssignments
            .FirstOrDefaultAsync(r => r.UserId == userId && r.TenantId == tenantId && r.RoleName == roleName);
        
        if (existingAssignment != null)
        {
            _context.UserRoleAssignments.Remove(existingAssignment);
        }

        // Create new assignment
        var assignment = new UserRoleAssignment
        {
            UserId = userId,
            TenantId = tenantId,
            RoleName = roleName,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        _context.UserRoleAssignments.Add(assignment);

        // Create individual permissions based on the role template
        foreach (var permissionTemplate in roleTemplate.PermissionTemplates)
        {
            // Remove existing permissions granted by this role
            var existingPermissions = await _context.UserPermissions
                .Where(p => p.UserId == userId && p.TenantId == tenantId && 
                           p.Action == permissionTemplate.Action && 
                           p.ResourceType == permissionTemplate.ResourceType &&
                           p.GrantedByRole == roleName)
                .ToListAsync();
            
            _context.UserPermissions.RemoveRange(existingPermissions);

            // Create new permission
            var permission = new UserPermission
            {
                UserId = userId,
                TenantId = tenantId,
                Action = permissionTemplate.Action,
                ResourceType = permissionTemplate.ResourceType,
                ResourceId = null, // Role-based permissions are type-level, not resource-specific
                GrantedByRole = roleName,
                Constraints = permissionTemplate.Constraints,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            _context.UserPermissions.Add(permission);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned role '{RoleName}' to user {UserId}, creating {PermissionCount} permissions", 
            roleName, userId, roleTemplate.PermissionTemplates.Count);
    }

    public async Task RevokeRoleFromUserAsync(Guid userId, Guid? tenantId, string roleName)
    {
        // Remove role assignment
        var assignment = await _context.UserRoleAssignments
            .FirstOrDefaultAsync(r => r.UserId == userId && r.TenantId == tenantId && r.RoleName == roleName);
        
        if (assignment != null)
        {
            _context.UserRoleAssignments.Remove(assignment);
        }

        // Remove all permissions granted by this role
        var permissions = await _context.UserPermissions
            .Where(p => p.UserId == userId && p.TenantId == tenantId && p.GrantedByRole == roleName)
            .ToListAsync();

        _context.UserPermissions.RemoveRange(permissions);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked role '{RoleName}' from user {UserId}, removing {PermissionCount} permissions", 
            roleName, userId, permissions.Count);
    }

    public async Task<List<UserRoleAssignment>> GetUserRolesAsync(Guid userId, Guid? tenantId)
    {
        return await _context.UserRoleAssignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.IsActive)
            .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.RoleName)
            .ToListAsync();
    }

    // ===== USER PERMISSION MANAGEMENT =====

    public async Task GrantPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null, string? grantedByRole = null, DateTime? expiresAt = null)
    {
        // Remove existing permission if it exists
        var existingPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && 
                                    p.Action == action && p.ResourceType == resourceType && p.ResourceId == resourceId);
        
        if (existingPermission != null)
        {
            _context.UserPermissions.Remove(existingPermission);
        }

        // Create new permission
        var permission = new UserPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            GrantedByRole = grantedByRole,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        _context.UserPermissions.Add(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Granted permission '{Action}' on '{ResourceType}' to user {UserId}", action, resourceType, userId);
    }

    public async Task RevokePermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null)
    {
        var permission = await _context.UserPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && 
                                    p.Action == action && p.ResourceType == resourceType && p.ResourceId == resourceId);
        
        if (permission != null)
        {
            _context.UserPermissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Revoked permission '{Action}' on '{ResourceType}' from user {UserId}", action, resourceType, userId);
        }
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null)
    {
        // Check for specific resource permission first
        if (resourceId.HasValue)
        {
            var specificPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.TenantId == tenantId && 
                              p.Action == action && p.ResourceType == resourceType && 
                              p.ResourceId == resourceId && p.IsActive &&
                              (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));
            
            if (specificPermission) return true;
        }

        // Check for type-level permission
        return await _context.UserPermissions
            .AnyAsync(p => p.UserId == userId && p.TenantId == tenantId && 
                          p.Action == action && p.ResourceType == resourceType && 
                          p.ResourceId == null && p.IsActive &&
                          (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId, Guid? tenantId, string? resourceType = null)
    {
        var query = _context.UserPermissions
            .Where(p => p.UserId == userId && p.TenantId == tenantId && p.IsActive)
            .Where(p => p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow);

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(p => p.ResourceType == resourceType);
        }

        return await query.OrderBy(p => p.ResourceType).ThenBy(p => p.Action).ToListAsync();
    }

    // ===== CONVENIENCE METHODS FOR TESTING LAB =====

    public async Task<bool> CanCreateTestingSessionsAsync(Guid userId, Guid? tenantId)
    {
        return await HasPermissionAsync(userId, tenantId, "create", "TestingSession");
    }

    public async Task<bool> CanEditTestingSessionAsync(Guid userId, Guid? tenantId, Guid sessionId)
    {
        return await HasPermissionAsync(userId, tenantId, "edit", "TestingSession", sessionId) ||
               await HasPermissionAsync(userId, tenantId, "edit", "TestingSession");
    }

    public async Task<bool> CanDeleteTestingSessionAsync(Guid userId, Guid? tenantId, Guid sessionId)
    {
        return await HasPermissionAsync(userId, tenantId, "delete", "TestingSession", sessionId) ||
               await HasPermissionAsync(userId, tenantId, "delete", "TestingSession");
    }
}
