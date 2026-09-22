using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

// SECURITY: only the guarded Authorization-module command types exist for these operations.
using GrantTenantPermissionCommand = GameGuild.Identity.Authorization.GrantTenantPermissionCommand;
using RevokeTenantPermissionCommand = GameGuild.Identity.Authorization.RevokeTenantPermissionCommand;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     API controller for permission grant CRUD operations across all three permission layers:
///     tenant-level, content-type-level, and resource-level grants.
///     Supports create, delete, revoke, and batch operations.
/// </summary>
/// <remarks>
///     Rate limited to 100 requests per minute per client to prevent DoS attacks on permission evaluation.
///     Mutating commands are the guarded Authorization-module types; their handlers enforce
///     tenant-admin / system-admin / global-defaults authorization and audit every change.
/// </remarks>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/permissions")]
[Microsoft.AspNetCore.Http.Tags("auth/permissions")]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableRateLimiting(RateLimitPolicies.Authorization)]
[Authorize]
public class PermissionGrantsController(
    IMediator mediator,
    IActorContextAccessor actorContextAccessor,
    ILogger<PermissionGrantsController> logger) : BaseApiController
{
    private readonly ILogger<PermissionGrantsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly IActorContextAccessor _actorContextAccessor =
        actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));

    private ActorContext Actor => _actorContextAccessor.ActorContext;

    #region Tenant Permission Grants

    /// <summary>
    ///     Create a tenant permission grant for a user
    /// </summary>
    [HttpPost("tenant-grants")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenantGrant([FromBody] GrantTenantPermissionRequest request)
    {
        // The acting user comes from the authenticated actor context — never the request body.
        var command = new GrantTenantPermissionCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Permissions = request.Permissions,
            GrantedBy = Actor.SubjectIdAsGuid ?? throw new UnauthorizedAccessException("Authenticated actor required"),
            ExpiresAt = request.ExpiresAt,
            Reason = request.Reason
        };

        var permissionId = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(PermissionEvaluationController.GetTenantPermissions),
            "PermissionEvaluation",
            new { userId = request.UserId, tenantId = request.TenantId },
            new { permissionId });
    }

    /// <summary>
    ///     Delete a tenant permission grant
    /// </summary>
    [HttpDelete("tenant-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTenantGrant(Guid grantId)
    {
        var command = new RevokeTenantPermissionByIdCommand { GrantId = grantId };
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Revoke tenant permissions from a user (legacy - use DELETE /tenant-grants/{grantId} instead)
    /// </summary>
    [HttpPost("tenant-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeTenantPermission([FromBody] RevokeTenantPermissionRequest request)
    {
        var command = new RevokeTenantPermissionCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Permissions = request.Permissions,
            RevokedBy = Actor.SubjectIdAsGuid ?? throw new UnauthorizedAccessException("Authenticated actor required"),
            Reason = request.Reason
        };

        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Batch create tenant permission grants
    /// </summary>
    [HttpPost("tenant-grants:batch-create")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchCreateTenantGrants([FromBody] BulkGrantTenantPermissionsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Batch delete tenant permission grants
    /// </summary>
    [HttpPost("tenant-grants:batch-delete")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchDeleteTenantGrants([FromBody] BulkRevokeTenantPermissionsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Content Type Permission Grants

    /// <summary>
    ///     Create a content type permission grant for a user
    /// </summary>
    [HttpPost("content-type-grants")]
    [ProducesResponseType(typeof(ContentTypePermission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContentTypePermission>> CreateContentTypeGrant([FromBody] GrantContentTypePermissionCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(PermissionEvaluationController.GetContentTypePermissions),
            "PermissionEvaluation",
            new { userId = command.UserId },
            result);
    }

    /// <summary>
    ///     Delete a content type permission grant
    /// </summary>
    [HttpDelete("content-type-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteContentTypeGrant(Guid grantId)
    {
        var command = new RevokeContentTypePermissionByIdCommand { GrantId = grantId };
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Revoke content type permissions (legacy - use DELETE /content-type-grants/{grantId} instead)
    /// </summary>
    [HttpPost("content-type-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeContentTypePermission([FromBody] RevokeContentTypePermissionCommand command)
    {
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region Resource Permission Grants

    /// <summary>
    ///     Create a resource permission grant for a user
    /// </summary>
    [HttpPost("resource-grants")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CreateResourceGrant([FromBody] GrantResourcePermissionCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(PermissionEvaluationController.GetResourcePermissions),
            "PermissionEvaluation",
            new { userId = command.UserId, resourceId = command.ResourceId },
            result);
    }

    /// <summary>
    ///     Delete a resource permission grant
    /// </summary>
    [HttpDelete("resource-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteResourceGrant(Guid grantId)
    {
        var command = new RevokeResourcePermissionByIdCommand { GrantId = grantId };
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Revoke resource permissions (legacy - use DELETE /resource-grants/{grantId} instead)
    /// </summary>
    [HttpPost("resource-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeResourcePermission([FromBody] RevokeResourcePermissionCommand command)
    {
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Batch create resource permission grants
    /// </summary>
    [HttpPost("resource-grants:batch-create")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchCreateResourceGrants([FromBody] BulkGrantResourcePermissionsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion
}

/// <summary>
///     Request body for creating a tenant permission grant.
///     The acting user is taken from the authenticated actor context, never the body.
/// </summary>
public sealed record GrantTenantPermissionRequest(
    Guid UserId,
    Guid TenantId,
    string[] Permissions,
    DateTime? ExpiresAt = null,
    string? Reason = null);

/// <summary>
///     Request body for revoking tenant permissions.
///     The acting user is taken from the authenticated actor context, never the body.
/// </summary>
public sealed record RevokeTenantPermissionRequest(
    Guid UserId,
    Guid TenantId,
    string[] Permissions,
    string? Reason = null);
