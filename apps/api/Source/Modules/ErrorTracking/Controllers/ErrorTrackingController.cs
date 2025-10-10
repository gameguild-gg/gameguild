using GameGuild.Modules.ErrorTracking.Commands;
using GameGuild.Modules.ErrorTracking.Queries;
using GameGuild.Modules.ErrorTracking.Services;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.ErrorTracking.Controllers;

/// <summary>
/// Controller for error tracking operations.
/// </summary>
[ApiController]
[Route("api/errors")]
[Authorize]
public class ErrorTrackingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ErrorTrackingController> _logger;

    public ErrorTrackingController(
        IMediator mediator,
        ILogger<ErrorTrackingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Capture an error event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CaptureError([FromBody] CaptureErrorRequest request, CancellationToken cancellationToken)
    {
        var command = new CaptureErrorCommand(
            request.TenantId,
            request.ExceptionType,
            request.Message,
            request.StackTrace,
            request.Severity,
            request.Environment,
            request.Release,
            request.UserId,
            request.Url,
            request.HttpMethod,
            request.UserAgent,
            request.IpAddress,
            request.Tags,
            request.ContextData,
            request.Breadcrumbs
        );

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get error issues with filtering.
    /// </summary>
    [HttpGet("issues")]
    [ProducesResponseType(typeof(List<ErrorIssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIssues(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] string? environment,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetErrorIssuesQuery(
            tenantId,
            status,
            severity,
            environment,
            startDate,
            endDate,
            pageNumber,
            pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get a single error issue by ID.
    /// </summary>
    [HttpGet("issues/{id:guid}")]
    [ProducesResponseType(typeof(ErrorIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssue(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetErrorIssueByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Get events for an error issue.
    /// </summary>
    [HttpGet("issues/{id:guid}/events")]
    [ProducesResponseType(typeof(List<ErrorEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIssueEvents(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetErrorEventsQuery(id, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Resolve an error issue.
    /// </summary>
    [HttpPost("issues/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveIssue(
        Guid id,
        [FromBody] ResolveIssueRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResolveIssueCommand(id, request.UserId, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Ignore an error issue.
    /// </summary>
    [HttpPost("issues/{id:guid}/ignore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IgnoreIssue(Guid id, CancellationToken cancellationToken)
    {
        var command = new IgnoreIssueCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Delete an error issue.
    /// </summary>
    [HttpDelete("issues/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteIssue(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteIssueCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get error statistics.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ErrorStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetErrorStatisticsQuery(tenantId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

/// <summary>
/// Request model for resolving an issue.
/// </summary>
public record ResolveIssueRequest(Guid UserId, string? Notes);
