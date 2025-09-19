using System.Text.Json;
using GameGuild.Database;
using GameGuild.Source.Core.Services;
using GameGuild.Source.Core.Tenants;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Source.Core.Services;

/// <summary>
/// Service interface for managing role templates
/// </summary>
public interface IRoleTemplateService {
    // Role Template Management
    Task<RoleTemplate> CreateRoleTemplateAsync(CreateRoleTemplateRequest request);
    Task<RoleTemplate?> GetRoleTemplateByIdAsync(Guid id);
    Task<RoleTemplate?> GetRoleTemplateBySlugAsync(string slug);
    Task<IEnumerable<RoleTemplate>> GetRoleTemplatesAsync(string? category = null, bool activeOnly = true);
    Task<RoleTemplate> UpdateRoleTemplateAsync(Guid id, UpdateRoleTemplateRequest request);
    Task<bool> DeleteRoleTemplateAsync(Guid id);
    Task<bool> ActivateRoleTemplateAsync(Guid id);
    Task<bool> DeactivateRoleTemplateAsync(Guid id);

    // Tenant Role Applications
    Task<TenantRoleApplication> ApplyRoleTemplateToTenantAsync(Guid roleTemplateId, Guid tenantId, ApplyRoleTemplateRequest? request = null);
    Task<TenantRoleApplication?> GetTenantRoleApplicationAsync(Guid roleTemplateId, Guid tenantId);
    Task<IEnumerable<TenantRoleApplication>> GetTenantRoleApplicationsAsync(Guid tenantId);
    Task<TenantRoleApplication> UpdateTenantRoleApplicationAsync(Guid id, UpdateTenantRoleApplicationRequest request);
    Task<bool> RemoveRoleTemplateFromTenantAsync(Guid roleTemplateId, Guid tenantId);

    // User Role Assignments
    Task<UserTenantRole> AssignRoleToUserAsync(Guid userId, Guid tenantRoleApplicationId, AssignRoleRequest? request = null);
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid tenantRoleApplicationId);
    Task<IEnumerable<UserTenantRole>> GetUserRolesAsync(Guid userId, Guid? tenantId = null);
    Task<IEnumerable<UserTenantRole>> GetRoleAssignmentsAsync(Guid tenantRoleApplicationId);
    Task<bool> HasRoleAsync(Guid userId, Guid tenantId, string roleSlug);

    // Permission Resolution
    Task<IEnumerable<PermissionDefinition>> GetEffectivePermissionsAsync(Guid userId, Guid tenantId);
    Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string resource, string action);
}

