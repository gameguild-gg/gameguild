using GameGuild.Common;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// REST API controller for managing tenants using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController(
  ICommandHandler<CreateTenantCommand, Common.Result<Tenant>> createTenantHandler,
  ICommandHandler<UpdateTenantCommand, Common.Result<Tenant>> updateTenantHandler,
  ICommandHandler<DeleteTenantCommand, Common.Result<bool>> deleteTenantHandler,
  ICommandHandler<RestoreTenantCommand, Common.Result<bool>> restoreTenantHandler,
  ICommandHandler<HardDeleteTenantCommand, Common.Result<bool>> hardDeleteTenantHandler,
  ICommandHandler<ActivateTenantCommand, Common.Result<bool>> activateTenantHandler,
  ICommandHandler<DeactivateTenantCommand, Common.Result<bool>> deactivateTenantHandler,
  IQueryHandler<SearchTenantsQuery, Common.Result<IEnumerable<Tenant>>> searchTenantsHandler,
  ICommandHandler<BulkDeleteTenantsCommand, Common.Result<int>> bulkDeleteTenantsHandler,
  ICommandHandler<BulkRestoreTenantsCommand, Common.Result<int>> bulkRestoreTenantsHandler,
  IQueryHandler<GetAllTenantsQuery, Common.Result<IEnumerable<Tenant>>> getAllTenantsHandler,
  IQueryHandler<GetTenantByIdQuery, Common.Result<Tenant?>> getTenantByIdHandler,
  IQueryHandler<GetTenantByNameQuery, Common.Result<Tenant?>> getTenantByNameHandler,
  IQueryHandler<GetTenantBySlugQuery, Common.Result<Tenant?>> getTenantBySlugHandler,
  IQueryHandler<GetDeletedTenantsQuery, Common.Result<IEnumerable<Tenant>>> getDeletedTenantsHandler,
  IQueryHandler<GetActiveTenantsQuery, Common.Result<IEnumerable<Tenant>>> getActiveTenantsHandler,
  IQueryHandler<GetTenantStatisticsQuery, Common.Result<TenantStatistics>> getTenantStatisticsHandler
) : ControllerBase {
  /// <summary>
  /// Get all tenants
  /// </summary>
  /// <param name="includeDeleted">Include soft-deleted tenants</param>
  /// <returns>List of tenants</returns>
  [HttpGet]
  public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants([FromQuery] bool includeDeleted = false) {
    var query = new GetAllTenantsQuery(includeDeleted);
    var result = await getAllTenantsHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get a specific tenant by ID
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <param name="includeDeleted">Include soft-deleted tenants</param>
  /// <returns>Tenant details</returns>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<Tenant>> GetTenantById(Guid id, [FromQuery] bool includeDeleted = false) {
    var query = new GetTenantByIdQuery(id, includeDeleted);
    var result = await getTenantByIdHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get a tenant by name
  /// </summary>
  /// <param name="name">Tenant name</param>
  /// <param name="includeDeleted">Include soft-deleted tenants</param>
  /// <returns>Tenant details</returns>
  [HttpGet("by-name/{name}")]
  public async Task<ActionResult<Tenant>> GetTenantByName(string name, [FromQuery] bool includeDeleted = false) {
    var query = new GetTenantByNameQuery(name, includeDeleted);
    var result = await getTenantByNameHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get a tenant by slug
  /// </summary>
  /// <param name="slug">Tenant slug</param>
  /// <param name="includeDeleted">Include soft-deleted tenants</param>
  /// <returns>Tenant details</returns>
  [HttpGet("by-slug/{slug}")]
  public async Task<ActionResult<Tenant>> GetTenantBySlug(string slug, [FromQuery] bool includeDeleted = false) {
    var query = new GetTenantBySlugQuery(slug, includeDeleted);
    var result = await getTenantBySlugHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get deleted tenants
  /// </summary>
  /// <returns>List of deleted tenants</returns>
  [HttpGet("deleted")]
  public async Task<ActionResult<IEnumerable<Tenant>>> GetDeletedTenants() {
    var query = new GetDeletedTenantsQuery();
    var result = await getDeletedTenantsHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get active tenants
  /// </summary>
  /// <returns>List of active tenants</returns>
  [HttpGet("active")]
  public async Task<ActionResult<IEnumerable<Tenant>>> GetActiveTenants() {
    var query = new GetActiveTenantsQuery();
    var result = await getActiveTenantsHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Search tenants with advanced filtering
  /// </summary>
  /// <param name="searchTerm">Search term</param>
  /// <param name="isActive">Filter by active status</param>
  /// <param name="includeDeleted">Include deleted tenants</param>
  /// <param name="sortBy">Sort field</param>
  /// <param name="sortDescending">Sort descending</param>
  /// <param name="limit">Limit results</param>
  /// <param name="offset">Offset results</param>
  /// <returns>Filtered tenants</returns>
  [HttpGet("search")]
  public async Task<ActionResult<IEnumerable<Tenant>>> SearchTenants(
    [FromQuery] string? searchTerm = null,
    [FromQuery] bool? isActive = null,
    [FromQuery] bool includeDeleted = false,
    [FromQuery] TenantSortField sortBy = TenantSortField.Name,
    [FromQuery] bool sortDescending = false,
    [FromQuery] int? limit = null,
    [FromQuery] int? offset = null
  ) {
    var query = new SearchTenantsQuery(searchTerm, isActive, includeDeleted, sortBy, sortDescending, limit, offset);
    var result = await searchTenantsHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Get tenant statistics
  /// </summary>
  /// <returns>Tenant statistics</returns>
  [HttpGet("statistics")]
  public async Task<ActionResult<TenantStatistics>> GetTenantStatistics() {
    var query = new GetTenantStatisticsQuery();
    var result = await getTenantStatisticsHandler.Handle(query, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Create a new tenant
  /// </summary>
  /// <param name="dto">Tenant creation DTO</param>
  /// <returns>Created tenant</returns>
  [HttpPost]
  [RequireTenantPermission(PermissionType.Create)]
  public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantDto dto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new CreateTenantCommand(dto.Name, dto.Description, dto.IsActive, dto.Slug);
    var result = await createTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return CreatedAtAction(nameof(GetTenantById), new { id = result.Value.Id }, result.Value);
  }

  /// <summary>
  /// Update an existing tenant
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <param name="dto">Tenant update DTO</param>
  /// <returns>Updated tenant</returns>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<Tenant>> UpdateTenant(Guid id, [FromBody] UpdateTenantDto dto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new UpdateTenantCommand(id, dto.Name, dto.Description, dto.IsActive, dto.Slug);
    var result = await updateTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Soft delete a tenant
  /// </summary>
  /// <param name="id">Tenant ID to delete</param>
  /// <returns>No content if successful</returns>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> DeleteTenant(Guid id) {
    var command = new DeleteTenantCommand(id);
    var result = await deleteTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Restore a soft-deleted tenant
  /// </summary>
  /// <param name="id">Tenant ID to restore</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id:guid}/restore")]
  public async Task<IActionResult> RestoreTenant(Guid id) {
    var command = new RestoreTenantCommand(id);
    var result = await restoreTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Permanently delete a tenant
  /// </summary>
  /// <param name="id">Tenant ID to delete permanently</param>
  /// <returns>No content if successful</returns>
  [HttpDelete("{id:guid}/permanent")]
  public async Task<IActionResult> HardDeleteTenant(Guid id) {
    var command = new HardDeleteTenantCommand(id);
    var result = await hardDeleteTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Activate a tenant
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id:guid}/activate")]
  public async Task<IActionResult> ActivateTenant(Guid id) {
    var command = new ActivateTenantCommand(id);
    var result = await activateTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Deactivate a tenant
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id:guid}/deactivate")]
  public async Task<IActionResult> DeactivateTenant(Guid id) {
    var command = new DeactivateTenantCommand(id);
    var result = await deactivateTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Bulk delete multiple tenants
  /// </summary>
  /// <param name="dto">Bulk delete DTO</param>
  /// <returns>Number of deleted tenants</returns>
  [HttpPost("bulk-delete")]
  public async Task<ActionResult<int>> BulkDeleteTenants([FromBody] BulkDeleteTenantsDto dto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new BulkDeleteTenantsCommand(dto.TenantIds);
    var result = await bulkDeleteTenantsHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }

  /// <summary>
  /// Bulk restore multiple tenants
  /// </summary>
  /// <param name="dto">Bulk restore DTO</param>
  /// <returns>Number of restored tenants</returns>
  [HttpPost("bulk-restore")]
  public async Task<ActionResult<int>> BulkRestoreTenants([FromBody] BulkRestoreTenantsDto dto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new BulkRestoreTenantsCommand(dto.TenantIds);
    var result = await bulkRestoreTenantsHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) return BadRequest(result.Error);

    return Ok(result.Value);
  }
}
