using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild.API.Authorization;

/// <summary>
///     Authorization filter that validates permissions using the RequiresPermissionAttribute.
///     Checks if the current user has all required permissions specified on the action/controller.
///     This filter is resolved via Dependency Injection using [ServiceFilter(typeof(PermissionAuthorizationFilter))].
/// </summary>
public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<PermissionAuthorizationFilter> _logger;

    private readonly IPermissionsContext _permissions;

    /// <summary>
    ///     Creates a new PermissionAuthorizationFilter with dependency injection
    /// </summary>
    /// <param name="permissions">Permission context service for checking user permissions</param>
    /// <param name="logger">Logger for authorization events</param>
    public PermissionAuthorizationFilter(IPermissionsContext permissions, ILogger<PermissionAuthorizationFilter> logger)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Called asynchronously before the authorization filter.
    ///     Validates that the user is authenticated and has all required permissions.
    /// </summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        // 0) Check if the endpoint has [AllowAnonymous] attribute
        var endpoint = httpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
        {
            _logger.LogDebug("Access granted: Endpoint has [AllowAnonymous] for {Path}", httpContext.Request.Path);
            return;
        }

        // Also check controller/action descriptors for [AllowAnonymous]
        if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var hasAllowAnonymous = controllerActionDescriptor.MethodInfo
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true)
                .Any();

            if (hasAllowAnonymous)
            {
                _logger.LogDebug("Access granted: Action has [AllowAnonymous] for {Path}", httpContext.Request.Path);
                return;
            }
        }

        // 1) Check basic authentication
        if (!_permissions.IsAuthenticated)
        {
            _logger.LogWarning("Authorization failed: User is not authenticated for {Path}", httpContext.Request.Path);

            context.Result = new ChallengeResult(); // Returns 401 Unauthorized

            return;
        }

        // 2) Discover required permissions from the action/controller
        var requiredPermissions = GetRequiredPermissions(context, httpContext);

        // 3) If no permissions are required, allow access
        if (requiredPermissions.Count == 0)
        {
            _logger.LogDebug("No permissions required for {Path}", httpContext.Request.Path);

            return;
        }

        // 4) System admins bypass permission checks
        if (_permissions.IsSystemAdmin)
        {
            _logger.LogDebug("Access granted: User is system admin for {Path}", httpContext.Request.Path);

            return;
        }

        // 5) Validate all required permissions
        foreach (var requiredPermission in requiredPermissions)
        {
            var hasPermission = await _permissions.HasTenantPermissionAsync(requiredPermission.Name);

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "Access denied: User {UserId} in tenant {TenantId} lacks permission '{Permission}' for {Path}",
                    _permissions.UserId,
                    _permissions.TenantId,
                    requiredPermission.Name,
                    httpContext.Request.Path
                );

                context.Result = new ForbidResult(); // Returns 403 Forbidden

                return;
            }

            _logger.LogDebug("Permission check passed: User {UserId} has '{Permission}' for {Path}", _permissions.UserId, requiredPermission.Name, httpContext.Request.Path);
        }

        _logger.LogInformation("Access granted: User {UserId} has all required permissions for {Path}", _permissions.UserId, httpContext.Request.Path);
    }

    /// <summary>
    ///     Extracts all RequiresPermissionAttribute instances from the action and controller.
    ///     Tries endpoint metadata first (minimal API/endpoint routing), then falls back to reflection.
    /// </summary>
    private List<RequiresPermissionAttribute> GetRequiredPermissions(AuthorizationFilterContext context, HttpContext httpContext)
    {
        // Try to get permissions from endpoint metadata (modern routing)
        var endpointPermissions = httpContext.GetEndpoint()?.Metadata?.GetOrderedMetadata<RequiresPermissionAttribute>();

        if (endpointPermissions != null && endpointPermissions.Any()) { return endpointPermissions.ToList(); }

        // Fallback to reflection for MVC controllers
        if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            // Get permissions from the action method
            var methodPermissions = controllerActionDescriptor.MethodInfo.GetCustomAttributes(typeof(RequiresPermissionAttribute), true).Cast<RequiresPermissionAttribute>();

            // Get permissions from the controller class
            var classPermissions = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(RequiresPermissionAttribute), true).Cast<RequiresPermissionAttribute>();

            // Combine both (method permissions override/add to class permissions)
            return methodPermissions.Concat(classPermissions).ToList();
        }

        return new List<RequiresPermissionAttribute>();
    }
}
