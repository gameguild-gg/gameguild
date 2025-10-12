using GameGuild.CQRS;
using GameGuild.Modules.Resources.Commands;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Resources.Controllers;

/// <summary>
/// REST API controller for managing resource usage and quotas using CQRS pattern
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ResourcesController(
    IQueryHandler<GetUsageRecordsQuery, Result<IEnumerable<ResourceUsageRecord>>> getUsageRecordsHandler,
    IQueryHandler<GetCurrentUsageSummaryQuery, Result<Dictionary<ResourceUsageType, long>>> getCurrentUsageSummaryHandler,
    IQueryHandler<CheckUsageLimitsQuery, Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>> checkUsageLimitsHandler,
    ICommandHandler<RecordUsageCommand, Result<ResourceUsageRecord>> recordUsageHandler
) : ControllerBase
{
    /// <summary>
    /// Get usage records for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="usageType">Optional usage type filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of usage records</returns>
    [HttpGet("tenant/{tenantId:guid}/usage-records")]
    public async Task<ActionResult<IEnumerable<ResourceUsageRecord>>> GetUsageRecords(
        Guid tenantId,
        [FromQuery] ResourceUsageType? usageType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetUsageRecordsQuery(tenantId, usageType, startDate, endDate);
        var result = await getUsageRecordsHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get current usage summary for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Dictionary of usage type to current count</returns>
    [HttpGet("tenant/{tenantId:guid}/usage-summary")]
    public async Task<ActionResult<Dictionary<ResourceUsageType, long>>> GetCurrentUsageSummary(Guid tenantId)
    {
        var query = new GetCurrentUsageSummaryQuery(tenantId);
        var result = await getCurrentUsageSummaryHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Check usage limits for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="usageType">Optional usage type filter</param>
    /// <returns>Dictionary of usage type to quota status</returns>
    [HttpGet("tenant/{tenantId:guid}/limits")]
    public async Task<ActionResult<Dictionary<ResourceUsageType, ResourceQuotaStatus>>> CheckLimits(
        Guid tenantId,
        [FromQuery] ResourceUsageType? usageType = null)
    {
        var query = new CheckUsageLimitsQuery(tenantId, usageType);
        var result = await checkUsageLimitsHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get usage by type across tenants
    /// </summary>
    /// <param name="usageType">Usage type to filter by</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <returns>Usage statistics by type</returns>
    [HttpGet("usage-by-type/{usageType}")]
    public async Task<ActionResult<IEnumerable<ResourceUsageRecord>>> UsageByType(
        ResourceUsageType usageType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var query = new GetUsageRecordsQuery(Guid.Empty, usageType, startDate, endDate);
        var result = await getUsageRecordsHandler.Handle(query, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Record usage for a tenant
    /// </summary>
    /// <param name="request">Usage recording request</param>
    /// <returns>Created usage record</returns>
    [HttpPost("record")]
    public async Task<ActionResult<ResourceUsageRecord>> RecordUsage([FromBody] RecordUsageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var command = new RecordUsageCommand(
            request.TenantId,
            request.UsageType,
            request.Count,
            request.Source,
            request.UserId,
            request.ResourceId,
            request.Metadata);

        var result = await recordUsageHandler.Handle(command, CancellationToken.None);

        if (!result.IsSuccess) return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetUsageRecords), new { tenantId = request.TenantId }, result.Value);
    }
}

/// <summary>
/// Request DTO for recording usage
/// </summary>
public record RecordUsageRequest(
    Guid TenantId,
    ResourceUsageType UsageType,
    long Count,
    string? Source = null,
    Guid? UserId = null,
    Guid? ResourceId = null,
    string? Metadata = null);
