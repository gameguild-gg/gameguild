using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.LaunchPad;

[ApiController]
[Authorize]
[Route("v1/launch-pad")]
public sealed class LaunchPadController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LaunchPlan>>> GetDashboard(
        [FromQuery] LaunchPlanStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLaunchPadDashboardQuery { Status = status }, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LaunchPlan>> GetLaunchPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLaunchPlanQuery { LaunchPlanId = id }, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) return ToActionResult(result);
        return result.Value == null ? NotFound() : Ok(result.Value);
    }

    [HttpGet("projects/{projectId:guid}")]
    public async Task<ActionResult<LaunchPlan>> GetProjectLaunchPlan(Guid projectId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLaunchPlanByProjectQuery { ProjectId = projectId }, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) return ToActionResult(result);
        return result.Value == null ? NotFound() : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LaunchPlan>> CreateLaunchPlan(
        [FromBody] CreateLaunchPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ActionResult<LaunchPlan>>(Conflict(new
        {
            code = "LaunchPad.ApplicationRequired",
            message = "Launch plans are created only when a Launch Pad application is approved."
        }));
    }

    [HttpPost("{id:guid}/checklist/{itemId:guid}:complete")]
    public async Task<ActionResult<LaunchPlan>> CompleteChecklistItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CompleteLaunchChecklistItemCommand
        {
            LaunchPlanId = id,
            ChecklistItemId = itemId
        }, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    [HttpPost("{id:guid}:publish")]
    public async Task<ActionResult<LaunchPlan>> PublishLaunch(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new PublishLaunchCommand { LaunchPlanId = id }, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    private ActionResult ToActionResult(Result result)
        => result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized, result.Error),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Validation => BadRequest(result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
        };
}
