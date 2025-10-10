using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Queries;

namespace GameGuild.Modules.SlaMonitoring.Controllers;

/// <summary>
/// API controller for SLA/SLO monitoring operations.
/// </summary>
[ApiController]
[Route("api/sla")]
[Authorize]
public class SlaMonitoringController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaMonitoringController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new service level objective.
    /// </summary>
    [HttpPost("slos")]
    public async Task<IActionResult> CreateSlo([FromBody] CreateSloCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetSlo), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Get all service level objectives.
    /// </summary>
    [HttpGet("slos")]
    public async Task<IActionResult> GetSlos(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSlosQuery(tenantId, isActive, serviceName, skip, take);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a service level objective by ID.
    /// </summary>
    [HttpGet("slos/{id}")]
    public async Task<IActionResult> GetSlo(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetSloByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a service level objective.
    /// </summary>
    [HttpPut("slos/{id}")]
    public async Task<IActionResult> UpdateSlo(Guid id, [FromBody] UpdateSloCommand command, CancellationToken cancellationToken)
    {
        if (id != command.SloId)
            return BadRequest("SLO ID mismatch");

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Delete a service level objective.
    /// </summary>
    [HttpDelete("slos/{id}")]
    public async Task<IActionResult> DeleteSlo(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteSloCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Record a service level indicator metric.
    /// </summary>
    [HttpPost("slis")]
    public async Task<IActionResult> RecordSli([FromBody] RecordSliMetricCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Accepted();
    }

    /// <summary>
    /// Get compliance status for an SLO.
    /// </summary>
    [HttpGet("slos/{id}/compliance")]
    public async Task<IActionResult> GetCompliance(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSloComplianceQuery(id, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get error budget for an SLO.
    /// </summary>
    [HttpGet("slos/{id}/error-budget")]
    public async Task<IActionResult> GetErrorBudget(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetErrorBudgetQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get SLO violations.
    /// </summary>
    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations(
        [FromQuery] Guid? sloId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool onlyActive = false,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSloViolationsQuery(sloId, tenantId, onlyActive, startDate, endDate, skip, take);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Resolve an SLO violation.
    /// </summary>
    [HttpPost("violations/{id}/resolve")]
    public async Task<IActionResult> ResolveViolation(
        Guid id,
        [FromBody] ResolveSloViolationCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ViolationId)
            return BadRequest("Violation ID mismatch");

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }
}
