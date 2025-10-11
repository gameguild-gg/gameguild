using GameGuild.Modules.DataArchival.Commands;
using GameGuild.Modules.DataArchival.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.DataArchival.Controllers;

/// <summary>
/// Controller for data archival operations.
/// </summary>
[ApiController]
[Route("api/archival")]
[Authorize]
public class DataArchivalController : ControllerBase
{
    private readonly IMediator _mediator;

    public DataArchivalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new archival policy.
    /// </summary>
    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateArchivalPolicyCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetPolicy), new { policyId = result.Data!.Id }, result.Data);

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Get an archival policy by ID.
    /// </summary>
    [HttpGet("policies/{policyId:guid}")]
    public async Task<IActionResult> GetPolicy(Guid policyId, CancellationToken cancellationToken)
    {
        var query = new GetArchivalPolicyByIdQuery { PolicyId = policyId };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Data != null)
            return Ok(result.Data);

        if (result.IsSuccess && result.Data == null)
            return NotFound();

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Get archival policies with optional filtering.
    /// </summary>
    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? entityType,
        CancellationToken cancellationToken)
    {
        var query = new GetArchivalPoliciesQuery
        {
            TenantId = tenantId,
            EntityType = entityType
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Update an archival policy.
    /// </summary>
    [HttpPut("policies/{policyId:guid}")]
    public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] UpdateArchivalPolicyCommand command, CancellationToken cancellationToken)
    {
        var updatedCommand = command with { PolicyId = policyId };
        var result = await _mediator.Send(updatedCommand, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Delete an archival policy.
    /// </summary>
    [HttpDelete("policies/{policyId:guid}")]
    public async Task<IActionResult> DeletePolicy(Guid policyId, CancellationToken cancellationToken)
    {
        var command = new DeleteArchivalPolicyCommand { PolicyId = policyId };
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Execute an archival policy.
    /// </summary>
    [HttpPost("policies/{policyId:guid}/execute")]
    public async Task<IActionResult> ExecutePolicy(Guid policyId, CancellationToken cancellationToken)
    {
        var command = new ExecuteArchivalPolicyCommand { PolicyId = policyId };
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId = result.Data }, new { jobId = result.Data });

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Get the status of an archival job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken cancellationToken)
    {
        var query = new GetArchivalJobStatusQuery { JobId = jobId };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Data != null)
            return Ok(result.Data);

        if (result.IsSuccess && result.Data == null)
            return NotFound();

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Get archival jobs with optional filtering.
    /// </summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? policyId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new GetArchivalJobsQuery
        {
            TenantId = tenantId,
            PolicyId = policyId,
            Status = status
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Cancel an archival job.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/cancel")]
    public async Task<IActionResult> CancelJob(Guid jobId, CancellationToken cancellationToken)
    {
        var command = new CancelArchivalJobCommand { JobId = jobId };
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok();

        return BadRequest(result.ErrorMessage);
    }
}
