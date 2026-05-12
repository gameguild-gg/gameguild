using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Tenant Bulk Operations API Controller - handles batch operations on multiple tenants
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("tenants")]
[Authorize]
public sealed class TenantBulkOperationsController(ISender sender) : BaseApiController
{
    #region Bulk Operations - /v1/tenants:action

    /// <summary>
    ///     Bulk create tenants
    /// </summary>
    /// <param name="request">Bulk create request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created tenant data</returns>
    [HttpPost("v{version:apiVersion}/tenants:create")]
    [EndpointSummary("Bulk create tenants")]
    [EndpointDescription("Creates multiple tenant organizations at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateTenants([FromBody] BulkCreateTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    ///     Bulk partial update tenants
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:update")]
    [EndpointSummary("Bulk partial update tenants")]
    [EndpointDescription("Updates multiple tenants with partial data.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkPartialUpdateTenants([FromBody] BulkUpdateTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk full update tenants
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:replace")]
    [EndpointSummary("Bulk full update tenants")]
    [EndpointDescription("Updates multiple tenants with complete data.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkFullUpdateTenants([FromBody] BulkUpdateTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk soft delete tenants
    /// </summary>
    /// <param name="request">Bulk delete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:delete")]
    [EndpointSummary("Bulk soft delete tenants")]
    [EndpointDescription("Soft deletes multiple tenants at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteTenants([FromBody] BulkDeleteTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk activate tenant accounts
    /// </summary>
    /// <param name="request">Bulk activate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:activate")]
    [EndpointSummary("Bulk activate tenant accounts")]
    [EndpointDescription("Activates multiple tenant accounts at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkActivateTenants([FromBody] BulkActivateTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk deactivate tenant accounts
    /// </summary>
    /// <param name="request">Bulk deactivate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:deactivate")]
    [EndpointSummary("Bulk deactivate tenant accounts")]
    [EndpointDescription("Deactivates multiple tenant accounts at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeactivateTenants([FromBody] BulkDeactivateTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk archive tenant accounts
    /// </summary>
    /// <param name="request">Bulk archive request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:archive")]
    [EndpointSummary("Bulk archive tenant accounts")]
    [EndpointDescription("Archives multiple tenant accounts at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkArchiveTenants([FromBody] BulkArchiveTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk undelete soft-deleted tenants
    /// </summary>
    /// <param name="request">Bulk undelete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:undelete")]
    [EndpointSummary("Bulk undelete soft-deleted tenants")]
    [EndpointDescription("Restores multiple soft-deleted tenants at once.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUndeleteTenants([FromBody] BulkUndeleteTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk hard delete tenants (irreversible purge)
    /// </summary>
    /// <param name="request">Bulk purge request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    [HttpPost("v{version:apiVersion}/tenants:purge")]
    [EndpointSummary("Bulk hard delete tenants (irreversible purge)")]
    [EndpointDescription("Permanently deletes multiple tenants. Admin operation requiring proper authorization.")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> BulkPurgeTenants([FromBody] BulkPurgeTenantsCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(request, ct).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion
}
