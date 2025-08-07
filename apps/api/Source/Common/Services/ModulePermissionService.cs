using GameGuild.Common.Services;
using GameGuild.Database;
using GameGuild.Modules.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Common.Services;

/// <summary>
/// Implementation of module-based permission service using Entity Framework
/// </summary>
public class ModulePermissionService(ApplicationDbContext context, ILogger<ModulePermissionService> logger) : IModulePermissionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<ModulePermissionService> _logger = logger;
    
    // ===== ROLE MANAGEMENT =====
    
    public async Task<UserRoleAssignment> AssignRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName, List<PermissionConstraint>? constraints = null, DateTime? expiresAt = null)
    {
        // Remove existing assignment for this user/tenant/module/role combination
        var existingAssignments = await _context.UserRoleAssignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.RoleName == roleName)
            .ToListAsync();
        
        _context.UserRoleAssignments.RemoveRange(existingAssignments);
        
        var assignment = new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Module = module,
            RoleName = roleName,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
        };
        
        // Set constraints using the method
        assignment.SetConstraints(constraints ?? new List<PermissionConstraint>());
        
        _context.UserRoleAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Assigned role {RoleName} to user {UserId} for module {Module} in tenant {TenantId}", 
            roleName, userId, module, tenantId);
        
        return assignment;
    }
    
    public async Task<bool> RevokeRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName)
    {
        var assignmentsToRemove = await _context.UserRoleAssignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.RoleName == roleName)
            .ToListAsync();
        
        if (assignmentsToRemove.Any())
        {
            _context.UserRoleAssignments.RemoveRange(assignmentsToRemove);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Revoked role {RoleName} from user {UserId} for module {Module} in tenant {TenantId}", 
                roleName, userId, module, tenantId);
            return true;
        }
        
        return false;
    }
    
    public async Task<List<UserRoleAssignment>> GetUserRolesAsync(Guid userId, Guid? tenantId, ModuleType module)
    {
        return await _context.UserRoleAssignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.IsActive)
            .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
    
    public async Task<List<UserRoleAssignment>> GetUsersWithRoleAsync(Guid? tenantId, ModuleType module, string roleName)
    {
        return await _context.UserRoleAssignments
            .Where(r => r.TenantId == tenantId && r.Module == module && r.RoleName == roleName && r.IsActive)
            .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
    
    // ===== PERMISSION CHECKING =====
    
    public async Task<bool> HasModulePermissionAsync(Guid userId, Guid? tenantId, ModuleType module, ModuleAction action, Guid? resourceId = null)
    {
        var userRoles = await GetUserRolesAsync(userId, tenantId, module);
        
        foreach (var roleAssignment in userRoles)
        {
            var roleDefinition = await _context.ModuleRoles
                .FirstOrDefaultAsync(r => r.Name == roleAssignment.RoleName && r.Module == module);
            if (roleDefinition == null) continue;
            
            var hasPermission = roleDefinition.Permissions.Any(p => 
                p.Module == module && 
                p.Action == action && 
                p.IsGranted &&
                (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));
            
            if (hasPermission)
            {
                // Check constraints
                if (await CheckConstraintsAsync(roleAssignment.Constraints.ToList(), userId, tenantId, resourceId).ConfigureAwait(false))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    public async Task<List<ModulePermissionDefinition>> GetUserModulePermissionsAsync(Guid userId, Guid? tenantId, ModuleType module)
    {
        // Get all roles for this user in this module
        var permissions = new List<ModulePermissionDefinition>();
        var userRoles = await GetUserRolesAsync(userId, tenantId, module);
        
        foreach (var roleAssignment in userRoles)
        {
            var roleDefinition = await _context.ModuleRoles
                .FirstOrDefaultAsync(r => r.Name == roleAssignment.RoleName && r.Module == module);
            if (roleDefinition != null)
            {
                permissions.AddRange(roleDefinition.Permissions.Where(p => p.IsGranted));
            }
        }
        
        return permissions.DistinctBy(p => new { p.Module, p.Action }).ToList();
    }
    
    public async Task<List<Guid>> GetUsersWithPermissionAsync(Guid? tenantId, ModuleType module, ModuleAction action)
    {
        var usersWithPermission = new List<Guid>();
        
        // Get all role assignments for this module
        var roleAssignments = await _context.UserRoleAssignments
            .Where(r => r.TenantId == tenantId && r.Module == module && r.IsActive)
            .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var assignment in roleAssignments)
        {
            var roleDefinition = await _context.ModuleRoles
                .FirstOrDefaultAsync(r => r.Name == assignment.RoleName && r.Module == module);
            if (roleDefinition?.Permissions.Any(p => p.Module == module && p.Action == action && p.IsGranted) == true)
            {
                usersWithPermission.Add(assignment.UserId);
            }
        }
        
        return usersWithPermission.Distinct().ToList();
    }
    
    // ===== TESTING LAB SPECIFIC PERMISSIONS =====
    
    public async Task<bool> CanCreateTestingSessionsAsync(Guid userId, Guid? tenantId)
    {
        return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.CreateSession);
    }
    
    public async Task<bool> CanDeleteTestingSessionsAsync(Guid userId, Guid? tenantId)
    {
        return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.DeleteSession);
    }
    
    public async Task<bool> CanManageTestersAsync(Guid userId, Guid? tenantId)
    {
        return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ManageTesters);
    }
    
    public async Task<bool> CanViewTestingReportsAsync(Guid userId, Guid? tenantId)
    {
        return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ViewReports);
    }
    
    public async Task<bool> CanExportTestingDataAsync(Guid userId, Guid? tenantId)
    {
        return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ExportData);
    }
    
    public async Task<TestingLabPermissions> GetUserTestingLabPermissionsAsync(Guid userId, Guid? tenantId)
    {
        var userRoles = await GetUserRolesAsync(userId, tenantId, ModuleType.TestingLab);
        
        return new TestingLabPermissions
        {
            CanCreateSessions = await CanCreateTestingSessionsAsync(userId, tenantId),
            CanEditSessions = await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.Edit),
            CanDeleteSessions = await CanDeleteTestingSessionsAsync(userId, tenantId),
            CanManageTesters = await CanManageTestersAsync(userId, tenantId),
            CanViewReports = await CanViewTestingReportsAsync(userId, tenantId),
            CanExportData = await CanExportTestingDataAsync(userId, tenantId),
            CanAdminister = await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.Administer),
            AssignedRoles = userRoles.Select(r => r.RoleName).ToList(),
            Constraints = userRoles.SelectMany(r => r.Constraints).ToList(),
        };
    }
    
    // ===== ROLE DEFINITION MANAGEMENT =====
    
    public async Task<ModuleRole> CreateRoleDefinitionAsync(string roleName, ModuleType module, string description, List<ModulePermissionDefinition> permissions, int priority = 0)
    {
        var existingRole = await _context.ModuleRoles
            .FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);
        if (existingRole != null)
        {
            throw new InvalidOperationException($"Role {roleName} already exists for module {module}");
        }
        
        var role = new ModuleRole
        {
            Name = roleName,
            Module = module,
            Description = description,
            Permissions = permissions,
            Priority = priority,
            IsSystemRole = false,
        };
        
        _context.ModuleRoles.Add(role);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created role definition {RoleName} for module {Module}", roleName, module);
        
        return role;
    }
    
    public async Task<ModuleRole> UpdateRoleDefinitionAsync(string roleName, ModuleType module, List<ModulePermissionDefinition> permissions)
    {
        var role = await _context.ModuleRoles
            .FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);
        if (role == null)
        {
            throw new InvalidOperationException($"Role {roleName} not found for module {module}");
        }
        
        if (role.IsSystemRole)
        {
            throw new InvalidOperationException($"Cannot modify system role {roleName}");
        }
        
        role.Permissions = permissions;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated role definition {RoleName} for module {Module}", roleName, module);
        
        return role;
    }
    
    public async Task<List<ModuleRole>> GetModuleRoleDefinitionsAsync(ModuleType module)
    {
        return await _context.ModuleRoles
            .Where(r => r.Module == module)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }
    
    public async Task<bool> DeleteRoleDefinitionAsync(string roleName, ModuleType module)
    {
        var role = await _context.ModuleRoles
            .FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);
        if (role == null) return false;
        
        if (role.IsSystemRole)
        {
            throw new InvalidOperationException($"Cannot delete system role {roleName}");
        }
        
        // Check if any users are assigned to this role
        var hasAssignments = await _context.UserRoleAssignments
            .AnyAsync(r => r.RoleName == roleName && r.Module == module && r.IsActive);
        if (hasAssignments)
        {
            throw new InvalidOperationException($"Cannot delete role {roleName} because users are still assigned to it");
        }
        
        _context.ModuleRoles.Remove(role);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted role definition {RoleName} for module {Module}", roleName, module);
        
        return true;
    }
    
    // ===== PRIVATE HELPERS =====
    
    private static Task<bool> CheckConstraintsAsync(List<PermissionConstraint> constraints, Guid userId, Guid? tenantId, Guid? resourceId)
    {
        // For now, just return true - in production this would check actual constraints
        // like resource ownership, time-based restrictions, etc.
        return Task.FromResult(true);
    }
    
    /// <summary>
    /// Ensures default module roles exist in the database. Should be called during application startup.
    /// </summary>
    public async Task EnsureDefaultRolesExistAsync()
    {
        var existingRoles = await _context.ModuleRoles
            .Where(r => r.Module == ModuleType.TestingLab)
            .ToListAsync();
        
        if (existingRoles.Any()) return; // Already initialized
        
        // Testing Lab Roles
        var testingLabAdminPermissions = new List<ModulePermissionDefinition>
        {
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Create, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Read, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Edit, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Delete, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Administer, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.CreateSession, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.DeleteSession, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ManageTesters, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ViewReports, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ExportData, IsGranted = true },
        };
        
        var testingLabManagerPermissions = new List<ModulePermissionDefinition>
        {
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Create, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Read, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Edit, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.CreateSession, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ManageTesters, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ViewReports, IsGranted = true },
        };
        
        var testingLabCoordinatorPermissions = new List<ModulePermissionDefinition>
        {
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Read, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.CreateSession, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ManageTesters, IsGranted = true },
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.ViewReports, IsGranted = true },
        };
        
        var testingLabTesterPermissions = new List<ModulePermissionDefinition>
        {
            new() { Module = ModuleType.TestingLab, Action = ModuleAction.Read, IsGranted = true },
        };
        
        var defaultRoles = new[]
        {
            new ModuleRole
            {
                Name = "TestingLabAdmin",
                Module = ModuleType.TestingLab,
                Description = "Full administrative access to Testing Lab",
                Permissions = testingLabAdminPermissions,
                Priority = 100,
                IsSystemRole = true,
            },
            new ModuleRole
            {
                Name = "TestingLabManager",
                Module = ModuleType.TestingLab,
                Description = "Can create and edit sessions, manage testers, but cannot delete sessions",
                Permissions = testingLabManagerPermissions,
                Priority = 80,
                IsSystemRole = true,
            },
            new ModuleRole
            {
                Name = "TestingLabCoordinator",
                Module = ModuleType.TestingLab,
                Description = "Can create sessions and manage testers",
                Permissions = testingLabCoordinatorPermissions,
                Priority = 60,
                IsSystemRole = true,
            },
            new ModuleRole
            {
                Name = "TestingLabTester",
                Module = ModuleType.TestingLab,
                Description = "Can only participate in testing sessions",
                Permissions = testingLabTesterPermissions,
                Priority = 40,
                IsSystemRole = true,
            },
        };
        
        _context.ModuleRoles.AddRange(defaultRoles);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Initialized default module roles in database");
    }
}
