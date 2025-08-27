using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Common.Services;

/// <summary>
/// Implementation of the enhanced DAC Permission Resolver
/// </summary>
public class DacPermissionResolver : IDacPermissionResolver {
  private readonly IPermissionService _permissionService;
  private readonly ILogger<DacPermissionResolver> _logger;

  public DacPermissionResolver(
      IPermissionService permissionService,
      ILogger<DacPermissionResolver> logger) {
    _permissionService = permissionService;
    _logger = logger;
  }

  public async Task<PermissionResult> ResolvePermissionAsync<TResource>(
      Guid userId,
      Guid? tenantId,
      PermissionType permission,
      Guid? resourceId = null,
      string? contentTypeName = null) where TResource : Entity {
    var hierarchy = await GetPermissionHierarchyAsync<TResource>(
        userId, tenantId, permission, resourceId, contentTypeName);

    return hierarchy.FinalResult;
  }

  public async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync<TResource>(
      Guid userId,
      Guid? tenantId,
      Guid? resourceId = null,
      string? contentTypeName = null) where TResource : Entity {
    var effectivePermissions = new List<EffectivePermission>();

    // Get all possible permissions from the enum
    var allPermissions = Enum.GetValues<PermissionType>();

    foreach (var permission in allPermissions) {
      var result = await ResolvePermissionAsync<TResource>(
          userId, tenantId, permission, resourceId, contentTypeName);

      if (result.IsGranted) {
        effectivePermissions.Add(new EffectivePermission {
          Permission = permission,
          IsGranted = result.IsGranted,
          Source = result.Source,
          SourceDescription = GetSourceDescription(result.Source),
          GrantedBy = result.GrantedBy,
          GrantedAt = result.GrantedAt,
          ExpiresAt = result.ExpiresAt,
          IsInherited = result.IsInherited,
          IsExplicit = !result.IsInherited,
          Priority = result.Priority,
        });
      }
    }

    return effectivePermissions.OrderBy(p => p.Priority);
  }

  public async Task<bool> CanGrantPermissionsAsync(
      Guid grantorUserId,
      Guid? tenantId,
      PermissionType[] permissions,
      Guid? resourceId = null,
      string? contentTypeName = null) {
    // User can only grant permissions they have themselves
    foreach (var permission in permissions) {
      var canGrant = resourceId.HasValue
          ? await _permissionService.HasResourcePermissionAsync<ResourcePermission<Entity>, Entity>(
              grantorUserId, tenantId, resourceId.Value, permission)
          : contentTypeName != null
              ? await _permissionService.HasContentTypePermissionAsync(
                  grantorUserId, tenantId, contentTypeName, permission)
              : await _permissionService.HasTenantPermissionAsync(grantorUserId, tenantId, permission);

      if (!canGrant) {
        _logger.LogWarning(
            "User {UserId} attempted to grant permission {Permission} they don't have in tenant {TenantId}",
            grantorUserId, permission, tenantId);
        return false;
      }
    }

    return true;
  }

