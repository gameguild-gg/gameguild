using GameGuild.Identity.Authorization;

namespace GameGuild.TestingLab;

public interface ITestingLabPermissionService {
  Task<IReadOnlyList<RoleTemplate>> GetRoleTemplatesAsync();
  Task<RoleTemplate> CreateRoleTemplateAsync(string name, string description, IReadOnlyCollection<PermissionTemplate> permissionTemplates);
  Task<RoleTemplate?> UpdateRoleTemplateAsync(string idOrName, string? name, string description, IReadOnlyCollection<PermissionTemplate> permissionTemplates);
  Task<bool> DeleteRoleTemplateAsync(string idOrName);
  Task<IReadOnlyList<TestingLabAssignedRole>> GetUserRolesAsync(Guid userId, Guid? tenantId);
  Task<IReadOnlyList<TestingLabUserPermission>> GetUserPermissionsAsync(Guid userId, Guid? tenantId);
  Task AssignRoleToUserAsync(Guid userId, Guid? tenantId, string roleName, DateTime? expiresAt = null);
  Task RevokeRoleFromUserAsync(Guid userId, Guid? tenantId, string roleName);
  Task GrantPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid resourceId, string? reason = null, DateTime? expiresAt = null);
  Task RevokePermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid resourceId);
  Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null);
}

public sealed class TestingLabAssignedRole {
  public string RoleName { get; set; } = string.Empty;
}

public sealed class TestingLabUserPermission {
  public string Action { get; set; } = string.Empty;
  public string ResourceType { get; set; } = string.Empty;
  public Guid? ResourceId { get; set; }
}

public sealed class TestingLabPermissionService(IApplicationDbContext context) : ITestingLabPermissionService {
  private const string TemplateCategory = "TestingLab";

  public async Task<IReadOnlyList<RoleTemplate>> GetRoleTemplatesAsync() {
    var templates = await context.Set<GameGuild.Identity.Authorization.PermissionTemplate>()
      .Where(template => template.IsActive && (template.Category == TemplateCategory || template.Name.StartsWith("TestingLab")))
      .OrderBy(template => template.Name)
      .ToListAsync()
      .ConfigureAwait(false);

    return templates.Select(MapTemplate).ToList();
  }

  public async Task<RoleTemplate> CreateRoleTemplateAsync(string name, string description, IReadOnlyCollection<PermissionTemplate> permissionTemplates) {
    ValidateTemplateName(name);
    var normalizedName = name.Trim();
    var exists = await context.Set<GameGuild.Identity.Authorization.PermissionTemplate>()
      .AnyAsync(template => template.Name == normalizedName)
      .ConfigureAwait(false);
    if (exists) throw new InvalidOperationException($"Role template '{normalizedName}' already exists.");

    var template = new GameGuild.Identity.Authorization.PermissionTemplate {
      Name = normalizedName,
      Description = description.Trim(),
      Category = TemplateCategory,
      Permissions = NormalizePermissionTemplates(permissionTemplates),
      IsSystemTemplate = false,
      IsActive = true,
    };

    context.Set<GameGuild.Identity.Authorization.PermissionTemplate>().Add(template);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return MapTemplate(template);
  }

  public async Task<RoleTemplate?> UpdateRoleTemplateAsync(string idOrName, string? name, string description, IReadOnlyCollection<PermissionTemplate> permissionTemplates) {
    var template = await FindRoleTemplateAsync(idOrName).ConfigureAwait(false);
    if (template == null) return null;
    if (template.IsSystemTemplate) throw new InvalidOperationException($"System role template '{template.Name}' cannot be modified.");

    if (!string.IsNullOrWhiteSpace(name) && !string.Equals(template.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) {
      var normalizedName = name.Trim();
      var duplicate = await context.Set<GameGuild.Identity.Authorization.PermissionTemplate>()
        .AnyAsync(candidate => candidate.Id != template.Id && candidate.Name == normalizedName)
        .ConfigureAwait(false);
      if (duplicate) throw new InvalidOperationException($"Role template '{normalizedName}' already exists.");
      template.Name = normalizedName;
    }

    template.Description = description.Trim();
    template.Category = TemplateCategory;
    template.Permissions = NormalizePermissionTemplates(permissionTemplates);

    await context.SaveChangesAsync().ConfigureAwait(false);

    return MapTemplate(template);
  }

  public async Task<bool> DeleteRoleTemplateAsync(string idOrName) {
    var template = await FindRoleTemplateAsync(idOrName).ConfigureAwait(false);
    if (template == null) return false;
    if (template.IsSystemTemplate) throw new InvalidOperationException($"System role template '{template.Name}' cannot be deleted.");

    context.Set<GameGuild.Identity.Authorization.PermissionTemplate>().Remove(template);
    await context.SaveChangesAsync().ConfigureAwait(false);

    return true;
  }

  public async Task<IReadOnlyList<TestingLabAssignedRole>> GetUserRolesAsync(Guid userId, Guid? tenantId) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);

