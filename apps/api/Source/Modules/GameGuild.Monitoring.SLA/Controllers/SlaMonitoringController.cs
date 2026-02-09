using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     API controller for SLA/SLO monitoring operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sla")]
[Authorize]
public class SlaMonitoringController(ISender sender, IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     Extracts the tenant ID from the authenticated user context.
    ///     Falls back to query-parameter value when provided, otherwise uses actor context.
    /// </summary>
    private Guid GetTenantId(Guid? explicitTenantId = null)
    {
        if (explicitTenantId.HasValue && explicitTenantId.Value != Guid.Empty)
            return explicitTenantId.Value;

        return actorContextAccessor.ActorContext.TenantId
               ?? throw new UnauthorizedAccessException("Tenant context is required but was not found in the authenticated user's claims.");
    }

    /// <summary>
    ///     Create a new service level objective.
    /// </summary>
    /// <param name="command">SLO creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created SLO details</returns>
    [HttpPost("slos")]
    [ProducesResponseType(typeof(SloDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSlo([FromBody] CreateSloCommand command, CancellationToken cancellationToken)
    {
        // Override TenantId from authenticated context when not explicitly set
        var tenantId = GetTenantId(command.TenantId != Guid.Empty ? command.TenantId : null);
        var enrichedCommand = command with { TenantId = tenantId };
        var result = await sender.Send(enrichedCommand, cancellationToken);

        return CreatedAtAction(nameof(GetSlo), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Get all service level objectives with optional filtering.
    /// </summary>
    /// <param name="tenantId">Tenant identifier (optional — defaults to current user's tenant)</param>
    /// <param name="serviceName">Optional service name filter</param>
    /// <param name="isEnabled">Optional enabled status filter</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of SLOs</returns>
    [HttpGet("slos")]
    [ProducesResponseType(typeof(List<SloDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlos(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default
    )
    {
        var actualTenantId = GetTenantId(tenantId);
        var query = new GetSlosQuery(actualTenantId, serviceName, isEnabled, skip, take);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Get a service level objective by ID.
    /// </summary>
    /// <param name="id">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SLO details</returns>
    [HttpGet("slos/{id:guid}")]
    [ProducesResponseType(typeof(SloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlo(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var query = new GetSloByIdQuery(id, tenantId);
        var result = await sender.Send(query, cancellationToken);

        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    ///     Update an existing service level objective.
    /// </summary>
    /// <param name="id">SLO identifier</param>
    /// <param name="command">Update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated SLO details</returns>
    [HttpPut("slos/{id:guid}")]
    [ProducesResponseType(typeof(SloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSlo(Guid id, [FromBody] UpdateSloCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        // Ensure TenantId is set from authenticated context
        var tenantId = GetTenantId(command.TenantId != Guid.Empty ? command.TenantId : null);
        var enrichedCommand = command with { TenantId = tenantId };
        var result = await sender.Send(enrichedCommand, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Delete a service level objective.
    /// </summary>
    /// <param name="id">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("slos/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlo(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var command = new DeleteSloCommand(id, tenantId);
        await sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Record a service level indicator metric.
    /// </summary>
    /// <param name="command">SLI metric data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("slis")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordSliMetric([FromBody] RecordSliMetricCommand command, CancellationToken cancellationToken)
    {
        // Override TenantId from authenticated context when not explicitly set
        var tenantId = GetTenantId(command.TenantId != Guid.Empty ? command.TenantId : null);
        var enrichedCommand = command with { TenantId = tenantId };
        await sender.Send(enrichedCommand, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Get compliance information for an SLO over a time period.
    /// </summary>
    /// <param name="id">SLO identifier</param>
    /// <param name="startDate">Start date for compliance calculation (optional)</param>
    /// <param name="endDate">End date for compliance calculation (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compliance details</returns>
    [HttpGet("slos/{id:guid}/compliance")]
    [ProducesResponseType(typeof(SloComplianceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompliance(Guid id, [FromQuery] DateTimeOffset? startDate = null, [FromQuery] DateTimeOffset? endDate = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = new GetSloComplianceQuery(id, tenantId, startDate, endDate);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Get error budget information for an SLO.
    /// </summary>
    /// <param name="id">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error budget details</returns>
    [HttpGet("slos/{id:guid}/error-budget")]
    [ProducesResponseType(typeof(ErrorBudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetErrorBudget(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var query = new GetErrorBudgetQuery(id, tenantId);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Get SLO violations with optional filtering.
    /// </summary>
    /// <param name="sloId">Optional SLO filter</param>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="onlyUnresolved">Filter for only unresolved violations</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of violations</returns>
    [HttpGet("violations")]
    [ProducesResponseType(typeof(List<SloViolationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetViolations(
        [FromQuery] Guid? sloId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool onlyUnresolved = false,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default
    )
    {
        // Default to current user's tenant if not explicitly specified
        var actualTenantId = tenantId ?? actorContextAccessor.ActorContext.TenantId;
        var query = new GetSloViolationsQuery(sloId, actualTenantId, onlyUnresolved, startDate, endDate, skip, take);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Resolve an SLO violation.
    /// </summary>
    /// <param name="id">Violation identifier</param>
    /// <param name="command">Resolution details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("violations/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveViolation(Guid id, [FromBody] ResolveSloViolationCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ViolationId) return BadRequest("ID mismatch");

        // Ensure TenantId is set from authenticated context
        var tenantId = GetTenantId(command.TenantId != Guid.Empty ? command.TenantId : null);
        var enrichedCommand = command with { TenantId = tenantId };
        await sender.Send(enrichedCommand, cancellationToken);

        return NoContent();
    }
}
