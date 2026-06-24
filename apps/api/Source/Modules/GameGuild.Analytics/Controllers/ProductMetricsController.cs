using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;

namespace GameGuild.Analytics;

[Authorize]
[ApiController]
[Route("api/metrics/product")]
[Microsoft.AspNetCore.Http.Tags("metrics/product")]
public sealed class ProductMetricsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProductMetricsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductMetricsResponse>> Get(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] Guid? tenantId,
        CancellationToken ct)
    {
        return Ok(await sender.Send(new GetProductMetricsQuery(startUtc, endUtc, tenantId), ct).ConfigureAwait(false));
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] Guid? tenantId,
        [FromQuery] ProductMetricsExportFormat format = ProductMetricsExportFormat.Csv,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new ExportProductMetricsQuery(startUtc, endUtc, tenantId, format), ct).ConfigureAwait(false);
        return File(
            System.Text.Encoding.UTF8.GetBytes(result.Content),
            result.ContentType,
            result.FileName);
    }
}
