using GameGuild.Identity.Authorization;

namespace GameGuild.TestingLab;

public interface ITestingLabPermissionService {
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
