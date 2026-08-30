using System.Security.Claims;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Handles permission requirements using RBAC (Role-Based Access Control).
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
                CancellationToken.None).ConfigureAwait(false);

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
            throw;
        }
    }

    private bool TryGetUserAndTenantIds(ClaimsPrincipal user, out Guid userId, out Guid tenantId)
    {
        userId = Guid.Empty;
        tenantId = Guid.Empty;

        // SECURITY: Fail-closed if user ID cannot be parsed
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
        {
            _logger.LogWarning("Failed to parse user ID claim - fail-closed");
            return false;
        }
        
        // SECURITY: Reject Guid.Empty as valid user ID
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("User ID is Guid.Empty - fail-closed");
            return false;
        }

        // Try tenant context first (strongly-typed Guid?)
        if (_tenantContext.HasTenant && _tenantContext.TenantId.HasValue)
        {
            tenantId = _tenantContext.TenantId.Value;
            return true;
        }

        // Fallback to claims
        var tenantClaim = user.FindFirstValue(_tokenOptions.TenantClaimType);
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenantId))
        {
            // SECURITY: Reject Guid.Empty as valid tenant ID
            if (parsedTenantId == Guid.Empty)
            {
                _logger.LogWarning("Tenant ID claim is Guid.Empty - fail-closed");
                return false;
            }
            tenantId = parsedTenantId;
            return true;
        }

        // SECURITY: Fail-closed if no valid tenant context
        _logger.LogWarning("No valid tenant context available - fail-closed");
        return false;
    }
}
