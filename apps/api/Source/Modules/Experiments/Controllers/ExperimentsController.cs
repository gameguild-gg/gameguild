using GameGuild.Modules.Experiments.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Experiments.Controllers;

[ApiController]
[Route("api/experiments")]
[Authorize]
public class ExperimentsController : ControllerBase
{
    private readonly IExperimentService _experimentService;

    public ExperimentsController(IExperimentService experimentService)
    {
        _experimentService = experimentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateExperiment([FromBody] CreateExperimentRequest request, CancellationToken cancellationToken)
    {
        var result = await _experimentService.CreateExperimentAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{experimentId}")]
    public async Task<IActionResult> GetExperiment(Guid experimentId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.GetExperimentAsync(experimentId, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetExperiments([FromQuery] Guid? tenantId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _experimentService.GetExperimentsAsync(tenantId, status, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{experimentId}/start")]
    public async Task<IActionResult> StartExperiment(Guid experimentId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.StartExperimentAsync(experimentId, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpPost("{experimentId}/pause")]
    public async Task<IActionResult> PauseExperiment(Guid experimentId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.PauseExperimentAsync(experimentId, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpPost("{experimentId}/complete")]
    public async Task<IActionResult> CompleteExperiment(Guid experimentId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.CompleteExperimentAsync(experimentId, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpPost("{experimentId}/variants")]
    public async Task<IActionResult> AddVariant(Guid experimentId, [FromBody] AddVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _experimentService.AddVariantAsync(experimentId, request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{experimentId}/assignments")]
    public async Task<IActionResult> AssignUser(Guid experimentId, [FromBody] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.AssignUserAsync(experimentId, userId, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("assignments/{assignmentId}/conversions")]
    public async Task<IActionResult> RecordConversion(Guid assignmentId, [FromBody] decimal revenue, CancellationToken cancellationToken)
    {
        var result = await _experimentService.RecordConversionAsync(assignmentId, revenue, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpGet("{experimentId}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid experimentId, CancellationToken cancellationToken)
    {
        var result = await _experimentService.GetAnalyticsAsync(experimentId, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }
}
