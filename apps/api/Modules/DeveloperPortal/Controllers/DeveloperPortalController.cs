using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Modules.DeveloperPortal.Commands;
using GameGuild.Modules.DeveloperPortal.Queries;

namespace GameGuild.Modules.DeveloperPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/developer")]
public class DeveloperPortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeveloperPortalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("keys")]
    public async Task<IActionResult> GenerateApiKey(
        [FromBody] GenerateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApiKeys), new { developerId = command.DeveloperId }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("keys")]
    public async Task<IActionResult> GetApiKeys(
        [FromQuery] Guid developerId,
        [FromQuery] bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetApiKeysByDeveloperQuery(developerId, includeRevoked);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> RevokeApiKey(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RevokeApiKeyCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    [HttpPost("keys/{id}/rotate")]
    public async Task<IActionResult> RotateApiKey(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RotateApiKeyCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(new { newKeyId = result.Value })
            : BadRequest(result.Error);
    }

    [HttpGet("usage/stats")]
    public async Task<IActionResult> GetUsageStats(
        [FromQuery] Guid developerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetApiUsageStatsQuery(developerId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("usage/logs")]
    public async Task<IActionResult> GetUsageLogs(
        [FromQuery] Guid developerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetApiUsageLogsQuery(developerId, startDate, endDate, skip, take);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboardingStatus(
        [FromQuery] Guid developerId,
        CancellationToken cancellationToken)
    {
        var query = new GetOnboardingStatusQuery(developerId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("onboarding/start")]
    public async Task<IActionResult> StartOnboarding(
        [FromBody] StartOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOnboardingStatus), new { developerId = command.DeveloperId }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding(
        [FromBody] CompleteOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    [HttpPatch("onboarding/progress")]
    public async Task<IActionResult> UpdateOnboardingProgress(
        [FromBody] UpdateOnboardingProgressCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }
}
