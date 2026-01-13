using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Controller for managing tenant-level permissions.
///     Provides REST endpoints for granting, revoking, and querying tenant permissions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/authorization/tenants")]
[Authorize]
[Produces("application/json")]
public sealed class TenantPermissionsController(ISender sender, ILogger<TenantPermissionsController> logger) : ControllerBase
{
    /// <summary>
    ///     Grants tenant-level permissions to a user.
    /// </summary>
    /// <param name="command">The grant permission command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created permission record.</returns>
    /// <response code="200">Permissions granted successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have permission to grant tenant permissions.</response>
    [HttpPost("grant")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GrantPermission([FromBody] GrantTenantPermissionCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting tenant permissions {Permissions} to user {UserId} in tenant {TenantId}",
            string.Join(", ", command.Permissions),
            command.UserId,
            command.TenantId);

        var permissionId = await sender.Send(command, cancellationToken);

        return Ok(new { PermissionId = permissionId });
    }

    /// <summary>
    ///     Revokes tenant-level permissions from a user.
    /// </summary>
    /// <param name="command">The revoke permission command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success/failure.</returns>
    /// <response code="200">Permissions revoked successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have permission to revoke tenant permissions.</response>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokePermission([FromBody] RevokeTenantPermissionCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Revoking tenant permissions {Permissions} from user {UserId} in tenant {TenantId}",
            string.Join(", ", command.Permissions),
            command.UserId,
            command.TenantId);

        var success = await sender.Send(command, cancellationToken);

        return Ok(new { Success = success });
    }

    /// <summary>
    ///     Gets all tenant-level permissions for a user.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">Optional user ID (defaults to current user).</param>
    /// <param name="includeEffective">Whether to include effective permissions from roles/groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tenant permissions.</returns>
    /// <response code="200">Permissions retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{tenantId}/permissions")]
    [ProducesResponseType(typeof(GetTenantPermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPermissions(
        [FromRoute] Guid tenantId,
        [FromQuery] Guid? userId,
        [FromQuery] bool includeEffective = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantPermissionsQuery
        {
            TenantId = new TenantId(tenantId),
            UserId = userId,
            IncludeEffective = includeEffective
        };

        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Checks if a user has a specific tenant-level permission.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="userId">Optional user ID (defaults to current user).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating if the user has the permission.</returns>
    /// <response code="200">Permission check completed.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{tenantId}/has-permission")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HasPermission(
        [FromRoute] Guid tenantId,
        [FromQuery] string permission,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantPermissionsQuery
        {
            TenantId = new TenantId(tenantId),
            UserId = userId,
            IncludeEffective = true
        };

        var result = await sender.Send(query, cancellationToken);

        var hasPermission = result.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

        return Ok(new { HasPermission = hasPermission, Permission = permission });
    }

    // ========================================================================
    // GLOBAL/TENANT DEFAULT PERMISSIONS ENDPOINTS
    // ========================================================================

    /// <summary>
    ///     Sets global default permissions applied to all users.
    /// </summary>
    /// <param name="command">The set global defaults command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Global defaults set successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have system:manage-global-defaults permission.</response>
    [HttpPost("global/defaults")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetGlobalDefaults(
        [FromBody] SetGlobalDefaultPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Setting global default permissions: {Permissions}",
            string.Join(", ", command.Permissions));

        var success = await sender.Send(command, cancellationToken);

        return Ok(new { Success = success });
    }

    /// <summary>
    ///     Sets tenant default permissions applied to all users in a tenant.
    /// </summary>
    /// <param name="command">The set tenant defaults command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Tenant defaults set successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have tenant admin privileges.</response>
    [HttpPost("defaults")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetTenantDefaults(
        [FromBody] SetTenantDefaultPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Setting tenant {TenantId} default permissions: {Permissions}",
            command.TenantId,
            string.Join(", ", command.Permissions));

        var success = await sender.Send(command, cancellationToken);

        return Ok(new { Success = success });
    }

    // ========================================================================
    // DENY PERMISSIONS ENDPOINTS
    // ========================================================================

    /// <summary>
    ///     Denies tenant-level permissions for a user (DENY-WINS).
    /// </summary>
    /// <param name="command">The deny permission command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the updated permission record.</returns>
    /// <response code="200">Permissions denied successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have permission to deny tenant permissions.</response>
    [HttpPost("deny")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DenyPermission(
        [FromBody] DenyTenantPermissionCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Denying tenant permissions {Permissions} for user {UserId} in tenant {TenantId}",
            string.Join(", ", command.Permissions),
            command.UserId,
            command.TenantId);

        var permissionId = await sender.Send(command, cancellationToken);

        return Ok(new { PermissionId = permissionId });
    }

    /// <summary>
    ///     Removes deny entries from a user's permissions.
    /// </summary>
    /// <param name="command">The remove deny permissions command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success/failure.</returns>
    /// <response code="200">Deny entries removed successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User doesn't have permission to modify deny permissions.</response>
    [HttpPost("deny/remove")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveDenyPermission(
        [FromBody] RemoveDenyPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Removing deny permissions {Permissions} from user {UserId} in tenant {TenantId}",
            string.Join(", ", command.Permissions),
            command.UserId,
            command.TenantId);

        var success = await sender.Send(command, cancellationToken);

        return Ok(new { Success = success });
    }
}
