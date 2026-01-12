using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Tenants API Controller - RESTful API for tenant organization management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Tags("tenants")]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/tenants

    /// <summary>
    ///     Create a new tenant organization
    /// </summary>
    /// <param name="body">Tenant creation request containing name, slug, admin email, and optional description</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created tenant with unique identifier and basic information</returns>
    [HttpPost("v{version:apiVersion}/tenants")]
    [EndpointSummary("Create a new tenant organization")]
    [EndpointDescription("Creates a new tenant organization within the GameGuild platform.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(new CreateTenantCommand(body.Name, body.Slug, body.AdminEmail, body.Description), ct).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetTenantById), new { tenantId = id }, new { id, body.Name, body.Slug });
    }

    /// <summary>
    ///     Get tenants with pagination, search, and sorting
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of tenants per page (default: 20, max: 100)</param>
    /// <param name="status">Optional status filter: 'active' (active only), 'inactive' (inactive only), or null (all statuses)</param>
    /// <param name="searchTerm">Optional search term to filter tenants by name or slug</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of tenant organizations with metadata</returns>
    [HttpGet("v{version:apiVersion}/tenants")]
    [EndpointSummary("Get tenants with pagination, search, and sorting")]
    [EndpointDescription("Retrieves a paginated list of all tenant organizations accessible to the requesting user.")]
    [ProducesResponseType<Models.PagedResult<Tenant>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? searchTerm = null, CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Convert status filter to repository parameters
        bool? isActiveFilter = status?.ToLowerInvariant() switch
        {
            "active" => true,
            "inactive" => false,
            _ => null // null means include all statuses
        };

        var tenants = await sender.Send(
                new GetTenantsPageQuery(
                    page,
                    pageSize,
                    isActiveFilter != true, // Include inactive if not filtering for active only
                    true, // Always include archived for now since repository doesn't support this filter
                    searchTerm
                ),
                ct
            )
            .ConfigureAwait(false);

        return Ok(tenants);
    }

    /// <summary>
    ///     Get payment history for tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="startDate">Optional start date filter for payment history</param>
    /// <param name="endDate">Optional end date filter for payment history</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment history</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/payments")]
    [EndpointSummary("Get payment history for tenant")]
    [EndpointDescription("Retrieves payment history for a specific tenant with optional date filtering.")]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory(Guid tenantId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetPaymentHistoryQuery(null, tenantId, startDate, endDate), ct));
    }

    #endregion

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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkCreateTenantsCommand
        await Task.CompletedTask;

        return Created(string.Empty, new { message = "Bulk create tenants - not implemented yet" });
    }

    /// <summary>
    ///     Bulk partial update tenants
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants:update")]
    [EndpointSummary("Bulk partial update tenants")]
    [EndpointDescription("Updates multiple tenants with partial data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkPartialUpdateTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkUpdateTenantsCommand
        await Task.CompletedTask;

        return NoContent();
    }

    /// <summary>
    ///     Bulk full update tenants
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants:replace")]
    [EndpointSummary("Bulk full update tenants")]
    [EndpointDescription("Updates multiple tenants with complete data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkFullUpdateTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkUpdateTenantsCommand
        await Task.CompletedTask;

        return NoContent();
    }

    /// <summary>
    ///     Bulk soft delete tenants
    /// </summary>
    /// <param name="request">Bulk delete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants:delete")]
    [EndpointSummary("Bulk soft delete tenants")]
    [EndpointDescription("Soft deletes multiple tenants at once.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkDeleteTenantsCommand
        await Task.CompletedTask;

        return NoContent();
    }

    /// <summary>
    ///     Bulk activate tenant accounts
    /// </summary>
    /// <param name="request">Bulk activate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Activated tenant data</returns>
    [HttpPost("v{version:apiVersion}/tenants:activate")]
    [EndpointSummary("Bulk activate tenant accounts")]
    [EndpointDescription("Activates multiple tenant accounts at once.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkActivateTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkActivateTenantsCommand
        await Task.CompletedTask;

        return Ok(new { message = "Bulk activate tenants - not implemented yet" });
    }

    /// <summary>
    ///     Bulk deactivate tenant accounts
    /// </summary>
    /// <param name="request">Bulk deactivate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Deactivated tenant data</returns>
    [HttpPost("v{version:apiVersion}/tenants:deactivate")]
    [EndpointSummary("Bulk deactivate tenant accounts")]
    [EndpointDescription("Deactivates multiple tenant accounts at once.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeactivateTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkDeactivateTenantsCommand
        await Task.CompletedTask;

        return Ok(new { message = "Bulk deactivate tenants - not implemented yet" });
    }

    /// <summary>
    ///     Bulk archive tenant accounts
    /// </summary>
    /// <param name="request">Bulk archive request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Archived tenant data</returns>
    [HttpPost("v{version:apiVersion}/tenants:archive")]
    [EndpointSummary("Bulk archive tenant accounts")]
    [EndpointDescription("Archives multiple tenant accounts at once.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkArchiveTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkArchiveTenantsCommand
        await Task.CompletedTask;

        return Ok(new { message = "Bulk archive tenants - not implemented yet" });
    }

    /// <summary>
    ///     Bulk undelete soft-deleted tenants
    /// </summary>
    /// <param name="request">Bulk undelete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Restored tenant data</returns>
    [HttpPost("v{version:apiVersion}/tenants:undelete")]
    [EndpointSummary("Bulk undelete soft-deleted tenants")]
    [EndpointDescription("Restores multiple soft-deleted tenants at once.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUndeleteTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkUndeleteTenantsCommand
        await Task.CompletedTask;

        return Ok(new { message = "Bulk undelete tenants - not implemented yet" });
    }

    /// <summary>
    ///     Bulk hard delete tenants (irreversible purge)
    /// </summary>
    /// <param name="request">Bulk purge request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants:purge")]
    [EndpointSummary("Bulk hard delete tenants (irreversible purge)")]
    [EndpointDescription("Permanently deletes multiple tenants. Admin operation requiring proper authorization.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> BulkPurgeTenants([FromBody] object request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO: Implement BulkPurgeTenantsCommand
        await Task.CompletedTask;

        return NoContent();
    }

    #endregion

    #region Individual Item Operations - /v1/tenants/{tenantId}

    /// <summary>
    ///     Check if tenant exists by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("v{version:apiVersion}/tenants/{tenantId:guid}")]
    [EndpointSummary("Check if tenant exists by ID")]
    [EndpointDescription("Checks if a tenant exists by ID without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckTenantExistsById(Guid tenantId, CancellationToken ct)
    {
        var tenant = await sender.Send(new GetTenantByIdQuery(tenantId), ct).ConfigureAwait(false);

        return tenant is null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Get tenant by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tenant details</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}")]
    [EndpointSummary("Get tenant by ID")]
    [EndpointDescription("Retrieves detailed information for a specific tenant by their unique identifier.")]
    [ProducesResponseType<Tenant>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenantById(Guid tenantId, CancellationToken ct)
    {
        var tenant = await sender.Send(new GetTenantByIdQuery(tenantId), ct).ConfigureAwait(false);

        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>
    ///     Partially update tenant by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="body">Partial update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("v{version:apiVersion}/tenants/{tenantId:guid}")]
    [EndpointSummary("Partially update tenant by ID")]
    [EndpointDescription("Updates specific fields of a tenant by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchTenantById(Guid tenantId, [FromBody] UpdateTenantRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantCommand(tenantId, body.Name, body.Description), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Update tenant by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="body">Complete tenant data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("v{version:apiVersion}/tenants/{tenantId:guid}")]
    [EndpointSummary("Update tenant by ID")]
    [EndpointDescription("Fully updates a tenant by ID with complete tenant data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTenantById(Guid tenantId, [FromBody] UpdateTenantRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantCommand(tenantId, body.Name, body.Description), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Soft delete tenant by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="body">Archive request containing reason and optional metadata</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/tenants/{tenantId:guid}")]
    [EndpointSummary("Soft delete tenant by ID")]
    [EndpointDescription("Soft deletes a tenant by ID (can be restored).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenantById(Guid tenantId, [FromBody] ArchiveRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new ArchiveTenantCommand(tenantId, body.Reason), ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region Individual Tenant Actions - /v1/tenants/{tenantId}:action

    /// <summary>
    ///     Activate tenant account
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants/{tenantId:guid}:activate")]
    [EndpointSummary("Activate tenant account")]
    [EndpointDescription("Activates a tenant organization by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateTenant(Guid tenantId, CancellationToken ct)
    {
        await sender.Send(new ActivateTenantCommand(tenantId), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Deactivate tenant account
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants/{tenantId:guid}:deactivate")]
    [EndpointSummary("Deactivate tenant account")]
    [EndpointDescription("Deactivates a tenant organization by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateTenant(Guid tenantId, CancellationToken ct)
    {
        await sender.Send(new DeactivateTenantCommand(tenantId), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Archive (soft delete) tenant account
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="body">Archive request containing reason and optional metadata</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants/{tenantId:guid}:archive")]
    [EndpointSummary("Archive (soft delete) tenant account")]
    [EndpointDescription("Archives a tenant organization by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveTenant(Guid tenantId, [FromBody] ArchiveRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new ArchiveTenantCommand(tenantId, body.Reason), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Undelete a soft-deleted tenant account
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="body">Recovery request containing reason and optional restoration settings</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants/{tenantId:guid}:undelete")]
    [EndpointSummary("Undelete a soft-deleted tenant account")]
    [EndpointDescription("Undeletes a previously soft-deleted (archived) tenant organization.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UndeleteTenant(Guid tenantId, [FromBody] RecoverRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new RecoverTenantCommand(tenantId, body.Reason), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Permanently delete (hard delete) tenant account
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/tenants/{tenantId:guid}:purge")]
    [EndpointSummary("Permanently delete (hard delete) tenant account")]
    [EndpointDescription("Permanently and irreversibly deletes a tenant organization. Admin operation requiring proper authorization.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurgeTenant(Guid tenantId, CancellationToken ct)
    {
        await sender.Send(new DeleteTenantCommand(tenantId), ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion
}
