using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
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
[ApiController]
[ApiVersion("1.0")]
[Tags("resources")]
[Authorize(Policy = "RequireAdminRole")]
[EnableRateLimiting(RateLimitPolicies.Internal)]
public sealed class ResourcesController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/resources

    /// <summary>
    ///     Get usage by resource type across all tenants
    /// </summary>
    /// <param name="usageType">Resource usage type to query</param>
    /// <param name="startDate">Start date for the query range</param>
    /// <param name="endDate">End date for the query range</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated usage data by type</returns>
    [HttpGet("v{version:apiVersion}/resources/usage-by-type/{usageType}")]
    [EndpointSummary("Get usage by resource type across all tenants")]
    [EndpointDescription("Retrieves aggregated resource usage for a specific type across all tenants within the specified date range.")]
    [ProducesResponseType<Dictionary<Guid, int>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UsageByType(ResourceUsageType usageType, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetResourceUsageByTypeQuery(usageType, startDate, endDate), ct).ConfigureAwait(false));
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

    #endregion
}
