using GameGuild.Core.Domain.Permissions;

namespace GameGuild.Core.Infrastructure.Permissions;

/// <summary> Enhanced DAC Permission Resolver implementing three-layer permission system Follows Clean Architecture with proper separation of concerns </summary>
public class DacPermissionResolver : IDacPermissionResolver {
  private readonly ILogger<DacPermissionResolver> _logger;

  private readonly IPermissionService _permissionService;

  public DacPermissionResolver(IPermissionService permissionService, ILogger<DacPermissionResolver> logger) {
    _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<PermissionResult> ResolvePermissionAsync<TResource>(Guid userId, Guid? tenantId, PermissionType permission, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase {
    try {
      var hierarchy = await GetPermissionHierarchyAsync<TResource>(userId, tenantId, permission, resourceId, contentTypeName);

      _logger.LogDebug("Permission {Permission} resolved for user {UserId} in tenant {TenantId}: {IsGranted} from {Source}", permission, userId, tenantId, hierarchy.FinalResult.IsGranted, hierarchy.FinalResult.Source);

      return hierarchy.FinalResult;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error resolving permission {Permission} for user {UserId} in tenant {TenantId}", permission, userId, tenantId);

      return new PermissionResult { IsGranted = false, Source = PermissionSource.None, Reason = "Error during permission resolution", Priority = 0 };
    }
  }

  public async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync<TResource>(Guid userId, Guid? tenantId, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase {
    var effectivePermissions = new List<EffectivePermission>();

    try {
      // Get all possible permissions from the enum
      var allPermissions = Enum.GetValues<PermissionType>();

      foreach (var permission in allPermissions) {
        var result = await ResolvePermissionAsync<TResource>(userId, tenantId, permission, resourceId, contentTypeName);

        if (result.IsGranted) {
          effectivePermissions.Add(
            new EffectivePermission {
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
            }
          );
        }
      }

      return effectivePermissions.OrderBy(p => p.Priority);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting effective permissions for user {UserId} in tenant {TenantId}", userId, tenantId);

      return [];
    }
  }

  public async Task<bool> CanGrantPermissionsAsync(Guid grantorUserId, Guid? tenantId, PermissionType[] permissions, Guid? resourceId = null, string? contentTypeName = null) {
    try {
      // User can only grant permissions they have themselves
      foreach (var permission in permissions) {
        var canGrant = await HasPermissionInContext(grantorUserId, tenantId, permission, resourceId, contentTypeName);

        if (!canGrant) {
          _logger.LogWarning("User {UserId} attempted to grant permission {Permission} they don't have in tenant {TenantId}", grantorUserId, permission, tenantId);

          return false;
        }
      }

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking grant permissions for user {UserId} in tenant {TenantId}", grantorUserId, tenantId);

      return false;
    }
  }

  public async Task<PermissionHierarchy> GetPermissionHierarchyAsync<TResource>(Guid userId, Guid? tenantId, PermissionType permission, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase {
    var hierarchy = new PermissionHierarchy { Permission = permission, UserId = userId, TenantId = tenantId, ResourceId = resourceId, ContentTypeName = contentTypeName };

    var layers = new List<PermissionLayer>();

    try {
      // Layer 1: Global Defaults (Priority 1)
      await AddGlobalDefaultLayer(layers, permission);

      // Layer 2: Tenant Defaults (Priority 2)
      if (tenantId.HasValue) { await AddTenantDefaultLayer(layers, tenantId.Value, permission); }

      // Layer 3: Content Type Defaults (Priority 3)
      if (contentTypeName != null) { await AddContentTypeDefaultLayer(layers, tenantId, contentTypeName, permission); }

      // Layer 4: User Tenant Permissions (Priority 4)
      if (tenantId.HasValue) { await AddUserTenantLayer(layers, userId, tenantId.Value, permission); }

      // Layer 5: User Content Type Permissions (Priority 5)
      if (contentTypeName != null) { await AddUserContentTypeLayer(layers, userId, tenantId, contentTypeName, permission); }

      // Layer 6: Resource Defaults (Priority 6)
      if (resourceId.HasValue) { await AddResourceDefaultLayer<TResource>(layers, tenantId, resourceId.Value, permission); }

      // Layer 7: User Resource Permissions (Priority 7 - Highest)
      if (resourceId.HasValue) { await AddUserResourceLayer<TResource>(layers, userId, tenantId, resourceId.Value, permission); }

      hierarchy.Layers = layers.OrderBy(l => l.Priority).ToList();

      // Determine final result - highest priority layer wins
      var effectiveLayer = DetermineFinalResult(hierarchy.Layers);
      hierarchy.FinalResult = CreateFinalResult(effectiveLayer);

      return hierarchy;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error building permission hierarchy for user {UserId} in tenant {TenantId}", userId, tenantId);

      hierarchy.FinalResult = new PermissionResult { IsGranted = false, Source = PermissionSource.None, Reason = "Error building permission hierarchy", Priority = 0 };

      return hierarchy;
    }
  }

  public async Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>> BulkResolvePermissionsAsync<TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds, PermissionType[] permissions) where TResource : EntityBase {
    var results = new Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>();

    try {
      foreach (var resourceId in resourceIds) {
        var resourceResults = new Dictionary<PermissionType, PermissionResult>();

        foreach (var permission in permissions) {
          var result = await ResolvePermissionAsync<TResource>(userId, tenantId, permission, resourceId);
          resourceResults[permission] = result;
        }

        results[resourceId] = resourceResults;
      }

      return results;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error in bulk permission resolution for user {UserId} in tenant {TenantId}", userId, tenantId);

      return new Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>();
    }
  }

  #region Private Helper Methods

  private async Task<bool> HasPermissionInContext(Guid userId, Guid? tenantId, PermissionType permission, Guid? resourceId, string? contentTypeName) {
    if (resourceId.HasValue) { return await _permissionService.HasResourcePermissionAsync<ResourcePermission<EntityBase>, EntityBase>(userId, tenantId, resourceId.Value, permission); }

    if (contentTypeName != null) { return await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, permission); }

    return await _permissionService.HasTenantPermissionAsync(userId, tenantId, permission);
  }

  private async Task AddGlobalDefaultLayer(List<PermissionLayer> layers, PermissionType permission) {
    var globalDefaults = await _permissionService.GetGlobalDefaultPermissionsAsync();

    if (globalDefaults.Contains(permission)) { layers.Add(new PermissionLayer { Source = PermissionSource.GlobalDefault, IsGranted = true, IsDefault = true, Priority = 1, Description = "Global default permission" }); }
  }

  private async Task AddTenantDefaultLayer(List<PermissionLayer> layers, Guid tenantId, PermissionType permission) {
    var tenantDefaults = await _permissionService.GetTenantDefaultPermissionsAsync(tenantId);

    if (tenantDefaults.Contains(permission)) { layers.Add(new PermissionLayer { Source = PermissionSource.TenantDefault, IsGranted = true, IsDefault = true, Priority = 2, Description = $"Tenant {tenantId} default permission" }); }
  }

  private async Task AddContentTypeDefaultLayer(List<PermissionLayer> layers, Guid? tenantId, string contentTypeName, PermissionType permission) {
    var contentTypePermissions = await _permissionService.GetContentTypePermissionsAsync(null, tenantId, contentTypeName);

    if (contentTypePermissions.Contains(permission)) {
      layers.Add(new PermissionLayer { Source = PermissionSource.ContentTypeDefault, IsGranted = true, IsDefault = true, Priority = 3, Description = $"Content type {contentTypeName} default permission" });
    }
  }

  private async Task AddUserTenantLayer(List<PermissionLayer> layers, Guid userId, Guid tenantId, PermissionType permission) {
    var userTenantPermissions = await _permissionService.GetTenantPermissionsAsync(userId, tenantId);

    if (userTenantPermissions.Contains(permission)) {
      layers.Add(new PermissionLayer { Source = PermissionSource.TenantUser, IsGranted = true, IsDefault = false, Priority = 4, Description = $"User {userId} tenant {tenantId} permission" });
    }
  }

  private async Task AddUserContentTypeLayer(List<PermissionLayer> layers, Guid userId, Guid? tenantId, string contentTypeName, PermissionType permission) {
    var userContentTypePermissions = await _permissionService.GetContentTypePermissionsAsync(userId, tenantId, contentTypeName);

    if (userContentTypePermissions.Contains(permission)) {
      layers.Add(new PermissionLayer { Source = PermissionSource.ContentTypeUser, IsGranted = true, IsDefault = false, Priority = 5, Description = $"User {userId} content type {contentTypeName} permission" });
    }
  }

  private async Task AddResourceDefaultLayer<TResource>(List<PermissionLayer> layers, Guid? tenantId, Guid resourceId, PermissionType permission) where TResource : EntityBase {
    var resourceDefaults = await _permissionService.GetResourcePermissionsAsync<ResourcePermission<TResource>, TResource>(null, tenantId, resourceId);

    if (resourceDefaults.Contains(permission)) { layers.Add(new PermissionLayer { Source = PermissionSource.ResourceDefault, IsGranted = true, IsDefault = true, Priority = 6, Description = $"Resource {resourceId} default permission" }); }
  }

  private async Task AddUserResourceLayer<TResource>(List<PermissionLayer> layers, Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission) where TResource : EntityBase {
    var userResourcePermissions = await _permissionService.GetResourcePermissionsAsync<ResourcePermission<TResource>, TResource>(userId, tenantId, resourceId);

    if (userResourcePermissions.Contains(permission)) {
      layers.Add(new PermissionLayer { Source = PermissionSource.ResourceUser, IsGranted = true, IsDefault = false, Priority = 7, Description = $"User {userId} resource {resourceId} permission" });
    }
  }

  private static PermissionLayer? DetermineFinalResult(List<PermissionLayer> layers) { return layers.Where(l => l.IsGranted.HasValue).OrderByDescending(l => l.Priority).FirstOrDefault(); }

  private static PermissionResult CreateFinalResult(PermissionLayer? effectiveLayer) {
    return new PermissionResult {
      IsGranted = effectiveLayer?.IsGranted == true,
      Source = effectiveLayer?.Source ?? PermissionSource.None,
      GrantedBy = effectiveLayer?.GrantedBy,
      GrantedAt = effectiveLayer?.GrantedAt,
      ExpiresAt = effectiveLayer?.ExpiresAt,
      Priority = effectiveLayer?.Priority ?? 0,
      IsInherited = effectiveLayer?.IsDefault == true,
      Reason = effectiveLayer?.Description ?? "No permission found",
    };
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

  #endregion
}
