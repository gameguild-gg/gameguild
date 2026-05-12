using Asp.Versioning;
using GameGuild.Identity.Authorization.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authorization.Controllers;

/// <summary>
///     API controller for Permission Analytics
/// </summary>
[Microsoft.AspNetCore.Http.Tags("access-control/permission-analytics")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/permission-analytics")]
[Authorize]
[Produces("application/json")]
public class PermissionAnalyticsController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Get permission usage metrics
    /// </summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(List<PermissionUsageMetrics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionUsage(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken
    )
    {
        var query = new GetPermissionUsageQuery(tenantId, fromDate, toDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Get user activity summary
    /// </summary>
    [HttpGet("user-activity")]
    [ProducesResponseType(typeof(List<UserActivitySummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserActivity(
        [FromQuery] Guid? tenantId,
        [FromQuery] int top = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetUserActivityQuery(tenantId, top, fromDate, toDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Get resource access patterns
    /// </summary>
    [HttpGet("resource-patterns")]
    [ProducesResponseType(typeof(List<ResourceAccessPattern>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceAccessPatterns(
        [FromQuery] Guid? tenantId,
        [FromQuery] int top = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetResourceAccessPatternsQuery(tenantId, top, fromDate, toDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Get permission trends
    /// </summary>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(List<PermissionTrend>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionTrends(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken
    )
    {
        var query = new GetPermissionTrendsQuery(tenantId, fromDate, toDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Detect permission anomalies
    /// </summary>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(List<PermissionAnomaly>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectAnomalies(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime? fromDate,
        CancellationToken cancellationToken
    )
    {
        var query = new DetectPermissionAnomaliesQuery(tenantId, fromDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Generate a permission analytics report
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(PermissionAnalyticsReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateReport(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd,
        CancellationToken cancellationToken
    )
    {
        var query = new GeneratePermissionReportQuery(tenantId, periodStart, periodEnd);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
