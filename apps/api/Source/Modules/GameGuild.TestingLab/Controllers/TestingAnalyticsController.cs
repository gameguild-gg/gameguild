using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.TestingLab;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing/analytics")]
[Authorize]
public sealed class TestingAnalyticsController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Analytics)]
    public async Task<ActionResult<TestingLabAnalyticsReportProjection>> GetReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeComparison = true,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new GetTestingLabAnalyticsReportQuery(fromDate, toDate, includeComparison),
            cancellationToken).ConfigureAwait(false));

    [HttpGet("export")]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Analytics)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestingLabAnalyticsExportProjection>> Export(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ExportTestingLabAnalyticsReportQuery(fromDate, toDate),
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : ToActionResult(result);
    }
}
