using GameGuild.Database;


namespace GameGuild.Core.Infrastructure.Permissions;

/// <summary> Infrastructure implementation of the three-layer permission service Implements Clean Architecture and Domain-Driven Design principles </summary>
public class PermissionService : IPermissionService {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<PermissionService> _logger;

  public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger) {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  #region Layer 1: Tenant-Wide Permissions

  public async Task<TenantPermission> GrantTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[ ] permissions) {
    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    // Combine all permissions using bitwise OR
    var combinedPermissions = permissions.Aggregate((current, next) => current | next);

    // Check if permission already exists
    var existingPermission = await _context.TenantPermissions.FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

    if (existingPermission != null) {
      existingPermission.UpdatePermissions(combinedPermissions);
      _logger.LogInformation("Updated tenant permissions for User:{UserId}, Tenant:{TenantId}, Permissions:{Permissions}", userId, tenantId, combinedPermissions);
    }
    else {
      existingPermission = new TenantPermission(userId, tenantId, combinedPermissions);
      _context.TenantPermissions.Add(existingPermission);
      _logger.LogInformation("Granted new tenant permissions for User:{UserId}, Tenant:{TenantId}, Permissions:{Permissions}", userId, tenantId, combinedPermissions);
    }

    await _context.SaveChangesAsync();

    return existingPermission;
  }

  public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[ ] userIds, Guid tenantId, PermissionType[ ] permissions) {
    if (userIds == null || userIds.Length == 0) throw new ArgumentException("At least one user ID must be specified", nameof(userIds));

    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    var combinedPermissions = permissions.Aggregate((current, next) => current | next);
    var result = new List<TenantPermission>();

    // Get existing permissions for all users
    var existingPermissions = await _context.TenantPermissions.Where(tp => userIds.Contains(tp.UserId!.Value) && tp.TenantId == tenantId && tp.DeletedAt == null).ToListAsync();

    foreach (var userId in userIds) {
      var existingPermission = existingPermissions.FirstOrDefault(ep => ep.UserId == userId);

      if (existingPermission != null) {
        existingPermission.UpdatePermissions(combinedPermissions);
        result.Add(existingPermission);
      }
      else {
        var newPermission = new TenantPermission(userId, tenantId, combinedPermissions);
        _context.TenantPermissions.Add(newPermission);
        result.Add(newPermission);
      }
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation("Bulk granted tenant permissions for {UserCount} users in Tenant:{TenantId}, Permissions:{Permissions}", userIds.Length, tenantId, combinedPermissions);

    return result;
  }

  public async Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission) {
    // For null user, only check default permissions
    if (!userId.HasValue) return await CheckDefaultTenantPermissionAsync(tenantId, permission);

    // 1. Check user-specific permissions first
    var userPermission = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

    if (userPermission?.HasPermission(permission) == true) return true;

    // 2. Check tenant default permissions
    if (tenantId.HasValue) {
      var tenantDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId && tp.DeletedAt == null);

      if (tenantDefault?.HasPermission(permission) == true) return true;
    }

    // 3. Check global default permissions
    var globalDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null && tp.DeletedAt == null);

    return globalDefault?.HasPermission(permission) == true;
  }

  public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId) {
    var permissions = new HashSet<PermissionType>();

    // Get user-specific permissions
    if (userId.HasValue) {
      var userPermission = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null);

      if (userPermission != null) AddPermissionFlags(permissions, userPermission.Permissions);
    }

    // Get tenant default permissions
    if (tenantId.HasValue) {
      var tenantDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId && tp.DeletedAt == null);

      if (tenantDefault != null) AddPermissionFlags(permissions, tenantDefault.Permissions);
    }

    // Get global default permissions
    var globalDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null && tp.DeletedAt == null);

    if (globalDefault != null) AddPermissionFlags(permissions, globalDefault.Permissions);

    return permissions;
  }

  public async Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync() {
    var globalDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null && tp.DeletedAt == null);

    if (globalDefault == null) return Enumerable.Empty<PermissionType>();

    var permissions = new HashSet<PermissionType>();
    AddPermissionFlags(permissions, globalDefault.Permissions);

    return permissions;
  }

  public async Task SetGlobalDefaultPermissionsAsync(PermissionType[] permissions) {
    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    var combinedPermissions = permissions.Aggregate((current, next) => current | next);

    var existingPermission = await _context.TenantPermissions.FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null && tp.DeletedAt == null);

    if (existingPermission != null) {
      existingPermission.UpdatePermissions(combinedPermissions);
      _logger.LogInformation("Updated global default permissions: {Permissions}", combinedPermissions);
    }
    else {
      var newPermission = new TenantPermission(null, null, combinedPermissions);
      _context.TenantPermissions.Add(newPermission);
      _logger.LogInformation("Created global default permissions: {Permissions}", combinedPermissions);
    }

    await _context.SaveChangesAsync();
  }

  public async Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid tenantId) {
    var tenantDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId && tp.DeletedAt == null);

    if (tenantDefault == null) return Enumerable.Empty<PermissionType>();

    var permissions = new HashSet<PermissionType>();
    AddPermissionFlags(permissions, tenantDefault.Permissions);

    return permissions;
  }

  #endregion

  #region Layer 2: Content-Type Permissions

  public async Task<ContentTypePermission> GrantContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions) {
    if (string.IsNullOrWhiteSpace(contentTypeName)) throw new ArgumentException("Content type name cannot be empty", nameof(contentTypeName));

    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    var combinedPermissions = permissions.Aggregate((current, next) => current | next);

    var existingPermission = await _context.ContentTypePermissions.FirstOrDefaultAsync(ctp => ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentTypeName == contentTypeName && ctp.DeletedAt == null);

    if (existingPermission != null) { existingPermission.UpdatePermissions(combinedPermissions); }
    else {
      existingPermission = ContentTypePermission.Create(userId, tenantId, contentTypeName, combinedPermissions);
      _context.ContentTypePermissions.Add(existingPermission);
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation("Granted content type permissions for User:{UserId}, Tenant:{TenantId}, ContentType:{ContentType}, Permissions:{Permissions}", userId, tenantId, contentTypeName, combinedPermissions);

    return existingPermission;
  }

  public async Task<bool> HasContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType permission) {
    var contentTypePermission = await _context.ContentTypePermissions.AsNoTracking().FirstOrDefaultAsync(ctp => ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentTypeName == contentTypeName && ctp.DeletedAt == null);

    return contentTypePermission?.HasPermission(permission) == true;
  }

  public async Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName) {
    var permissions = new HashSet<PermissionType>();

    var contentTypePermission = await _context.ContentTypePermissions.AsNoTracking().FirstOrDefaultAsync(ctp => ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentTypeName == contentTypeName && ctp.DeletedAt == null);

    if (contentTypePermission != null) AddPermissionFlags(permissions, contentTypePermission.Permissions);

    return permissions;
  }

  #endregion

  #region Layer 3: Resource-Specific Permissions

  public async Task<TPermission> GrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
    where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    var combinedPermissions = permissions.Aggregate((current, next) => current | next);

    var existingPermission = await _context.Set<TPermission>().FirstOrDefaultAsync(rp => rp.UserId == userId && rp.TenantId == tenantId && rp.ResourceId == resourceId && rp.DeletedAt == null);

    if (existingPermission != null) { existingPermission.UpdatePermissions(combinedPermissions); }
    else {
      existingPermission = (TPermission) ResourcePermission<TResource>.Create(userId, tenantId, resourceId, combinedPermissions);
      _context.Set<TPermission>().Add(existingPermission);
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation(
      "Granted resource permissions for User:{UserId}, Tenant:{TenantId}, Resource:{ResourceId}, Type:{ResourceType}, Permissions:{Permissions}",
      userId,
      tenantId,
      resourceId,
      typeof(TResource).Name,
      combinedPermissions
    );

    return existingPermission;
  }

  public async Task<bool> HasResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission) where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    var resourcePermission = await _context.Set<TPermission>().AsNoTracking().FirstOrDefaultAsync(rp => rp.UserId == userId && rp.TenantId == tenantId && rp.ResourceId == resourceId && rp.DeletedAt == null);

    return resourcePermission?.HasPermission(permission) == true;
  }

  public async Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId) where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    var permissions = new HashSet<PermissionType>();

    var resourcePermission = await _context.Set<TPermission>().AsNoTracking().FirstOrDefaultAsync(rp => rp.UserId == userId && rp.TenantId == tenantId && rp.ResourceId == resourceId && rp.DeletedAt == null);

    if (resourcePermission != null) AddPermissionFlags(permissions, resourcePermission.Permissions);

    return permissions;
  }

  public async Task BulkGrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[ ] resourceIds, PermissionType[ ] permissions)
    where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    if (resourceIds == null || resourceIds.Length == 0) throw new ArgumentException("At least one resource ID must be specified", nameof(resourceIds));

    if (permissions == null || permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    var combinedPermissions = permissions.Aggregate((current, next) => current | next);

    var existingPermissions = await _context.Set<TPermission>().Where(rp => resourceIds.Contains(rp.ResourceId) && rp.UserId == userId && rp.TenantId == tenantId && rp.DeletedAt == null).ToListAsync();

    foreach (var resourceId in resourceIds) {
      var existingPermission = existingPermissions.FirstOrDefault(ep => ep.ResourceId == resourceId);

      if (existingPermission != null) { existingPermission.UpdatePermissions(combinedPermissions); }
      else {
        var newPermission = (TPermission) ResourcePermission<TResource>.Create(userId, tenantId, resourceId, combinedPermissions);
        _context.Set<TPermission>().Add(newPermission);
      }
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation(
      "Bulk granted resource permissions for User:{UserId}, Tenant:{TenantId}, Resources:{ResourceCount}, Type:{ResourceType}, Permissions:{Permissions}",
      userId,
      tenantId,
      resourceIds.Length,
      typeof(TResource).Name,
      combinedPermissions
    );
  }

  public async Task ShareResourceAsync<TPermission, TResource>(Guid resourceId, Guid targetUserId, Guid? tenantId, PermissionType[ ] permissions, DateTime? expiresAt = null)
    where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    var resourcePermission = await GrantResourcePermissionAsync<TPermission, TResource>(targetUserId, tenantId, resourceId, permissions);

    if (expiresAt.HasValue) {
      resourcePermission.SetExpiration(expiresAt.Value);
      await _context.SaveChangesAsync();
    }

    _logger.LogInformation("Shared resource {ResourceId} with User:{TargetUserId}, Permissions:{Permissions}, ExpiresAt:{ExpiresAt}", resourceId, targetUserId, permissions, expiresAt);
  }

  public async Task RevokeResourceAccessAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId) where TPermission : ResourcePermission<TResource>, new() where TResource : EntityBase {
    var resourcePermission = await _context.Set<TPermission>().FirstOrDefaultAsync(rp => rp.UserId == userId && rp.TenantId == tenantId && rp.ResourceId == resourceId && rp.DeletedAt == null);

    if (resourcePermission != null) {
      resourcePermission.SoftDelete();
      await _context.SaveChangesAsync();

      _logger.LogInformation("Revoked resource access for User:{UserId}, Tenant:{TenantId}, Resource:{ResourceId}", userId, tenantId, resourceId);
    }
  }

  #endregion

  #region Private Helper Methods

  private async Task<bool> CheckDefaultTenantPermissionAsync(Guid? tenantId, PermissionType permission) {
    // Check tenant default first if tenantId is provided
    if (tenantId.HasValue) {
      var tenantDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == tenantId && tp.DeletedAt == null);

      if (tenantDefault?.HasPermission(permission) == true) return true;
    }

    // Check global default
    var globalDefault = await _context.TenantPermissions.AsNoTracking().FirstOrDefaultAsync(tp => tp.UserId == null && tp.TenantId == null && tp.DeletedAt == null);

    return globalDefault?.HasPermission(permission) == true;
  }

  private static void AddPermissionFlags(HashSet<PermissionType> permissions, PermissionType permissionFlags) {
    foreach (var permission in Enum.GetValues<PermissionType>()) {
      if ((permissionFlags & permission) == permission) permissions.Add(permission);
    }
  }

  #endregion
}
