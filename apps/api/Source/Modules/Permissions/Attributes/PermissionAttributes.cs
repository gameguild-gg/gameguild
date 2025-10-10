using GameGuild.Core.Domain.Identity;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Permissions.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PermissionLayer = GameGuild.Authorization.PermissionLayer;

namespace GameGuild.Modules.Permissions.Attributes;

/// <summary>
/// Enhanced permission attribute with context awareness and audit logging
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionWithContextAttribute : TypeFilterAttribute
{
    public RequirePermissionWithContextAttribute(
        PermissionType permission,
        PermissionLayer layer = PermissionLayer.Auto,
        string? resourceIdParameter = null,
        string? contentTypeParameter = null,
        bool requireOwnership = false,
        bool allowDelegation = true)
        : base(typeof(PermissionWithContextFilter))
    {
        Arguments = new object[] { permission, layer, resourceIdParameter ?? string.Empty, contentTypeParameter ?? string.Empty, requireOwnership, allowDelegation };
    }
}

/// <summary>
/// Filter that implements the enhanced permission checking with context
/// </summary>
public class PermissionWithContextFilter : IAsyncAuthorizationFilter
{
    private readonly ICachedPermissionService _permissionService;
    private readonly IPermissionsContext _permissionsContext;
    private readonly IPermissionAuditService _auditService;
    private readonly IPermissionDelegationService _delegationService;
    private readonly ILogger<PermissionWithContextFilter> _logger;

    private readonly PermissionType _requiredPermission;
    private readonly PermissionLayer _layer;
    private readonly string _resourceIdParameter;
    private readonly string _contentTypeParameter;
    private readonly bool _requireOwnership;
    private readonly bool _allowDelegation;

