using GameGuild.Database;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common;

/// <summary>
/// Implementation of the three-layer permission service
/// Layer 1: Tenant-wide permissions with default support
/// </summary>
public class PermissionService(ApplicationDbContext context) : IPermissionService {
  // ===== LAYER 1: TENANT-WIDE PERMISSIONS =====

  public async Task<TenantPermission> GrantTenantPermissionAsync(
    Guid? userId, Guid? tenantId,
    PermissionType[] permissions
  ) {
    if (permissions == null) throw new ArgumentNullException(nameof(permissions));

    if (permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    if (permissions.Length > TenantPermissionConstants.MaxPermissionsPerGrant)
      throw new ArgumentException(
        $"Cannot grant more than {TenantPermissionConstants.MaxPermissionsPerGrant} permissions at once",
        nameof(permissions)
      );

    // Find existing permission record or create new one
    var existingPermission =
      await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                            tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
      );

    TenantPermission tenantPermission;

    if (existingPermission != null) {
      // Update existing permission
      tenantPermission = existingPermission;

      foreach (var permission in permissions!) tenantPermission.AddPermission(permission);

      tenantPermission.Touch();
    }
    else {
      // Create new permission record
      tenantPermission = new TenantPermission {
        UserId = userId, TenantId = tenantId,
        // IsActive replaced by IsValid property,
        // JoinedAt replaced by CreatedAt (inherited)
        // Status removed - using IsValid property instead
      };

      foreach (var permission in permissions!) tenantPermission.AddPermission(permission);

      context.TenantPermissions.Add(tenantPermission);
    }

    await context.SaveChangesAsync();

    return tenantPermission;
  }