/// <summary>
/// Implementation of role template service
/// </summary>
public class RoleTemplateService(
  ApplicationDbContext context,
  ITenantIsolationService tenantIsolationService,
  ILogger<RoleTemplateService> logger) : IRoleTemplateService {

    public async Task<RoleTemplate> CreateRoleTemplateAsync(CreateRoleTemplateRequest request) {
        // Validate slug uniqueness
        if (await context.RoleTemplates.AnyAsync(rt => rt.Slug == request.Slug)) {
            throw new InvalidOperationException($"Role template with slug '{request.Slug}' already exists");
        }

        var roleTemplate = new RoleTemplate {
            Name = request.Name,
            Slug = request.Slug,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            IsActive = request.IsActive,
            IsSystemRole = request.IsSystemRole,
            PermissionDefinitions = JsonSerializer.Serialize(request.Permissions)
        };

        context.RoleTemplates.Add(roleTemplate);
        await context.SaveChangesAsync();

        logger.LogInformation("Created role template: {RoleTemplateName} ({Slug})", roleTemplate.Name, roleTemplate.Slug);
        return roleTemplate;
    }

    public async Task<RoleTemplate?> GetRoleTemplateByIdAsync(Guid id) {
        return await context.RoleTemplates
          .Include(rt => rt.Applications)
          .FirstOrDefaultAsync(rt => rt.Id == id);
    }

    public async Task<RoleTemplate?> GetRoleTemplateBySlugAsync(string slug) {
        return await context.RoleTemplates
          .Include(rt => rt.Applications)
          .FirstOrDefaultAsync(rt => rt.Slug == slug);
    }

    public async Task<IEnumerable<RoleTemplate>> GetRoleTemplatesAsync(string? category = null, bool activeOnly = true) {
        var query = context.RoleTemplates.AsQueryable();

        if (activeOnly) {
            query = query.Where(rt => rt.IsActive);
        }

        if (!string.IsNullOrEmpty(category)) {
            query = query.Where(rt => rt.Category == category);
        }

        return await query.OrderBy(rt => rt.Category).ThenBy(rt => rt.Priority).ToListAsync();
    }

    public async Task<RoleTemplate> UpdateRoleTemplateAsync(Guid id, UpdateRoleTemplateRequest request) {
        var roleTemplate = await context.RoleTemplates.FirstOrDefaultAsync(rt => rt.Id == id);
        if (roleTemplate == null) {
            throw new InvalidOperationException($"Role template with ID {id} not found");
        }

        if (roleTemplate.IsSystemRole && request.IsSystemRole == false) {
            throw new InvalidOperationException("Cannot modify system role template");
        }

        // Update properties
        if (!string.IsNullOrEmpty(request.Name)) roleTemplate.Name = request.Name;
        if (!string.IsNullOrEmpty(request.DisplayName)) roleTemplate.DisplayName = request.DisplayName;
        if (request.Description != null) roleTemplate.Description = request.Description;
        if (!string.IsNullOrEmpty(request.Category)) roleTemplate.Category = request.Category;
        if (request.Priority.HasValue) roleTemplate.Priority = request.Priority.Value;
        if (request.IsActive.HasValue) roleTemplate.IsActive = request.IsActive.Value;
        if (request.Permissions != null) {
            roleTemplate.PermissionDefinitions = JsonSerializer.Serialize(request.Permissions);
        }

        roleTemplate.Touch();
        await context.SaveChangesAsync();

        logger.LogInformation("Updated role template: {RoleTemplateName} ({Id})", roleTemplate.Name, roleTemplate.Id);
        return roleTemplate;
    }

    public async Task<bool> DeleteRoleTemplateAsync(Guid id) {
        var roleTemplate = await context.RoleTemplates.FirstOrDefaultAsync(rt => rt.Id == id);
        if (roleTemplate == null) return false;

        if (roleTemplate.IsSystemRole) {
            throw new InvalidOperationException("Cannot delete system role template");
        }

        // Check if role template is in use
        var inUse = await context.TenantRoleApplications.AnyAsync(tra => tra.RoleTemplateId == id);
        if (inUse) {
            throw new InvalidOperationException("Cannot delete role template that is currently applied to tenants");
        }

        context.RoleTemplates.Remove(roleTemplate);
        await context.SaveChangesAsync();

        logger.LogInformation("Deleted role template: {RoleTemplateName} ({Id})", roleTemplate.Name, roleTemplate.Id);
        return true;
    }

    public async Task<bool> ActivateRoleTemplateAsync(Guid id) {
        var roleTemplate = await context.RoleTemplates.FirstOrDefaultAsync(rt => rt.Id == id);
        if (roleTemplate == null) return false;

        roleTemplate.IsActive = true;
        roleTemplate.Touch();
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateRoleTemplateAsync(Guid id) {
        var roleTemplate = await context.RoleTemplates.FirstOrDefaultAsync(rt => rt.Id == id);
        if (roleTemplate == null) return false;

        roleTemplate.IsActive = false;
        roleTemplate.Touch();
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<TenantRoleApplication> ApplyRoleTemplateToTenantAsync(Guid roleTemplateId, Guid tenantId, ApplyRoleTemplateRequest? request = null) {
        // Check if already applied
        var existing = await context.TenantRoleApplications
          .FirstOrDefaultAsync(tra => tra.RoleTemplateId == roleTemplateId && tra.Tenant!.Id == tenantId);

        if (existing != null) {
            throw new InvalidOperationException("Role template is already applied to this tenant");
        }

        var roleTemplate = await GetRoleTemplateByIdAsync(roleTemplateId);
        if (roleTemplate == null) {
            throw new InvalidOperationException($"Role template with ID {roleTemplateId} not found");
        }

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) {
            throw new InvalidOperationException($"Tenant with ID {tenantId} not found");
        }

        var application = new TenantRoleApplication {
            RoleTemplateId = roleTemplateId,
            Tenant = tenant,
            CustomName = request?.CustomName,
            CustomDescription = request?.CustomDescription,
            PermissionOverrides = request?.PermissionOverrides != null
            ? JsonSerializer.Serialize(request.PermissionOverrides)
            : null,
            IsActive = request?.IsActive ?? true
        };

        context.TenantRoleApplications.Add(application);
        await context.SaveChangesAsync();

        logger.LogInformation("Applied role template {RoleTemplate} to tenant {TenantId}", roleTemplate.Name, tenantId);
        return application;
    }

    public async Task<TenantRoleApplication?> GetTenantRoleApplicationAsync(Guid roleTemplateId, Guid tenantId) {
        return await context.TenantRoleApplications
          .Include(tra => tra.RoleTemplate)
          .Include(tra => tra.Tenant)
          .FirstOrDefaultAsync(tra => tra.RoleTemplateId == roleTemplateId && tra.Tenant!.Id == tenantId);
    }

    public async Task<IEnumerable<TenantRoleApplication>> GetTenantRoleApplicationsAsync(Guid tenantId) {
        var query = context.TenantRoleApplications
          .Include(tra => tra.RoleTemplate)
          .Where(tra => tra.Tenant!.Id == tenantId);

        return await tenantIsolationService.ApplyTenantFilter(query, tenantId).ToListAsync();
    }

    public async Task<TenantRoleApplication> UpdateTenantRoleApplicationAsync(Guid id, UpdateTenantRoleApplicationRequest request) {
        var application = await context.TenantRoleApplications.FirstOrDefaultAsync(tra => tra.Id == id);
        if (application == null) {
            throw new InvalidOperationException($"Tenant role application with ID {id} not found");
        }

        if (request.CustomName != null) application.CustomName = request.CustomName;
        if (request.CustomDescription != null) application.CustomDescription = request.CustomDescription;
        if (request.IsActive.HasValue) application.IsActive = request.IsActive.Value;
        if (request.PermissionOverrides != null) {
            application.PermissionOverrides = JsonSerializer.Serialize(request.PermissionOverrides);
        }

        application.Touch();
        await context.SaveChangesAsync();

        return application;
    }

    public async Task<bool> RemoveRoleTemplateFromTenantAsync(Guid roleTemplateId, Guid tenantId) {
        var application = await GetTenantRoleApplicationAsync(roleTemplateId, tenantId);
        if (application == null) return false;

        // Check if there are active user assignments
        var hasActiveUsers = await context.UserTenantRoles
          .AnyAsync(utr => utr.TenantRoleApplicationId == application.Id && utr.IsActive);

        if (hasActiveUsers) {
            throw new InvalidOperationException("Cannot remove role template with active user assignments");
        }

        context.TenantRoleApplications.Remove(application);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<UserTenantRole> AssignRoleToUserAsync(Guid userId, Guid tenantRoleApplicationId, AssignRoleRequest? request = null) {
        // Check if already assigned
        var existing = await context.UserTenantRoles
          .FirstOrDefaultAsync(utr => utr.UserId == userId && utr.TenantRoleApplicationId == tenantRoleApplicationId);

        if (existing != null) {
            throw new InvalidOperationException("User already has this role assigned");
        }

        var assignment = new UserTenantRole {
            UserId = userId,
            TenantRoleApplicationId = tenantRoleApplicationId,
            EffectiveFrom = request?.EffectiveFrom ?? DateTime.UtcNow,
            ExpiresAt = request?.ExpiresAt,
            AssignedByUserId = request?.AssignedByUserId,
            Notes = request?.Notes,
            IsActive = request?.IsActive ?? true
        };

        context.UserTenantRoles.Add(assignment);
        await context.SaveChangesAsync();

        logger.LogInformation("Assigned role {RoleApplicationId} to user {UserId}", tenantRoleApplicationId, userId);
        return assignment;
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid tenantRoleApplicationId) {
        var assignment = await context.UserTenantRoles
          .FirstOrDefaultAsync(utr => utr.UserId == userId && utr.TenantRoleApplicationId == tenantRoleApplicationId);

        if (assignment == null) return false;

        context.UserTenantRoles.Remove(assignment);
        await context.SaveChangesAsync();

        logger.LogInformation("Removed role {RoleApplicationId} from user {UserId}", tenantRoleApplicationId, userId);
        return true;
    }

    public async Task<IEnumerable<UserTenantRole>> GetUserRolesAsync(Guid userId, Guid? tenantId = null) {
        var query = context.UserTenantRoles
          .Include(utr => utr.TenantRoleApplication)
          .ThenInclude(tra => tra.RoleTemplate)
          .Include(utr => utr.TenantRoleApplication.Tenant)
          .Where(utr => utr.UserId == userId && utr.IsActive);

        if (tenantId.HasValue) {
            query = query.Where(utr => utr.TenantRoleApplication.Tenant!.Id == tenantId.Value);
        }

        return await tenantIsolationService.ApplyTenantFilter(query, tenantId).ToListAsync();
    }

    public async Task<IEnumerable<UserTenantRole>> GetRoleAssignmentsAsync(Guid tenantRoleApplicationId) {
        return await context.UserTenantRoles
          .Include(utr => utr.TenantRoleApplication)
          .Where(utr => utr.TenantRoleApplicationId == tenantRoleApplicationId && utr.IsActive)
          .ToListAsync();
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid tenantId, string roleSlug) {
        return await context.UserTenantRoles
          .AnyAsync(utr => utr.UserId == userId
                        && utr.IsActive
                        && utr.TenantRoleApplication.Tenant!.Id == tenantId
                        && utr.TenantRoleApplication.RoleTemplate.Slug == roleSlug);
    }

    public async Task<IEnumerable<PermissionDefinition>> GetEffectivePermissionsAsync(Guid userId, Guid tenantId) {
        var userRoles = await GetUserRolesAsync(userId, tenantId);
        var allPermissions = new List<PermissionDefinition>();

        foreach (var userRole in userRoles) {
            // Parse base permissions from role template
            var basePermissions = JsonSerializer.Deserialize<List<PermissionDefinition>>(
              userRole.TenantRoleApplication.RoleTemplate.PermissionDefinitions) ?? new List<PermissionDefinition>();

            allPermissions.AddRange(basePermissions);

            // Parse permission overrides if any
            if (!string.IsNullOrEmpty(userRole.TenantRoleApplication.PermissionOverrides)) {
                var overrides = JsonSerializer.Deserialize<List<PermissionDefinition>>(
                  userRole.TenantRoleApplication.PermissionOverrides) ?? new List<PermissionDefinition>();

                allPermissions.AddRange(overrides);
            }
        }

        // Remove duplicates and return
        return allPermissions
          .GroupBy(p => new { p.Resource, p.Action, p.Scope })
          .Select(g => g.First())
          .ToList();
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string resource, string action) {
        var permissions = await GetEffectivePermissionsAsync(userId, tenantId);
        return permissions.Any(p => p.Resource == resource && p.Action == action);
    }
}

// Request DTOs
public class CreateRoleTemplateRequest {
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsSystemRole { get; set; } = false;
    public List<PermissionDefinition> Permissions { get; set; } = new();
}

public class UpdateRoleTemplateRequest {
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsSystemRole { get; set; }
    public List<PermissionDefinition>? Permissions { get; set; }
}

public class ApplyRoleTemplateRequest {
    public string? CustomName { get; set; }
    public string? CustomDescription { get; set; }
    public List<PermissionDefinition>? PermissionOverrides { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTenantRoleApplicationRequest {
    public string? CustomName { get; set; }
    public string? CustomDescription { get; set; }
    public List<PermissionDefinition>? PermissionOverrides { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignRoleRequest {
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}