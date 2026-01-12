using System.Security.Claims;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Handles permission requirements using RBAC (Role-Based Access Control).
///     Checks token claims first, then falls back to database lookup if needed.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAuthorizationTenantContext _tenantContext;
    private readonly IAuthorizationPermissionService _permissionService;
    private readonly AuthorizationTokenOptions _tokenOptions;
    private readonly ILogger<PermissionHandler> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="PermissionHandler"/>.
    /// </summary>
    public PermissionHandler(
        IAuthorizationTenantContext tenantContext,
        IAuthorizationPermissionService permissionService,
        IOptions<AuthorizationTokenOptions> tokenOptions,
        ILogger<PermissionHandler> logger)
    {
        _tenantContext = tenantContext;
        _permissionService = permissionService;
        _tokenOptions = tokenOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check claims-based permissions first (if allowed)
        if (requirement.AllowClaimsBased && HasPermissionInClaims(context.User, requirement.Permission))
        {
            _logger.LogDebug("Permission '{Permission}' granted via token claims", requirement.Permission);
            context.Succeed(requirement);
            return;
        }

        // Fall back to database lookup
        if (!TryGetUserAndTenantIds(context.User, out var userId, out var tenantId))
        {
            _logger.LogWarning("Cannot determine user/tenant for permission check");
            context.Fail(new AuthorizationFailureReason(this, "User or tenant context unavailable"));
            return;
        }

        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(
                userId,
                tenantId,
                requirement.Permission,
                CancellationToken.None);

            if (hasPermission)
            {
                _logger.LogDebug("Permission '{Permission}' granted via database lookup", requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Permission denied: user {UserId} lacks '{Permission}' in tenant {TenantId}",
                    userId,
                    requirement.Permission,
                    tenantId);
                context.Fail(new AuthorizationFailureReason(this, $"Missing permission: {requirement.Permission}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission '{Permission}'", requirement.Permission);
            context.Fail(new AuthorizationFailureReason(this, "Error checking permissions"));
        }
    }

    private bool HasPermissionInClaims(ClaimsPrincipal user, string permission)
    {
        var permissionClaims = user.FindAll(_tokenOptions.PermissionClaimType);
        return permissionClaims.Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetUserAndTenantIds(ClaimsPrincipal user, out Guid userId, out Guid tenantId)
    {
        userId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
            return false;

        if (_tenantContext.HasTenant && Guid.TryParse(_tenantContext.TenantId, out tenantId))
            return true;

        var tenantClaim = user.FindFirstValue(_tokenOptions.TenantClaimType);
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out tenantId))
            return true;

        return false;
    }
}