  public async Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission) {
    // For null user, only check default permissions
    if (!userId.HasValue) return await CheckDefaultPermissionAsync(tenantId, permission); // 1. Check user-specific permissions first

    var userPermission = await context.TenantPermissions.AsNoTracking()
                                      .FirstOrDefaultAsync(tp =>
                                                             tp.UserId == userId &&
                                                             tp.TenantId == tenantId &&
                                                             tp.DeletedAt == null &&
                                                             (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                                      );

    if (userPermission?.IsValid == true && userPermission.HasPermission(permission)) return true;

    // 2. Check tenant default permissions
    if (tenantId.HasValue) {
      var tenantDefault = await context.TenantPermissions.AsNoTracking()
                                       .FirstOrDefaultAsync(tp =>
                                                              tp.UserId == null &&
                                                              tp.TenantId == tenantId &&
                                                              tp.DeletedAt == null &&
                                                              (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                                       );

      if (tenantDefault?.IsValid == true && tenantDefault.HasPermission(permission)) return true;
    }

    // 3. Check global default permissions
    var globalDefault = await context.TenantPermissions.AsNoTracking()
                                     .FirstOrDefaultAsync(tp =>
                                                            tp.UserId == null &&
                                                            tp.TenantId == null &&
                                                            tp.DeletedAt == null &&
                                                            (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                                     );

    return globalDefault?.IsValid == true && globalDefault.HasPermission(permission);
  }

  public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId) {
    var permission = await context.TenantPermissions.AsNoTracking()
                                  .FirstOrDefaultAsync(tp =>
                                                         tp.UserId == userId &&
                                                         tp.TenantId == tenantId &&
                                                         tp.DeletedAt == null &&
                                                         (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                                  );

    if (permission?.IsValid != true) return [];

    return GetPermissionsFromEntity(permission);
  }

  public async Task RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions) {
    var tenantPermission =
      await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                            tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
      );

    if (tenantPermission == null) return;

    foreach (var permission in permissions) tenantPermission.RemovePermission(permission);

    tenantPermission.Touch();
    await context.SaveChangesAsync();
  }

  // === DEFAULT PERMISSION MANAGEMENT ===

  public async Task<TenantPermission> SetTenantDefaultPermissionsAsync(Guid? tenantId, PermissionType[] permissions) { return await GrantTenantPermissionAsync(null, tenantId, permissions); }

  public async Task<TenantPermission> SetGlobalDefaultPermissionsAsync(PermissionType[] permissions) { return await GrantTenantPermissionAsync(null, null, permissions); }

  public async Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid? tenantId) { return await GetTenantPermissionsAsync(null, tenantId); }

  public async Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync() { return await GetTenantPermissionsAsync(null, null); }

  public async Task<IEnumerable<PermissionType>> GetEffectiveTenantPermissionsAsync(Guid userId, Guid? tenantId) {
    var effectivePermissions = new HashSet<PermissionType>();

    // 1. Start with global defaults
    var globalDefaults = await GetGlobalDefaultPermissionsAsync();

    foreach (var permission in globalDefaults) effectivePermissions.Add(permission);

    // 2. Add tenant defaults (if applicable)
    if (tenantId.HasValue) {
      var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId);

      foreach (var permission in tenantDefaults) effectivePermissions.Add(permission);
    }

    // 3. Add user-specific permissions (override/add to defaults)
    var userPermissions = await GetTenantPermissionsAsync(userId, tenantId);

    foreach (var permission in userPermissions) effectivePermissions.Add(permission);

    return effectivePermissions;
  }

  // === USER-TENANT MEMBERSHIP FUNCTIONALITY ===

  public async Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(Guid userId) {
    return await context.TenantPermissions.AsNoTracking()
                        .Include(tp => tp.Tenant)
                        .Where(tp =>
                                 tp.UserId == userId &&
                                 tp.TenantId != null &&
                                 tp.DeletedAt == null &&
                                 (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                        )
                        .ToListAsync();
  }

  public async Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId) {
    // Check if user is already a member
    var existingMembership =
      await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                            tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
      );

    if (existingMembership != null) {
      // Reactivate if expired or deleted
      if (!existingMembership.IsValid) {
        existingMembership.ExpiresAt = null; // Remove expiration
        existingMembership.Restore(); // Undelete if deleted
        // Reset to minimal permissions only when reactivating
        existingMembership.PermissionFlags1 = 0UL; // Clear all permissions
        existingMembership.PermissionFlags2 = 0UL; // Clear all permissions

        foreach (var permission in TenantPermissionConstants.MinimalUserPermissions) existingMembership.AddPermission(permission);

        existingMembership.Touch();
        await context.SaveChangesAsync();
      }

      return existingMembership;
    }

    // Create new membership with minimal permissions
    var membership = new TenantPermission {
      UserId = userId, TenantId = tenantId,
      // IsActive replaced by IsValid property,
      // JoinedAt replaced by CreatedAt (inherited)
      // Status removed - using IsValid property instead
    };

    // Grant minimal permissions
    foreach (var permission in TenantPermissionConstants.MinimalUserPermissions) membership.AddPermission(permission);

    context.TenantPermissions.Add(membership);
    await context.SaveChangesAsync();

    return membership;
  }

  public async Task LeaveTenantAsync(Guid userId, Guid tenantId) {
    var membership = await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                                           tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
                     );

    if (membership != null) {
      // Instead of setting Status, we expire the membership or delete it
      membership.ExpiresAt = DateTime.UtcNow; // Expire immediately
      membership.Touch();
      await context.SaveChangesAsync();
    }
  }

  public async Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId) {
    var membership = await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                                           tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
                     );

    return membership?.IsActiveMembership == true;
  }

  public async Task<TenantPermission> UpdateTenantMembershipExpirationAsync(
    Guid userId, Guid tenantId,
    DateTime? expiresAt
  ) {
    var membership = await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                                           tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null
                     );

    if (membership == null) throw new InvalidOperationException("User is not a member of this tenant");

    membership.ExpiresAt = expiresAt;
    membership.Touch();

    await context.SaveChangesAsync();

    return membership;
  }

  // ===== LAYER 2: CONTENT-TYPE-WIDE PERMISSIONS =====

  public async Task GrantContentTypePermissionAsync(
    Guid? userId, Guid? tenantId, string contentTypeName,
    PermissionType[] permissions
  ) {
    if (string.IsNullOrWhiteSpace(contentTypeName)) throw new ArgumentException("Content type name cannot be null or empty", nameof(contentTypeName));
    if (permissions?.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    // Find existing permission record or create new one
    var existingPermission = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                        ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentType == contentTypeName && ctp.DeletedAt == null
                             );

    ContentTypePermission contentTypePermission;

    if (existingPermission != null) {
      // Update existing permission
      contentTypePermission = existingPermission;

      foreach (var permission in permissions!) contentTypePermission.AddPermission(permission);

      contentTypePermission.Touch();
    }
    else {
      // Create new permission record
      contentTypePermission = new ContentTypePermission {
        UserId = userId, TenantId = tenantId, ContentType = contentTypeName,
        // AssignedAt replaced by CreatedAt (inherited)
        // AssignedByUserId removed - will be tracked through permission logs
        // IsActive replaced by IsValid property
      };

      // Add permissions
      foreach (var permission in permissions!) contentTypePermission.AddPermission(permission);

      context.ContentTypePermissions.Add(contentTypePermission);
    }

    await context.SaveChangesAsync();
  }

  public async Task<bool> HasContentTypePermissionAsync(
    Guid userId, Guid? tenantId, string contentTypeName,
    PermissionType permission
  ) {
    if (string.IsNullOrWhiteSpace(contentTypeName)) return false;

    // Check user-specific content type permission
    var userPermission = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                    ctp.UserId == userId &&
                                                                                    ctp.TenantId == tenantId &&
                                                                                    ctp.ContentType == contentTypeName &&
                                                                                    ctp.DeletedAt == null &&
                                                                                    (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
                         );

    if (userPermission?.HasPermission(permission) == true) return true; // Check tenant default for this content type

    if (tenantId.HasValue) {
      var tenantDefault = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                     ctp.UserId == null &&
                                                                                     ctp.TenantId == tenantId &&
                                                                                     ctp.ContentType == contentTypeName &&
                                                                                     ctp.DeletedAt == null &&
                                                                                     (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
                          );

      if (tenantDefault?.HasPermission(permission) == true) return true;
    }

    // Check global default for this content type
    var globalDefault = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                   ctp.UserId == null &&
                                                                                   ctp.TenantId == null &&
                                                                                   ctp.ContentType == contentTypeName &&
                                                                                   ctp.DeletedAt == null &&
                                                                                   (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
                        );

    if (globalDefault?.HasPermission(permission) == true) return true;

    // If content-type specific permissions don't exist, fall back to tenant-level permissions
    // This implements the permission hierarchy where tenant permissions apply to all content types
    if (tenantId.HasValue) return await HasTenantPermissionAsync(userId, tenantId, permission);

    // If nothing found, check global tenant permissions as last resort
    return await CheckDefaultPermissionAsync(null, permission);
  }

  public async Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(
    Guid? userId, Guid? tenantId,
    string contentTypeName
  ) {
    if (string.IsNullOrWhiteSpace(contentTypeName)) return [];

    var contentTypePermission = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                           ctp.UserId == userId &&
                                                                                           ctp.TenantId == tenantId &&
                                                                                           ctp.ContentType == contentTypeName &&
                                                                                           ctp.DeletedAt == null &&
                                                                                           (ctp.ExpiresAt == null || ctp.ExpiresAt > DateTime.UtcNow)
                                );

    if (contentTypePermission == null) return [];

    return GetPermissionsFromContentTypeEntity(contentTypePermission);
  }

  public async Task RevokeContentTypePermissionAsync(
    Guid? userId, Guid? tenantId, string contentTypeName,
    PermissionType[] permissions
  ) {
    if (string.IsNullOrWhiteSpace(contentTypeName)) throw new ArgumentException("Content type name cannot be null or empty", nameof(contentTypeName));

    if (permissions?.Length == 0) return; // Nothing to revoke

    var existingPermission = await context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                        ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentType == contentTypeName && ctp.DeletedAt == null
                             );

    if (existingPermission != null) {
      foreach (var permission in permissions!) existingPermission.RemovePermission(permission);

      existingPermission.Touch();
      await context.SaveChangesAsync();
    }
  }

  private static IEnumerable<PermissionType> GetPermissionsFromContentTypeEntity(ContentTypePermission permission) {
    var permissions = new List<PermissionType>();

    // Check all possible permission types, using distinct values to avoid duplicates like Delete/SoftDelete
    foreach (var permissionType in Enum.GetValues<PermissionType>().Distinct()) {
      if (permission.HasPermission(permissionType)) permissions.Add(permissionType);
    }

    return permissions;
  }

  // === PRIVATE HELPER METHODS ===

  private async Task<bool> CheckDefaultPermissionAsync(Guid? tenantId, PermissionType permission) {
    // Check tenant default
    if (tenantId.HasValue) {
      var tenantDefault = await context.TenantPermissions.AsNoTracking()
                                       .FirstOrDefaultAsync(tp =>
                                                              tp.UserId == null && tp.TenantId == tenantId && tp.ExpiresAt == null && tp.DeletedAt == null
                                       );

      if (tenantDefault?.IsValid == true && tenantDefault.HasPermission(permission)) return true;
    }

    // Check global default
    var globalDefault = await context.TenantPermissions.AsNoTracking()
                                     .FirstOrDefaultAsync(tp =>
                                                            tp.UserId == null && tp.TenantId == null && tp.ExpiresAt == null && tp.DeletedAt == null
                                     );

    return globalDefault?.IsValid == true && globalDefault.HasPermission(permission);
  }

  private static IEnumerable<PermissionType> GetPermissionsFromEntity(TenantPermission permission) {
    var permissions = new List<PermissionType>();

    // Check all possible permission types, using distinct values to avoid duplicates like Delete/SoftDelete
    foreach (var permissionType in Enum.GetValues<PermissionType>().Distinct()) {
      if (permission.HasPermission(permissionType)) permissions.Add(permissionType);
    }

    return permissions;
  }

  // ===== HELPER METHODS =====

  public async Task<Guid?> GetResourceTenantIdAsync(Guid resourceId) {
    // TODO: Implement when we have concrete resource entities
    // This will need to query the specific resource tables to find tenant ID
    await Task.CompletedTask;

    return null;
  }

  public async Task<string?> GetResourceContentTypeAsync(Guid resourceId) {
    // TODO: Implement when we have concrete resource entities
    // This will need to determine the content type based on the resource
    await Task.CompletedTask;

    return null;
  }

  // ===== LAYER 3: RESOURCE-ENTRY PERMISSIONS IMPLEMENTATION =====

  public async Task GrantResourcePermissionAsync<TPermission, TResource>(
    Guid? userId, Guid? tenantId, Guid resourceId,
    PermissionType[] permissions
  ) where TPermission : ResourcePermission<TResource>, new() where TResource : Entity {
    var existingPermission = await context.Set<TPermission>()
                                          .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId);

    if (existingPermission != null) {
      // Update existing permissions
      foreach (var permission in permissions) existingPermission.AddPermission(permission);

      existingPermission.Touch();
    }
    else {
      // Create new permission record
      var newPermission = new TPermission { UserId = userId, TenantId = tenantId, ResourceId = resourceId };

      foreach (var permission in permissions) newPermission.AddPermission(permission);

      context.Set<TPermission>().Add(newPermission);
    }

    await context.SaveChangesAsync();
  }

  public async Task<bool> HasResourcePermissionAsync<TPermission, TResource>(
    Guid userId, Guid? tenantId,
    Guid resourceId, PermissionType permission
  ) where TPermission : ResourcePermission<TResource>
    where TResource : Entity {
    var resourcePermission = await context.Set<TPermission>()
                                          .FirstOrDefaultAsync(p =>
                                                                 p.UserId == userId &&
                                                                 p.TenantId == tenantId &&
                                                                 p.ResourceId == resourceId &&
                                                                 p.ExpiresAt == null &&
                                                                 p.DeletedAt == null
                                          );

    return resourcePermission?.HasPermission(permission) ?? false;
  }

  public async Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(
    Guid? userId,
    Guid? tenantId, Guid resourceId
  ) where TPermission : ResourcePermission<TResource> where TResource : Entity {
    var resourcePermission = await context.Set<TPermission>()
                                          .FirstOrDefaultAsync(p =>
                                                                 p.UserId == userId &&
                                                                 p.TenantId == tenantId &&
                                                                 p.ResourceId == resourceId &&
                                                                 p.ExpiresAt == null &&
                                                                 p.DeletedAt == null
                                          );

    if (resourcePermission == null) return [];

    var permissions = new HashSet<PermissionType>();

    foreach (var permissionType in Enum.GetValues<PermissionType>()) {
      if (resourcePermission.HasPermission(permissionType)) permissions.Add(permissionType);
    }

    return permissions;
  }

  public async Task RevokeResourcePermissionAsync<TPermission, TResource>(
    Guid? userId, Guid? tenantId, Guid resourceId,
    PermissionType[] permissions
  ) where TPermission : ResourcePermission<TResource> where TResource : Entity {
    var existingPermission = await context.Set<TPermission>()
                                          .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.ResourceId == resourceId);

    if (existingPermission != null) {
      foreach (var permission in permissions) existingPermission.RemovePermission(permission);

      existingPermission.Touch();
      await context.SaveChangesAsync();
    }
  }

  public async Task<Dictionary<Guid, IEnumerable<PermissionType>>>
    GetBulkResourcePermissionsAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds)
    where TPermission : ResourcePermission<TResource> where TResource : Entity {
    if (resourceIds == null || resourceIds.Length == 0) return new Dictionary<Guid, IEnumerable<PermissionType>>();

    var permissions = await context.Set<TPermission>()
                                   .Where(p =>
                                            p.UserId == userId &&
                                            p.TenantId == tenantId &&
                                            resourceIds.Contains(p.ResourceId) &&
                                            p.ExpiresAt == null &&
                                            p.DeletedAt == null
                                   )
                                   .ToListAsync();

    var result = new Dictionary<Guid, IEnumerable<PermissionType>>();

    // Pre-cache enum values to avoid repeated Enum.GetValues calls
    var allPermissionTypes = Enum.GetValues<PermissionType>();

    foreach (var permission in permissions) {
      var permissionTypes = new List<PermissionType>();

      foreach (var permType in allPermissionTypes) {
        if (permission.HasPermission(permType)) permissionTypes.Add(permType);
      }

      result[permission.ResourceId] = permissionTypes;
    }

    return result;
  }

  public async Task ShareResourceAsync<TPermission, TResource>(
    Guid resourceId, Guid targetUserId, Guid? tenantId,
    PermissionType[] permissions, DateTime? expiresAt = null
  )
    where TPermission : ResourcePermission<TResource>, new() where TResource : Entity {
    await GrantResourcePermissionAsync<TPermission, TResource>(
      targetUserId,
      tenantId,
      resourceId,
      permissions
    );

    // Set expiration if provided
    if (expiresAt.HasValue) {
      var permission = await context.Set<TPermission>()
                                    .FirstOrDefaultAsync(p =>
                                                           p.UserId == targetUserId && p.TenantId == tenantId && p.ResourceId == resourceId
                                    );

      if (permission != null) {
        permission.ExpiresAt = expiresAt.Value;
        permission.Touch();
        await context.SaveChangesAsync();
      }
    }
  }

  public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
    Guid[] userIds, Guid tenantId,
    PermissionType[] permissions
  ) {
    if (userIds == null || userIds.Length == 0) throw new ArgumentException("At least one user ID must be specified", nameof(userIds));

    if (permissions == null) throw new ArgumentNullException(nameof(permissions));

    if (permissions.Length == 0) throw new ArgumentException("At least one permission must be specified", nameof(permissions));

    if (permissions.Length > TenantPermissionConstants.MaxPermissionsPerGrant)
      throw new ArgumentException(
        $"Cannot grant more than {TenantPermissionConstants.MaxPermissionsPerGrant} permissions at once",
        nameof(permissions)
      );

    // Get existing permissions for all users in this tenant
    var existingPermissions = await context.TenantPermissions
                                           .Where(tp => userIds.Contains(tp.UserId!.Value) && tp.TenantId == tenantId && tp.DeletedAt == null)
                                           .ToListAsync();

    var existingPermissionLookup = existingPermissions.ToDictionary(ep => ep.UserId!.Value);
    var result = new List<TenantPermission>();

    foreach (var userId in userIds) {
      TenantPermission tenantPermission;

      if (existingPermissionLookup.TryGetValue(userId, out var existingPermission)) {
        // Update existing permission
        tenantPermission = existingPermission;

        foreach (var permission in permissions) tenantPermission.AddPermission(permission);

        tenantPermission.Touch();
      }
      else {
        // Create new permission record
        tenantPermission = new TenantPermission { UserId = userId, TenantId = tenantId, };

        foreach (var permission in permissions) tenantPermission.AddPermission(permission);

        context.TenantPermissions.Add(tenantPermission);
      }

      result.Add(tenantPermission);
    }

    // Single SaveChangesAsync for all operations
    await context.SaveChangesAsync();

    return result;
  }

  public async Task BulkGrantResourcePermissionAsync<TPermission, TResource>(
    Guid userId, Guid? tenantId,
    Guid[] resourceIds, PermissionType[] permissions
  )
    where TPermission : ResourcePermission<TResource>, new() where TResource : Entity {
    if (resourceIds == null || resourceIds.Length == 0) return;
    if (permissions == null || permissions.Length == 0) return;

    var existingPermissions = await context.Set<TPermission>()
                                           .Where(p =>
                                                    p.UserId == userId && p.TenantId == tenantId && resourceIds.Contains(p.ResourceId) && p.DeletedAt == null
                                           )
                                           .ToListAsync();

    var existingResourceIds = existingPermissions.Select(p => p.ResourceId).ToHashSet();
    var newResourceIds = resourceIds.Except(existingResourceIds).ToArray();

    // Update existing permissions
    foreach (var existingPermission in existingPermissions) {
      foreach (var permission in permissions) existingPermission.AddPermission(permission);

      existingPermission.Touch();
    }

    // Create new permissions for resources that don't have any
    var newPermissions = new List<TPermission>();

    foreach (var resourceId in newResourceIds) {
      var resourcePermission = new TPermission { UserId = userId, TenantId = tenantId, ResourceId = resourceId };

      foreach (var permission in permissions) resourcePermission.AddPermission(permission);

      newPermissions.Add(resourcePermission);
    }

    if (newPermissions.Count > 0) context.Set<TPermission>().AddRange(newPermissions);

    await context.SaveChangesAsync();
  }
}
