using GameGuild.Core.Domain.Identity;
using GameGuild.Core.Domain.Permissions;


namespace GameGuild.Authorization.Identity;

/// <summary> Implementation of permissions context for the current request Provides centralized permission checking and authorization services </summary>
public class PermissionsContext : IPermissionsContext {
  private readonly IDacPermissionResolver _dacPermissionResolver;

  private readonly ILogger<PermissionsContext> _logger;

  private readonly IModulePermissionService _modulePermissionService;

  private readonly IPermissionService _permissionService;

  private readonly ITenantContext _tenantContext;

  private readonly IUserContext _userContext;

  public PermissionsContext(
    IUserContext userContext,
    ITenantContext tenantContext,
    IPermissionService permissionService,
    IDacPermissionResolver dacPermissionResolver,
    IModulePermissionService modulePermissionService,
    ILogger<PermissionsContext> logger
  ) {
    _userContext = userContext;
    _tenantContext = tenantContext;
    _permissionService = permissionService;
    _dacPermissionResolver = dacPermissionResolver;
    _modulePermissionService = modulePermissionService;
    _logger = logger;
  }

  // === CONTEXT PROPERTIES ===

  public Guid? UserId { get => _userContext.UserId; }

  public Guid? TenantId { get => _tenantContext.TenantId; }

  public bool IsAuthenticated { get => _userContext.IsAuthenticated; }

  public bool IsSystemAdmin { get => _userContext.IsInRole("SystemAdmin") || _userContext.IsInRole("SuperAdmin"); }

  public bool IsTenantAdmin { get => _userContext.IsInRole("TenantAdmin") || _userContext.IsInRole("Admin"); }

  // === BASIC PERMISSION CHECKS ===

  public async Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null) {
    if (!IsAuthenticated || !UserId.HasValue) {
      _logger.LogDebug("Permission check failed: User not authenticated");

      return false;
    }

    var effectiveTenantId = tenantId ?? TenantId;

    try { return await _permissionService.HasTenantPermissionAsync(UserId.Value, effectiveTenantId, permission); }
    catch (Exception ex) {
      _logger.LogWarning(ex, "Error checking tenant permission {Permission} for user {UserId} in tenant {TenantId}", permission, UserId, effectiveTenantId);

      return false;
    }
  }

  public async Task<bool> HasContentTypePermissionAsync(PermissionType permission, string contentType, Guid? tenantId = null) {
    if (!IsAuthenticated || !UserId.HasValue) {
      _logger.LogDebug("Permission check failed: User not authenticated");

      return false;
    }

    var effectiveTenantId = tenantId ?? TenantId;

    try { return await _permissionService.HasContentTypePermissionAsync(UserId.Value, effectiveTenantId, contentType, permission); }
    catch (Exception ex) {
      _logger.LogWarning(ex, "Error checking content type permission {Permission} for user {UserId} in tenant {TenantId} for content type {ContentType}", permission, UserId, effectiveTenantId, contentType);

      return false;
    }
  }

  public async Task<bool> HasResourcePermissionAsync(PermissionType permission, string resourceType, Guid resourceId, Guid? tenantId = null) {
    if (!IsAuthenticated || !UserId.HasValue) {
      _logger.LogDebug("Permission check failed: User not authenticated");

      return false;
    }

    var effectiveTenantId = tenantId ?? TenantId;

    try { return await _permissionService.HasResourcePermissionAsync(UserId.Value, effectiveTenantId, resourceType, resourceId, permission); }
    catch (Exception ex) {
      _logger.LogWarning(ex, "Error checking resource permission {Permission} for user {UserId} in tenant {TenantId} for resource {ResourceType}:{ResourceId}", permission, UserId, effectiveTenantId, resourceType, resourceId);

      return false;
    }
  }

  public async Task<bool> HasAnyTenantPermissionAsync(PermissionType[] permissions, Guid? tenantId = null) {
    if (!(permissions?.Length > 0)) return false;

    foreach (var permission in permissions) {
      if (await HasTenantPermissionAsync(permission, tenantId)) { return true; }
    }

    return false;
  }
}