  public async Task<PermissionHierarchy> GetPermissionHierarchyAsync<TResource>(
      Guid userId,
      Guid? tenantId,
      PermissionType permission,
      Guid? resourceId = null,
      string? contentTypeName = null) where TResource : Entity {
    var hierarchy = new PermissionHierarchy {
      Permission = permission,
      UserId = userId,
      TenantId = tenantId,
      ResourceId = resourceId,
      ContentTypeName = contentTypeName,
    };

    var layers = new List<PermissionLayer>();

    // Layer 1: Global Defaults (Priority 1)
    var globalDefaults = await _permissionService.GetGlobalDefaultPermissionsAsync();
    if (globalDefaults.Contains(permission)) {
      layers.Add(new PermissionLayer {
        Source = PermissionSource.GlobalDefault,
        IsGranted = true,
        IsDefault = true,
        Priority = 1,
        Description = "Global default permission",
      });
    }

    // Layer 2: Tenant Defaults (Priority 2)
    if (tenantId.HasValue) {
      var tenantDefaults = await _permissionService.GetTenantDefaultPermissionsAsync(tenantId);
      if (tenantDefaults.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.TenantDefault,
          IsGranted = true,
          IsDefault = true,
          Priority = 2,
          Description = $"Tenant {tenantId} default permission",
        });
      }
    }

    // Layer 3: Content Type Defaults (Priority 3)
    if (contentTypeName != null) {
      var contentTypePermissions = await _permissionService.GetContentTypePermissionsAsync(
          null, tenantId, contentTypeName);
      if (contentTypePermissions.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.ContentTypeDefault,
          IsGranted = true,
          IsDefault = true,
          Priority = 3,
          Description = $"Content type {contentTypeName} default permission",
        });
      }
    }

    // Layer 4: User Tenant Permissions (Priority 4)
    if (tenantId.HasValue) {
      var userTenantPermissions = await _permissionService.GetTenantPermissionsAsync(userId, tenantId);
      if (userTenantPermissions.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.TenantUser,
          IsGranted = true,
          IsDefault = false,
          Priority = 4,
          Description = $"User {userId} tenant {tenantId} permission",
        });
      }
    }

    // Layer 5: User Content Type Permissions (Priority 5)
    if (contentTypeName != null) {
      var userContentTypePermissions = await _permissionService.GetContentTypePermissionsAsync(
          userId, tenantId, contentTypeName);
      if (userContentTypePermissions.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.ContentTypeUser,
          IsGranted = true,
          IsDefault = false,
          Priority = 5,
          Description = $"User {userId} content type {contentTypeName} permission",
        });
      }
    }

    // Layer 6: Resource Defaults (Priority 6)
    if (resourceId.HasValue) {
      var resourceDefaults = await _permissionService.GetResourcePermissionsAsync<ResourcePermission<TResource>, TResource>(
          null, tenantId, resourceId.Value);
      if (resourceDefaults.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.ResourceDefault,
          IsGranted = true,
          IsDefault = true,
          Priority = 6,
          Description = $"Resource {resourceId} default permission",
        });
      }
    }

    // Layer 7: User Resource Permissions (Priority 7 - Highest)
    if (resourceId.HasValue) {
      var userResourcePermissions = await _permissionService.GetResourcePermissionsAsync<ResourcePermission<TResource>, TResource>(
          userId, tenantId, resourceId.Value);
      if (userResourcePermissions.Contains(permission)) {
        layers.Add(new PermissionLayer {
          Source = PermissionSource.ResourceUser,
          IsGranted = true,
          IsDefault = false,
          Priority = 7,
          Description = $"User {userId} resource {resourceId} permission",
        });
      }
    }

    hierarchy.Layers = layers.OrderBy(l => l.Priority).ToList();

    // Determine final result - highest priority layer wins
    var highestPriorityLayer = hierarchy.Layers
        .Where(l => l.IsGranted.HasValue)
        .OrderByDescending(l => l.Priority)
        .FirstOrDefault();

    hierarchy.FinalResult = new PermissionResult {
      IsGranted = highestPriorityLayer?.IsGranted == true,
      Source = highestPriorityLayer?.Source ?? PermissionSource.None,
      GrantedBy = highestPriorityLayer?.GrantedBy,
      GrantedAt = highestPriorityLayer?.GrantedAt,
      ExpiresAt = highestPriorityLayer?.ExpiresAt,
      Priority = highestPriorityLayer?.Priority ?? 0,
      IsInherited = highestPriorityLayer?.IsDefault == true,
      Reason = highestPriorityLayer?.Description ?? "No permission found",
    };

    return hierarchy;
  }

  public async Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>> BulkResolvePermissionsAsync<TResource>(
      Guid userId,
      Guid? tenantId,
      Guid[] resourceIds,
      PermissionType[] permissions) where TResource : Entity {
    var results = new Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>();

    foreach (var resourceId in resourceIds) {
      var resourceResults = new Dictionary<PermissionType, PermissionResult>();

      foreach (var permission in permissions) {
        var result = await ResolvePermissionAsync<TResource>(
            userId, tenantId, permission, resourceId);
        resourceResults[permission] = result;
      }

      results[resourceId] = resourceResults;
    }

    return results;
  }

  private static string GetSourceDescription(PermissionSource source) {
    return source switch {
      PermissionSource.GlobalDefault => "Global default permissions",
      PermissionSource.TenantDefault => "Tenant default permissions",
      PermissionSource.ContentTypeDefault => "Content type default permissions",
      PermissionSource.TenantUser => "User tenant permissions",
      PermissionSource.ContentTypeUser => "User content type permissions",
      PermissionSource.ResourceDefault => "Resource default permissions",
      PermissionSource.ResourceUser => "User resource permissions",
      PermissionSource.SystemOverride => "System override",
      _ => "Unknown source",
    };
  }
}
