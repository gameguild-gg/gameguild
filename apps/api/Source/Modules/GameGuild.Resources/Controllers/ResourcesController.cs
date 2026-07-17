using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Resources;

/// <summary>
///     Resources API Controller - RESTful API for global resource management and administration
/// </summary>
/// <remarks>
///     This controller exposes cross-tenant aggregates and should only be accessible to system administrators.
/// </remarks>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("resources")]
[Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
[EnableRateLimiting(RateLimitPolicies.Internal)]
public sealed class ResourcesController(ISender sender) : BaseApiController
{
    #region Collection Operations - /v1/resources

    /// <summary>
    ///     Get resource usage filtered by type
    /// </summary>
    /// <param name="type">Resource usage type to filter by (required)</param>
    /// <param name="startDate">Start date for the query range</param>
    /// <param name="endDate">End date for the query range</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated usage data for the specified type</returns>
    [HttpGet("v{version:apiVersion}/resources/usage")]
    [EndpointSummary("Get resource usage by type")]
    [EndpointDescription("Retrieves aggregated resource usage across all tenants within the specified date range for the given resource type.")]
    [ProducesResponseType<Dictionary<Guid, int>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsage([FromQuery(Name = "type")] ResourceUsageType type, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetResourceUsageByTypeQuery(type, startDate, endDate), ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Get resource usage trends over time
    /// </summary>
    /// <param name="type">Resource usage type to filter by (required)</param>
    /// <param name="startDate">Start date for the query range</param>
    /// <param name="endDate">End date for the query range</param>
    /// <param name="granularity">Time granularity for trend data (Daily, Weekly, Monthly)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Usage trends with time-series data</returns>
    [HttpGet("v{version:apiVersion}/resources/usage-trends")]
    [EndpointSummary("Get resource usage trends over time")]
    [EndpointDescription("Retrieves resource usage trends with time-series data aggregated by the specified granularity.")]
    [ProducesResponseType<UsageTrendsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsageTrends(
        [FromQuery(Name = "type")] ResourceUsageType type,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] TrendGranularity granularity = TrendGranularity.Daily,
        CancellationToken ct = default)
    {
        return Ok(await sender.Send(new GetResourceUsageTrendsQuery(type, startDate, endDate, granularity), ct).ConfigureAwait(false));
    }

    #endregion

    #region Administrative Operations - /v1/resources:action

    /// <summary>
    ///     Archive old resource usage records
    /// </summary>
    /// <param name="body">Archive request with cutoff date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of archived records</returns>
    [HttpPost("v{version:apiVersion}/resources:archive")]
    [EndpointSummary("Archive old resource usage records")]
    [EndpointDescription("Archives resource usage records older than the specified date for storage optimization.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Archive([FromBody] ArchiveResourceUsageRecordsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var count = await sender.Send(new ArchiveResourceUsageRecordsCommand(body.OlderThan), ct).ConfigureAwait(false);

        return Ok(new { archived = count });
    }

    /// <summary>
    ///     Cleanup orphaned resources
    /// </summary>
    /// <param name="body">Cleanup request with options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of cleaned up resources</returns>
    [HttpPost("v{version:apiVersion}/resources:cleanup")]
    [EndpointSummary("Cleanup orphaned resources")]
    [EndpointDescription("Identifies and removes orphaned resources that are no longer associated with any tenant or user.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cleanup([FromBody] CleanupOrphanedResourcesRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var count = await sender.Send(new CleanupOrphanedResourcesCommand(body.DryRun, body.ResourceTypes), ct).ConfigureAwait(false);

        return Ok(new { cleanedUp = count, dryRun = body.DryRun });
    }

    #endregion
}

/// <summary>
///     Granularity for trend data aggregation
/// </summary>
public enum TrendGranularity
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
///     Result of usage trends query
/// </summary>
public sealed record UsageTrendsResult(
    ResourceUsageType Type,
    DateTime StartDate,
    DateTime EndDate,
    TrendGranularity Granularity,
    List<UsageTrendDataPoint> DataPoints);

/// <summary>
///     Single data point in usage trend
/// </summary>
public record UsageTrendDataPoint(DateTime Period, long TotalUsage, int TenantCount);

/// <summary>
///     Request to cleanup orphaned resources
/// </summary>
public sealed record CleanupOrphanedResourcesRequest(bool DryRun = true, List<ResourceUsageType>? ResourceTypes = null);