    return permissions.Permissions
      .Where(permission => permission.StartsWith("role:", StringComparison.OrdinalIgnoreCase))
      .Select(permission => new TestingLabAssignedRole { RoleName = permission["role:".Length..] })
      .ToList();
  }

  public async Task<IReadOnlyList<TestingLabUserPermission>> GetUserPermissionsAsync(Guid userId, Guid? tenantId) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);

    return permissions.Permissions
      .Select(ParsePermission)
      .Where(permission => permission != null)
      .Select(permission => permission!)
      .ToList();
  }

  public async Task AssignRoleToUserAsync(Guid userId, Guid? tenantId, string roleName, DateTime? expiresAt = null) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);
    permissions.ExpiresAt = expiresAt;
    permissions.AddPermissions($"role:{roleName}");
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task RevokeRoleFromUserAsync(Guid userId, Guid? tenantId, string roleName) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);
    permissions.Permissions = permissions.Permissions
      .Where(permission => !string.Equals(permission, $"role:{roleName}", StringComparison.OrdinalIgnoreCase))
      .ToArray();
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task GrantPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid resourceId, string? reason = null, DateTime? expiresAt = null) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);
    permissions.ExpiresAt = expiresAt;
    permissions.Reason = reason;
    permissions.AddPermissions(FormatPermission(action, resourceType, resourceId));
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task RevokePermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid resourceId) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);
    var target = FormatPermission(action, resourceType, resourceId);
    permissions.Permissions = permissions.Permissions
      .Where(permission => !string.Equals(permission, target, StringComparison.OrdinalIgnoreCase))
      .ToArray();
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string action, string resourceType, Guid? resourceId = null) {
    var permissions = await GetTenantPermissions(userId, tenantId).ConfigureAwait(false);
    var exact = resourceId.HasValue ? FormatPermission(action, resourceType, resourceId.Value) : null;
    var scoped = $"{resourceType}:{action}";

    return permissions.Permissions.Any(permission =>
      string.Equals(permission, scoped, StringComparison.OrdinalIgnoreCase) ||
      (exact != null && string.Equals(permission, exact, StringComparison.OrdinalIgnoreCase)));
  }

  private async Task<TenantPermission> GetTenantPermissions(Guid userId, Guid? tenantId) {
    var permission = await context.Set<TenantPermission>()
      .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null)
      .ConfigureAwait(false);

    if (permission != null) return permission;

    permission = new TenantPermission {
      UserId = userId,
      TenantId = tenantId,
      Permissions = [],
    };
    context.Set<TenantPermission>().Add(permission);

    return permission;
  }

  private static string FormatPermission(string action, string resourceType, Guid resourceId) => $"{resourceType}:{action}:{resourceId}";

  private async Task<GameGuild.Identity.Authorization.PermissionTemplate?> FindRoleTemplateAsync(string idOrName) {
    if (Guid.TryParse(idOrName, out var id)) {
      return await context.Set<GameGuild.Identity.Authorization.PermissionTemplate>()
        .FirstOrDefaultAsync(template => template.Id == id && template.IsActive && (template.Category == TemplateCategory || template.Name.StartsWith("TestingLab")))
        .ConfigureAwait(false);
    }

    return await context.Set<GameGuild.Identity.Authorization.PermissionTemplate>()
      .FirstOrDefaultAsync(template => template.Name == idOrName && template.IsActive && (template.Category == TemplateCategory || template.Name.StartsWith("TestingLab")))
      .ConfigureAwait(false);
  }

  private static RoleTemplate MapTemplate(GameGuild.Identity.Authorization.PermissionTemplate template) {
    return new RoleTemplate {
      Id = template.Id,
      Name = template.Name,
      Description = template.Description,
      IsSystemRole = template.IsSystemTemplate,
      PermissionTemplates = template.Permissions.Select(ParseTemplatePermission).ToList(),
    };
  }

  private static PermissionTemplate ParseTemplatePermission(string permission) {
    var parts = permission.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return new PermissionTemplate {
      ResourceType = parts.Length > 0 ? parts[0] : string.Empty,
      Action = parts.Length > 1 ? parts[1] : string.Empty,
    };
  }

  private static string[] NormalizePermissionTemplates(IReadOnlyCollection<PermissionTemplate> templates) {
    return templates
      .Where(template => TestingLabResourceTypes.IsValid(template.ResourceType) && TestingLabActions.All.Contains(template.Action, StringComparer.OrdinalIgnoreCase))
      .Select(template => $"{template.ResourceType}:{template.Action}")
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private static void ValidateTemplateName(string name) {
    if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Role template name is required.");
    if (name.Trim().Length > 100) throw new InvalidOperationException("Role template name cannot exceed 100 characters.");
  }

  private static TestingLabUserPermission? ParsePermission(string permission) {
    if (permission.StartsWith("role:", StringComparison.OrdinalIgnoreCase)) return null;

    var parts = permission.Split(':', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2) return null;

    return new TestingLabUserPermission {
      ResourceType = parts[0],
      Action = parts[1],
      ResourceId = parts.Length > 2 && Guid.TryParse(parts[2], out var resourceId) ? resourceId : null,
    };
  }
}
