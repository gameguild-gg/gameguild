using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;


namespace GameGuild.Common;

/// <summary>
/// Implementation of permissions context for the current request
/// Provides centralized permission checking and authorization services
/// </summary>
public class PermissionsContext : IPermissionsContext
{
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IPermissionService _permissionService;
    private readonly IDacPermissionResolver _dacPermissionResolver;
    private readonly IModulePermissionService _modulePermissionService;
    private readonly ILogger<PermissionsContext> _logger;

    public PermissionsContext(
        IUserContext userContext,
        ITenantContext tenantContext,
        IPermissionService permissionService,
        IDacPermissionResolver dacPermissionResolver,
        IModulePermissionService modulePermissionService,
        ILogger<PermissionsContext> logger)
    {
        _userContext = userContext;
        _tenantContext = tenantContext;
        _permissionService = permissionService;
        _dacPermissionResolver = dacPermissionResolver;
        _modulePermissionService = modulePermissionService;
        _logger = logger;
    }

    // === CONTEXT PROPERTIES ===

    public Guid? UserId => _userContext.UserId;

    public Guid? TenantId => _tenantContext.TenantId;

    public bool IsAuthenticated => _userContext.IsAuthenticated;

    public bool IsSystemAdmin => _userContext.IsInRole("SystemAdmin") || _userContext.IsInRole("SuperAdmin");

    public bool IsTenantAdmin => _userContext.IsInRole("TenantAdmin") || _userContext.IsInRole("Admin");

    // === BASIC PERMISSION CHECKS ===

