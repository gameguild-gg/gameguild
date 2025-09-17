using GameGuild.Core.Domain.Permissions;
using GameGuild.Database;
using GameGuild.Modules.Permissions;


namespace GameGuild.Core.Infrastructure.Permissions;

/// <summary> Implementation of module-based permission service using Entity Framework Following Clean Architecture and DDD principles </summary>
public class ModulePermissionService : IModulePermissionService {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<ModulePermissionService> _logger;

  public ModulePermissionService(ApplicationDbContext context, ILogger<ModulePermissionService> logger) {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  #region Role Management

  public async Task<UserRoleAssignment> AssignRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName, List<PermissionConstraint>? constraints = null, DateTime? expiresAt = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    try {
      // Remove existing assignment for this user/tenant/module/role combination
      var existingAssignments = await _context.UserRoleAssignments.Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.RoleName == roleName).ToListAsync();

      if (existingAssignments.Count != 0) { _context.UserRoleAssignments.RemoveRange(existingAssignments); }

      var assignment = new UserRoleAssignment { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, Module = module, RoleName = roleName, CreatedAt = DateTime.UtcNow, ExpiresAt = expiresAt, IsActive = true };

      // Set constraints using the method
      assignment.SetConstraints(constraints ?? []);

      _context.UserRoleAssignments.Add(assignment);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Assigned role {RoleName} to user {UserId} for module {Module} in tenant {TenantId}", roleName, userId, module, tenantId);

      return assignment;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId} for module {Module} in tenant {TenantId}", roleName, userId, module, tenantId);

