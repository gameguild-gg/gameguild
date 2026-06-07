using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;

namespace GameGuild.Analytics;

[Authorize]
[ApiController]
[Route("api/analytics/dashboards")]
[Microsoft.AspNetCore.Http.Tags("analytics/dashboards")]
public sealed class DashboardsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DashboardDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DashboardDto>>> List(
        [FromQuery] Guid? tenantId,
        CancellationToken ct)
        => Ok(await sender.Send(new GetDashboardsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpGet("{id:guid}", Name = "GetAnalyticsDashboardById")]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardDto>> Get(Guid id, CancellationToken ct)
    {
        var dashboard = await sender.Send(new GetDashboardByIdQuery(id), ct).ConfigureAwait(false);
        return dashboard is null ? NotFound() : Ok(dashboard);
    }

    [HttpPost]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DashboardDto>> Create([FromBody] CreateDashboardRequest request, CancellationToken ct)
    {
        var dashboard = await sender.Send(new CreateDashboardCommand(request), ct).ConfigureAwait(false);
        return CreatedAtRoute("GetAnalyticsDashboardById", new { id = dashboard.Id }, dashboard);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardDto>> Update(Guid id, [FromBody] UpdateDashboardRequest request, CancellationToken ct)
    {
        var dashboard = await sender.Send(new UpdateDashboardCommand(id, request), ct).ConfigureAwait(false);
        return dashboard is null ? NotFound() : Ok(dashboard);
    }
}