    public async Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            _logger.LogDebug("Permission check failed: User not authenticated");
            return false;
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try
        {
            return await _permissionService.HasTenantPermissionAsync(UserId.Value, effectiveTenantId, permission);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking tenant permission {Permission} for user {UserId} in tenant {TenantId}",
                permission, UserId, effectiveTenantId);
            return false;
        }
    }

    public async Task<bool> HasAnyTenantPermissionAsync(PermissionType[] permissions, Guid? tenantId = null)
    {
        if (!permissions?.Any() == true) return false;

        foreach (var permission in permissions)
        {
            if (await HasTenantPermissionAsync(permission, tenantId))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasAllTenantPermissionsAsync(PermissionType[] permissions, Guid? tenantId = null)
    {
        if (!permissions?.Any() == true) return true;

        foreach (var permission in permissions)
        {
            if (!await HasTenantPermissionAsync(permission, tenantId))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            return Array.Empty<PermissionType>();
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try
        {
            return await _permissionService.GetEffectiveTenantPermissionsAsync(UserId.Value, effectiveTenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting tenant permissions for user {UserId} in tenant {TenantId}",
                UserId, effectiveTenantId);
            return Array.Empty<PermissionType>();
        }
    }

    // === RESOURCE-SPECIFIC PERMISSIONS ===

    public async Task<bool> HasResourcePermissionAsync(Guid resourceId, PermissionType permission)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            return false;
        }

        try
        {
            return await _dacPermissionResolver.HasPermissionAsync(
                UserId.Value, resourceId, permission.ToString(), TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking resource permission {Permission} for user {UserId} on resource {ResourceId}",
                permission, UserId, resourceId);
            return false;
        }
    }

    public async Task<bool> HasModulePermissionAsync(Guid moduleId, PermissionType permission)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            return false;
        }

        try
        {
            return await _modulePermissionService.HasModulePermissionAsync(
                UserId.Value, moduleId, permission, TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking module permission {Permission} for user {UserId} on module {ModuleId}",
                permission, UserId, moduleId);
            return false;
        }
    }

    public async Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync(Guid resourceId)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            return Array.Empty<PermissionType>();
        }

        try
        {
            var permissions = await _dacPermissionResolver.GetUserPermissionsAsync(
                UserId.Value, resourceId, TenantId);

            return permissions.Select(p => Enum.TryParse<PermissionType>(p, out var permType) ? permType : PermissionType.Read)
                            .Where(p => p != default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting resource permissions for user {UserId} on resource {ResourceId}",
                UserId, resourceId);
            return Array.Empty<PermissionType>();
        }
    }

    public async Task<IEnumerable<PermissionType>> GetModulePermissionsAsync(Guid moduleId)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            return Array.Empty<PermissionType>();
        }

        try
        {
            return await _modulePermissionService.GetModulePermissionsAsync(
                UserId.Value, moduleId, TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting module permissions for user {UserId} on module {ModuleId}",
                UserId, moduleId);
            return Array.Empty<PermissionType>();
        }
    }

    // === ROLE-BASED CHECKS ===

    public bool HasRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return _userContext.IsInRole(role);
    }

    public bool HasAnyRole(string[] roles)
    {
        if (!roles?.Any() == true) return false;

        return roles.Any(role => _userContext.IsInRole(role));
    }

    public IEnumerable<string> GetRoles()
    {
        return _userContext.Roles;
    }

    // === EFFECTIVE PERMISSIONS ===

    public async Task<Dictionary<string, IEnumerable<PermissionType>>> GetEffectivePermissionsAsync(Guid? tenantId = null)
    {
        var result = new Dictionary<string, IEnumerable<PermissionType>>();

        if (!IsAuthenticated || !UserId.HasValue)
        {
            return result;
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try
        {
            // Get tenant permissions
            var tenantPermissions = await GetTenantPermissionsAsync(effectiveTenantId);
            result["tenant"] = tenantPermissions;

            // Get global permissions
            if (effectiveTenantId != null)
            {
                var globalPermissions = await GetTenantPermissionsAsync(null);
                result["global"] = globalPermissions;
            }

            _logger.LogDebug("Retrieved effective permissions for user {UserId} in tenant {TenantId}: {PermissionCount} contexts",
                UserId, effectiveTenantId, result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for user {UserId} in tenant {TenantId}",
                UserId, effectiveTenantId);
        }

        return result;
    }

    // === PERMISSION VALIDATION HELPERS ===

    public async Task ValidatePermissionAsync(PermissionType permission, Guid? tenantId = null, Guid? resourceId = null)
    {
        bool hasPermission;

        if (resourceId.HasValue)
        {
            hasPermission = await HasResourcePermissionAsync(resourceId.Value, permission);
        }
        else
        {
            hasPermission = await HasTenantPermissionAsync(permission, tenantId);
        }

        if (!hasPermission)
        {
            var context = resourceId.HasValue ? $"resource {resourceId}" : $"tenant {tenantId ?? TenantId}";
            var message = $"User {UserId} lacks {permission} permission on {context}";

            _logger.LogWarning("Authorization failed: {Message}", message);
            throw new UnauthorizedAccessException(message);
        }
    }

    public async Task ValidateAnyPermissionAsync(PermissionType[] permissions, Guid? tenantId = null, Guid? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        if (!permissions.Any())
        {
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));
        }

        bool hasAnyPermission;

        if (resourceId.HasValue)
        {
            hasAnyPermission = false;
            foreach (var permission in permissions)
            {
                if (await HasResourcePermissionAsync(resourceId.Value, permission))
                {
                    hasAnyPermission = true;
                    break;
                }
            }
        }
        else
        {
            hasAnyPermission = await HasAnyTenantPermissionAsync(permissions, tenantId);
        }

        if (!hasAnyPermission)
        {
            var context = resourceId.HasValue ? $"resource {resourceId}" : $"tenant {tenantId ?? TenantId}";
            var permissionsList = string.Join(", ", permissions.Select(p => p.ToString()));
            var message = $"User {UserId} lacks any of the required permissions ({permissionsList}) on {context}";

            _logger.LogWarning("Authorization failed: {Message}", message);
            throw new UnauthorizedAccessException(message);
        }
    }

    public async Task ValidateAllPermissionsAsync(PermissionType[] permissions, Guid? tenantId = null, Guid? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        if (!permissions.Any()) return; // No permissions required

        bool hasAllPermissions;

        if (resourceId.HasValue)
        {
            hasAllPermissions = true;
            foreach (var permission in permissions)
            {
                if (!await HasResourcePermissionAsync(resourceId.Value, permission))
                {
                    hasAllPermissions = false;
                    break;
                }
            }
        }
        else
        {
            hasAllPermissions = await HasAllTenantPermissionsAsync(permissions, tenantId);
        }

        if (!hasAllPermissions)
        {
            var context = resourceId.HasValue ? $"resource {resourceId}" : $"tenant {tenantId ?? TenantId}";
            var permissionsList = string.Join(", ", permissions.Select(p => p.ToString()));
            var message = $"User {UserId} lacks all required permissions ({permissionsList}) on {context}";

            _logger.LogWarning("Authorization failed: {Message}", message);
            throw new UnauthorizedAccessException(message);
        }
    }
}