    public PermissionWithContextFilter(
        ICachedPermissionService permissionService,
        IPermissionsContext permissionsContext,
        IPermissionAuditService auditService,
        IPermissionDelegationService delegationService,
        ILogger<PermissionWithContextFilter> logger,
        PermissionType requiredPermission,
        PermissionLayer layer,
        string resourceIdParameter,
        string contentTypeParameter,
        bool requireOwnership,
        bool allowDelegation)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _permissionsContext = permissionsContext ?? throw new ArgumentNullException(nameof(permissionsContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _delegationService = delegationService ?? throw new ArgumentNullException(nameof(delegationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _requiredPermission = requiredPermission;
        _layer = layer;
        _resourceIdParameter = resourceIdParameter;
        _contentTypeParameter = contentTypeParameter;
        _requireOwnership = requireOwnership;
        _allowDelegation = allowDelegation;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = _permissionsContext.UserId;
        var tenantId = _permissionsContext.TenantId;

        if (!userId.HasValue)
        {
            await _auditService.LogPermissionDeniedAsync(
                null, tenantId, null, _requiredPermission, "User ID not found in context");
            context.Result = new UnauthorizedResult();
            return;
        }

        // Extract resource ID and content type from route/query parameters
        var resourceId = ExtractResourceId(context);
        var contentType = ExtractContentType(context);

        bool hasPermission = false;

        try
        {
            // Check permission based on layer
            hasPermission = _layer switch
            {
                PermissionLayer.Tenant => await _permissionService.HasTenantPermissionAsync(userId, tenantId, _requiredPermission),
                PermissionLayer.ContentType => !string.IsNullOrEmpty(contentType) && await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, _requiredPermission),
                PermissionLayer.Resource => resourceId.HasValue && await CheckResourcePermission(userId.Value, tenantId, resourceId.Value),
                PermissionLayer.Auto => await CheckPermissionWithAutoLayer(userId.Value, tenantId, resourceId, contentType),
                _ => false
            };

            // Check delegated permissions if allowed and direct permission failed
            if (!hasPermission && _allowDelegation)
            {
                hasPermission = await _delegationService.HasDelegatedPermissionAsync(
                    userId.Value, tenantId, resourceId, _requiredPermission);
            }

            // Additional ownership check if required
            if (hasPermission && _requireOwnership && resourceId.HasValue)
            {
                hasPermission = await CheckResourceOwnership(userId.Value, resourceId.Value);
            }

            // Log the permission check
            await _auditService.LogPermissionCheckAsync(
                userId, tenantId, resourceId, _requiredPermission, hasPermission, contentType);

            if (!hasPermission)
            {
                await _auditService.LogPermissionDeniedAsync(
                    userId, tenantId, resourceId, _requiredPermission,
                    "Insufficient permissions", contentType);

                context.Result = new ForbidResult();
                return;
            }

            // Record delegation usage if applicable
            if (_allowDelegation)
            {
                // This would need to be implemented to track which delegation was used
                // For now, we'll skip this to keep the example simple
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for User:{UserId} in Tenant:{TenantId}",
                _requiredPermission, userId, tenantId);

            await _auditService.LogPermissionDeniedAsync(
                userId, tenantId, resourceId, _requiredPermission,
                $"Permission check failed: {ex.Message}", contentType);

            context.Result = new StatusCodeResult(500);
        }
    }

    private Guid? ExtractResourceId(AuthorizationFilterContext context)
    {
        if (string.IsNullOrEmpty(_resourceIdParameter))
            return null;

        // Try route values first
        if (context.RouteData.Values.TryGetValue(_resourceIdParameter, out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeId))
        {
            return routeId;
        }

        // Try query parameters
        if (context.HttpContext.Request.Query.TryGetValue(_resourceIdParameter, out var queryValue)
            && Guid.TryParse(queryValue.FirstOrDefault(), out var queryId))
        {
            return queryId;
        }

        return null;
    }

    private string? ExtractContentType(AuthorizationFilterContext context)
    {
        if (string.IsNullOrEmpty(_contentTypeParameter))
            return null;

        // Try route values first
        if (context.RouteData.Values.TryGetValue(_contentTypeParameter, out var routeValue))
        {
            return routeValue?.ToString();
        }

        // Try query parameters
        if (context.HttpContext.Request.Query.TryGetValue(_contentTypeParameter, out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }

    private async Task<bool> CheckResourcePermission(Guid userId, Guid? tenantId, Guid resourceId)
    {
        // This is a simplified implementation
        // In practice, you'd need to determine the resource type and check accordingly
        // For now, we'll assume a generic resource permission check
        return await Task.FromResult(false); // Placeholder
    }

    private async Task<bool> CheckPermissionWithAutoLayer(Guid userId, Guid? tenantId, Guid? resourceId, string? contentType)
    {
        // Auto-detect the appropriate layer based on available context
        if (resourceId.HasValue)
        {
            return await CheckResourcePermission(userId, tenantId, resourceId.Value);
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            return await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, _requiredPermission);
        }

        return await _permissionService.HasTenantPermissionAsync(userId, tenantId, _requiredPermission);
    }

    private async Task<bool> CheckResourceOwnership(Guid userId, Guid resourceId)
    {
        // This would need to be implemented based on your resource ownership logic
        // For now, return true as a placeholder
        return await Task.FromResult(true);
    }
}

/// <summary>
/// Multiple permissions requirement attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAnyPermissionAttribute : TypeFilterAttribute
{
    public RequireAnyPermissionAttribute(params PermissionType[] permissions)
        : base(typeof(AnyPermissionFilter))
    {
        Arguments = new object[] { permissions };
    }
}

/// <summary>
/// Filter for checking any of multiple permissions
/// </summary>
public class AnyPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly ICachedPermissionService _permissionService;
    private readonly IPermissionsContext _permissionsContext;
    private readonly IPermissionAuditService _auditService;
    private readonly ILogger<AnyPermissionFilter> _logger;
    private readonly PermissionType[] _permissions;

    public AnyPermissionFilter(
        ICachedPermissionService permissionService,
        IPermissionsContext permissionsContext,
        IPermissionAuditService auditService,
        ILogger<AnyPermissionFilter> logger,
        PermissionType[] permissions)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _permissionsContext = permissionsContext ?? throw new ArgumentNullException(nameof(permissionsContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = _permissionsContext.UserId;
        var tenantId = _permissionsContext.TenantId;

        if (!userId.HasValue)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has any of the required permissions
        foreach (var permission in _permissions)
        {
            try
            {
                var hasPermission = await _permissionService.HasTenantPermissionAsync(userId, tenantId, permission);

                await _auditService.LogPermissionCheckAsync(
                    userId, tenantId, null, permission, hasPermission);

                if (hasPermission)
                {
                    return; // User has at least one required permission
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for User:{UserId}", permission, userId);
            }
        }

        // No permissions matched
        await _auditService.LogPermissionDeniedAsync(
            userId, tenantId, null, _permissions[0],
            $"User lacks any of required permissions: {string.Join(", ", _permissions)}");

        context.Result = new ForbidResult();
    }
}