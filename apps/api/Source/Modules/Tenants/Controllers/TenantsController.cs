using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace GameGuild.Modules.Tenants;

/// <summary> REST API controller for managing tenants using CQRS pattern </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class TenantsController(
    ICommandHandler<CreateTenantCommand, Result<Tenant>> createTenantHandler,
    ICommandHandler<UpdateTenantCommand, Result<Tenant>> updateTenantHandler,
    ICommandHandler<SoftDeleteTenantCommand, Result<bool>> deleteTenantHandler,
    ICommandHandler<RestoreTenantCommand, Result<bool>> restoreTenantHandler,
    ICommandHandler<HardDeleteTenantCommand, Result<bool>> hardDeleteTenantHandler,
    ICommandHandler<ActivateTenantCommand, Result<bool>> activateTenantHandler,
    ICommandHandler<DeactivateTenantCommand, Result<bool>> deactivateTenantHandler,
    IQueryHandler<GetTenantByIdQuery, Result<Tenant?>> getTenantByIdHandler,
    IQueryHandler<GetTenantByNameQuery, Result<Tenant?>> getTenantByNameHandler,
    IQueryHandler<GetTenantBySlugQuery, Result<Tenant?>> getTenantBySlugHandler,
    IQueryHandler<GetDeletedTenantsQuery, Result<IEnumerable<Tenant>>> getDeletedTenantsHandler,
    IQueryHandler<GetActiveTenantsQuery, Result<IEnumerable<Tenant>>> getActiveTenantsHandler
) : ControllerBase
{
    /// <summary> Get a specific tenant by ID </summary>
    /// <param name="id"> Tenant ID </param>
    /// <param name="includeDeleted"> Include soft-deleted tenants </param>
    /// <returns> Tenant details </returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Tenant>> GetTenantById(Guid id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetTenantByIdQuery(id, includeDeleted);
        var result = await getTenantByIdHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary> Get a tenant by name </summary>
    /// <param name="name"> Tenant name </param>
    /// <param name="includeDeleted"> Include soft-deleted tenants </param>
    /// <returns> Tenant details </returns>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<Tenant>> GetTenantByName(string name, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetTenantByNameQuery(name, includeDeleted);
        var result = await getTenantByNameHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary> Get a tenant by slug </summary>
    /// <param name="slug"> Tenant slug </param>
    /// <param name="includeDeleted"> Include soft-deleted tenants </param>
    /// <returns> Tenant details </returns>
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<Tenant>> GetTenantBySlug(string slug, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetTenantBySlugQuery(slug, includeDeleted);
        var result = await getTenantBySlugHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary> Get deleted tenants </summary>
    /// <returns> List of deleted tenants </returns>
    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetDeletedTenants()
    {
        var query = new GetDeletedTenantsQuery();
        var result = await getDeletedTenantsHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary> Get active tenants </summary>
    /// <returns> List of active tenants </returns>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetActiveTenants()
    {
        var query = new GetActiveTenantsQuery();
        var result = await getActiveTenantsHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // /// <summary> Search tenants with advanced filtering </summary>
    // /// <param name="searchTerm"> Search term </param>
    // /// <param name="isActive"> Filter by active status </param>
    // /// <param name="includeDeleted"> Include deleted tenants </param>
    // /// <param name="sortBy"> Sort field </param>
    // /// <param name="sortDescending"> Sort descending </param>
    // /// <param name="limit"> Limit results </param>
    // /// <param name="offset"> Offset results </param>
    // /// <returns> Filtered tenants </returns>
    // [HttpGet("search")]
    // public async Task<ActionResult<IEnumerable<Tenant>>> SearchTenants(
    //     [FromQuery] string? searchTerm = null,
    //     [FromQuery] bool? isActive = null,
    //     [FromQuery] bool includeDeleted = false,
    //     [FromQuery] TenantSortField sortBy = TenantSortField.Name,
    //     [FromQuery] bool sortDescending = false,
    //     [FromQuery] int? limit = null,
    //     [FromQuery] int? offset = null
    // )
    // {
    //     var query = new SearchTenantsQuery(searchTerm, isActive, includeDeleted, sortBy, sortDescending, limit, offset);
    //     var result = await searchTenantsHandler.Handle(query, CancellationToken.None);
    // 
    //     if (!result.IsSuccess) return BadRequest(result.Error);
    // 
    //     return Ok(result.Value);
    // }

    /// <summary> Create a new tenant </summary>
    /// <param name="request"> Tenant creation DTO </param>
    /// <returns> Created tenant </returns>
    [HttpPost]
    // [RequireTenantPermission(PermissionType.Create)]
    public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var command = new CreateTenantCommand(request.Name, request.Description, request.IsActive, request.Slug);
        var result = await createTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetTenantById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary> Update an existing tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <param name="request"> Tenant update DTO </param>
    /// <returns> Updated tenant </returns>
    [HttpPut("{id:guid}")]
    // [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<Tenant>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var command = new UpdateTenantCommand(id, request.Name, request.Description, request.IsActive, request.Slug);
        var result = await updateTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary> Soft delete a tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <returns> Deletion result </returns>
    [HttpDelete("{id:guid}")]
    // [RequireTenantPermission(PermissionType.Delete)]
    public async Task<ActionResult> SoftDeleteTenant(Guid id)
    {
        var command = new SoftDeleteTenantCommand(id);
        var result = await deleteTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary> Restore a soft-deleted tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <returns> Restoration result </returns>
    [HttpPost("{id:guid}/restore")]
    // [RequireTenantPermission(PermissionType.Restore)]
    public async Task<ActionResult> RestoreTenant(Guid id)
    {
        var command = new RestoreTenantCommand(id);
        var result = await restoreTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary> Permanently delete a tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <returns> Deletion result </returns>
    [HttpDelete("{id:guid}/permanent")]
    // [RequireTenantPermission(PermissionType.HardDelete)]
    public async Task<ActionResult> HardDeleteTenant(Guid id)
    {
        var command = new HardDeleteTenantCommand(id);
        var result = await hardDeleteTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary> Activate a tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <returns> Activation result </returns>
    [HttpPost("{id:guid}/activate")]
    // [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult> ActivateTenant(Guid id)
    {
        var command = new ActivateTenantCommand(id);
        var result = await activateTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary> Deactivate a tenant </summary>
    /// <param name="id"> Tenant ID </param>
    /// <returns> Deactivation result </returns>
    [HttpPost("{id:guid}/deactivate")]
    // [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult> DeactivateTenant(Guid id)
    {
        var command = new DeactivateTenantCommand(id);
        var result = await deactivateTenantHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return NoContent();
    }
}
