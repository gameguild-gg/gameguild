using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Resources.Commands;
using GameGuild.Resources.Models;
using GameGuild.Resources.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Resources.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ResourcesController(ISender sender) : ControllerBase
{
    // GET /resources/tenant/{tenantId}/usage?usageType=&startDate=&endDate=
    [HttpGet("tenant/{tenantId:guid}/usage-records")]
    public async Task<IActionResult> GetUsageRecords(Guid tenantId, [FromQuery] ResourceUsageType? usageType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetResourceUsageRecordsQuery(tenantId, usageType, startDate, endDate), ct).ConfigureAwait(false));
    }

    // GET /resources/tenant/{tenantId}/usage-summary
    [HttpGet("tenant/{tenantId:guid}/usage-summary")]
    public async Task<IActionResult> GetCurrentUsageSummary(Guid tenantId, CancellationToken ct) { return Ok(await sender.Send(new GetCurrentResourceUsageSummaryQuery(tenantId), ct).ConfigureAwait(false)); }

    // GET /resources/tenant/{tenantId}/limits?usageType=
    [HttpGet("tenant/{tenantId:guid}/limits")]
    public async Task<IActionResult> CheckLimits(Guid tenantId, [FromQuery] ResourceUsageType? usageType, CancellationToken ct)
    {
        return Ok(await sender.Send(new CheckResourceUsageLimitsQuery(tenantId, usageType), ct).ConfigureAwait(false));
    }

    // POST /quotas - Create or update a quota
    [HttpPost("/quotas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetQuota([FromBody] SetResourceQuotaRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await sender.Send(new SetResourceQuotaCommand(request.TenantId, request.Type, request.SoftLimit, request.HardLimit, request.Period, request.IsActive, request.ResetTime), ct).ConfigureAwait(false);

        return Ok();
    }

    // GET /resources/tenant/{tenantId}/quota?usageType= - Get quota for a specific resource type
    [HttpGet("tenant/{tenantId:guid}/quota")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuota(Guid tenantId, [FromQuery] ResourceUsageType usageType, CancellationToken ct)
    {
        var quota = await sender.Send(new GetResourceQuotaQuery(tenantId, usageType), ct).ConfigureAwait(false);

        if (quota == null) { return NotFound(); }

        return Ok(quota);
    }

    // GET /resources/usage-by-type/{usageType}?startDate=&endDate=
    [HttpGet("usage-by-type/{usageType}")]
    public async Task<IActionResult> UsageByType(ResourceUsageType usageType, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetResourceUsageByTypeQuery(usageType, startDate, endDate), ct).ConfigureAwait(false));
    }

    [HttpPost("record")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Record([FromBody] RecordResourceUsageRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var id = await sender.Send(new RecordResourceUsageCommand(body.TenantId, body.ResourceUsageType, body.Count, body.PeriodStart, body.PeriodEnd, body.Metadata), ct).ConfigureAwait(false);

        return Created(new Uri($"/resources/usage-records/{id}", UriKind.Relative), new { id });
    }

    [HttpPost("record-with-quota-check")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RecordWithQuotaCheck([FromBody] RecordResourceUsageRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        // Check quota before recording
        var quotaCheck = await sender.Send(new CheckResourceQuotaQuery(body.TenantId, body.ResourceUsageType, body.Count), ct).ConfigureAwait(false);

        if (!quotaCheck.IsAllowed) { return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Quota exceeded", details = quotaCheck }); }

        // Record usage
        var id = await sender.Send(new RecordResourceUsageCommand(body.TenantId, body.ResourceUsageType, body.Count, body.PeriodStart, body.PeriodEnd, body.Metadata), ct).ConfigureAwait(false);

        return Created(new Uri($"/resources/usage-records/{id}", UriKind.Relative), new { id, quotaInfo = quotaCheck });
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset([FromBody] ResetResourceUsageRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        await sender.Send(new ResetResourceUsageCommand(body.TenantId, body.ResourceUsageType), ct).ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive([FromBody] ArchiveResourceUsageRecordsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var count = await sender.Send(new ArchiveResourceUsageRecordsCommand(body.OlderThan), ct).ConfigureAwait(false);

        return Ok(new { archived = count });
    }
}