      throw;
    }
  }

  public async Task<bool> RevokeRoleAsync(Guid userId, Guid? tenantId, ModuleType module, string roleName) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    try {
      var assignmentsToRemove = await _context.UserRoleAssignments.Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.RoleName == roleName).ToListAsync();

      if (assignmentsToRemove.Count != 0) {
        _context.UserRoleAssignments.RemoveRange(assignmentsToRemove);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked role {RoleName} from user {UserId} for module {Module} in tenant {TenantId}", roleName, userId, module, tenantId);

        return true;
      }

      return false;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error revoking role {RoleName} from user {UserId} for module {Module} in tenant {TenantId}", roleName, userId, module, tenantId);

      throw;
    }
  }

  public async Task<List<UserRoleAssignment>> GetUserRolesAsync(Guid userId, Guid? tenantId, ModuleType module) {
    try { return await _context.UserRoleAssignments.Where(r => r.UserId == userId && r.TenantId == tenantId && r.Module == module && r.IsActive).Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow).ToListAsync(); }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting roles for user {UserId} in module {Module} for tenant {TenantId}", userId, module, tenantId);

      return [];
    }
  }

  public async Task<List<UserRoleAssignment>> GetUsersWithRoleAsync(Guid? tenantId, ModuleType module, string roleName) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    try { return await _context.UserRoleAssignments.Where(r => r.TenantId == tenantId && r.Module == module && r.RoleName == roleName && r.IsActive).Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow).ToListAsync(); }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting users with role {RoleName} in module {Module} for tenant {TenantId}", roleName, module, tenantId);

      return [];
    }
  }

  #endregion

  #region Permission Checking

  public async Task<bool> HasModulePermissionAsync(Guid userId, Guid? tenantId, ModuleType module, ModuleAction action, Guid? resourceId = null) {
    try {
      var userRoles = await GetUserRolesAsync(userId, tenantId, module);

      foreach (var roleAssignment in userRoles) {
        var roleDefinition = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleAssignment.RoleName && r.Module == module);

        if (roleDefinition == null) continue;

        var hasPermission = roleDefinition.Permissions.Any(p => p.Module == module && p.Action == action && p.IsGranted && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));

        if (hasPermission) {
          // Check constraints
          if (await CheckConstraintsAsync(roleAssignment.Constraints.ToList(), userId, tenantId, resourceId)) { return true; }
        }
      }

      return false;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking module permission {Action} for user {UserId} in module {Module} for tenant {TenantId}", action, userId, module, tenantId);

      return false;
    }
  }

  public async Task<List<ModulePermissionDefinition>> GetUserModulePermissionsAsync(Guid userId, Guid? tenantId, ModuleType module) {
    try {
      var permissions = new List<ModulePermissionDefinition>();
      var userRoles = await GetUserRolesAsync(userId, tenantId, module);

      foreach (var roleAssignment in userRoles) {
        var roleDefinition = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleAssignment.RoleName && r.Module == module);

        if (roleDefinition == null) continue;

        var rolePermissions = roleDefinition.Permissions.Where(p => p.IsGranted && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow)).ToList();

        permissions.AddRange(rolePermissions);
      }

      // Remove duplicates and return
      return permissions.GroupBy(p => new { p.Module, p.Action }).Select(g => g.First()).ToList();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting module permissions for user {UserId} in module {Module} for tenant {TenantId}", userId, module, tenantId);

      return [];
    }
  }

  public async Task<List<Guid>> GetUsersWithPermissionAsync(Guid? tenantId, ModuleType module, ModuleAction action) {
    try {
      var users = new List<Guid>();
      var roleAssignments = await _context.UserRoleAssignments.Where(r => r.TenantId == tenantId && r.Module == module && r.IsActive).Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow).ToListAsync();

      foreach (var assignment in roleAssignments) {
        var roleDefinition = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == assignment.RoleName && r.Module == module);

        if (roleDefinition == null) continue;

        var hasPermission = roleDefinition.Permissions.Any(p => p.Module == module && p.Action == action && p.IsGranted && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));

        if (hasPermission) { users.Add(assignment.UserId); }
      }

      return users.Distinct().ToList();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting users with permission {Action} in module {Module} for tenant {TenantId}", action, module, tenantId);

      return [];
    }
  }

  #endregion

  #region Testing Lab Specific Permissions

  public async Task<bool> CanCreateTestingSessionsAsync(Guid userId, Guid? tenantId) { return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.CreateSession); }

  public async Task<bool> CanDeleteTestingSessionsAsync(Guid userId, Guid? tenantId) { return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.DeleteSession); }

  public async Task<bool> CanManageTestersAsync(Guid userId, Guid? tenantId) { return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ManageTesters); }

  public async Task<bool> CanViewTestingReportsAsync(Guid userId, Guid? tenantId) { return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ViewReports); }

  public async Task<bool> CanExportTestingDataAsync(Guid userId, Guid? tenantId) { return await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.ExportData); }

  public async Task<TestingLabPermissions> GetUserTestingLabPermissionsAsync(Guid userId, Guid? tenantId) {
    try {
      var userRoles = await GetUserRolesAsync(userId, tenantId, ModuleType.TestingLab);
      var constraints = userRoles.SelectMany(r => r.Constraints).ToList();

      return new TestingLabPermissions {
        CanCreateSessions = await CanCreateTestingSessionsAsync(userId, tenantId),
        CanEditSessions = await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.EditSession),
        CanDeleteSessions = await CanDeleteTestingSessionsAsync(userId, tenantId),
        CanManageTesters = await CanManageTestersAsync(userId, tenantId),
        CanViewReports = await CanViewTestingReportsAsync(userId, tenantId),
        CanExportData = await CanExportTestingDataAsync(userId, tenantId),
        CanAdminister = await HasModulePermissionAsync(userId, tenantId, ModuleType.TestingLab, ModuleAction.Administer),
        AssignedRoles = userRoles.Select(r => r.RoleName).ToList(),
        Constraints = constraints,
      };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting Testing Lab permissions for user {UserId} in tenant {TenantId}", userId, tenantId);

      return new TestingLabPermissions();
    }
  }

  #endregion

  #region Role Definition Management

  public async Task<ModuleRole> CreateRoleDefinitionAsync(string roleName, ModuleType module, string description, List<ModulePermissionDefinition> permissions, int priority = 0) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);

    try {
      var existingRole = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);

      if (existingRole != null) { throw new InvalidOperationException($"Role {roleName} already exists for module {module}"); }

      var role = new ModuleRole { Id = Guid.NewGuid(), Name = roleName, Module = module, Description = description, Priority = priority, CreatedAt = DateTime.UtcNow, Permissions = permissions };

      _context.ModuleRoles.Add(role);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Created role definition {RoleName} for module {Module}", roleName, module);

      return role;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error creating role definition {RoleName} for module {Module}", roleName, module);

      throw;
    }
  }

  public async Task<ModuleRole> UpdateRoleDefinitionAsync(string roleName, ModuleType module, List<ModulePermissionDefinition> permissions) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    try {
      var role = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);

      if (role == null) { throw new InvalidOperationException($"Role {roleName} not found for module {module}"); }

      role.Permissions = permissions;
      role.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      _logger.LogInformation("Updated role definition {RoleName} for module {Module}", roleName, module);

      return role;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating role definition {RoleName} for module {Module}", roleName, module);

      throw;
    }
  }

  public async Task<List<ModuleRole>> GetModuleRoleDefinitionsAsync(ModuleType module) {
    try { return await _context.ModuleRoles.Where(r => r.Module == module).OrderBy(r => r.Priority).ThenBy(r => r.Name).ToListAsync(); }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting role definitions for module {Module}", module);

      return [];
    }
  }

  public async Task<bool> DeleteRoleDefinitionAsync(string roleName, ModuleType module) {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    try {
      // Check if any users are assigned to this role
      var hasAssignments = await _context.UserRoleAssignments.AnyAsync(r => r.RoleName == roleName && r.Module == module && r.IsActive);

      if (hasAssignments) {
        _logger.LogWarning("Cannot delete role {RoleName} for module {Module} because it has active assignments", roleName, module);

        return false;
      }

      var role = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleName && r.Module == module);

      if (role == null) { return false; }

      _context.ModuleRoles.Remove(role);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Deleted role definition {RoleName} for module {Module}", roleName, module);

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error deleting role definition {RoleName} for module {Module}", roleName, module);

      throw;
    }
  }

  public async Task EnsureDefaultRolesExistAsync() {
    try {
      // This would contain logic to ensure default roles exist
      // Implementation would depend on specific business requirements
      _logger.LogInformation("Ensuring default module roles exist");

      // Example: Create default Testing Lab roles if they don't exist
      await EnsureTestingLabDefaultRoles();

      await _context.SaveChangesAsync();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error ensuring default roles exist");

      throw;
    }
  }

  #endregion

  #region Private Helper Methods

  private async Task<bool> CheckConstraintsAsync(List<PermissionConstraint> constraints, Guid userId, Guid? tenantId, Guid? resourceId) {
    try {
      // Implement constraint checking logic
      // This would depend on specific constraint types defined in the domain
      foreach (var constraint in constraints) {
        if (!await EvaluateConstraint(constraint, userId, tenantId, resourceId)) { return false; }
      }

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking constraints for user {UserId} in tenant {TenantId}", userId, tenantId);

      return false;
    }
  }

  private async Task<bool> EvaluateConstraint(PermissionConstraint constraint, Guid userId, Guid? tenantId, Guid? resourceId) {
    // Implementation would depend on constraint types
    // Example constraint types: TimeWindow, ResourceScope, UserGroup, etc.
    return await Task.FromResult(true); // Placeholder
  }

  private async Task EnsureTestingLabDefaultRoles() {
    var testingLabRoles = new[ ] {
      new { Name = "Tester", Description = "Can participate in testing sessions", Permissions = new[ ] { ModuleAction.ParticipateInSession } },
      new { Name = "TestLead", Description = "Can manage testing sessions", Permissions = new[ ] { ModuleAction.CreateSession, ModuleAction.EditSession, ModuleAction.ManageTesters } },
      new { Name = "TestAdmin", Description = "Full testing lab administration", Permissions = Enum.GetValues<ModuleAction>() },
    };

    foreach (var roleInfo in testingLabRoles) {
      var existingRole = await _context.ModuleRoles.FirstOrDefaultAsync(r => r.Name == roleInfo.Name && r.Module == ModuleType.TestingLab);

      if (existingRole == null) {
        var permissions = roleInfo.Permissions.Select(action => new ModulePermissionDefinition { Module = ModuleType.TestingLab, Action = action, IsGranted = true, CreatedAt = DateTime.UtcNow }).ToList();

        var role = new ModuleRole {
          Id = Guid.NewGuid(),
          Name = roleInfo.Name,
          Module = ModuleType.TestingLab,
          Description = roleInfo.Description,
          Priority = roleInfo.Name == "TestAdmin" ? 1 : roleInfo.Name == "TestLead" ? 2 : 3,
          CreatedAt = DateTime.UtcNow,
          Permissions = permissions,
        };

        _context.ModuleRoles.Add(role);
      }
    }
  }

  #endregion
}
