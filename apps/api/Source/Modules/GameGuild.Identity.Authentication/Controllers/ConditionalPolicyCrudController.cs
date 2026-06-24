using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// PLANNED: Reactivate this controller when conditional policy features are ready for production
/// <summary>
///     API controller for Conditional Policy CRUD, lifecycle management, templates, and conditions.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/conditional-policies")]
[Microsoft.AspNetCore.Http.Tags("auth/conditional-policies")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class ConditionalPolicyCrudController(IMediator mediator, ILogger<ConditionalPolicyCrudController> logger) : BaseApiController
{
    private readonly ILogger<ConditionalPolicyCrudController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Policy CRUD Operations

    /// <summary>
    ///     Create a new conditional policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConditionalPolicy>> CreateConditionalPolicy([FromBody] CreateConditionalPolicyCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetConditionalPolicy), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Get a conditional policy by ID
    /// </summary>
    [HttpGet("{policyId}")]
    public async Task<ActionResult<ConditionalPolicy>> GetConditionalPolicy(Guid policyId)
    {
        var query = new GetConditionalPolicyQuery { PolicyId = policyId };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Update an existing conditional policy
    /// </summary>
    [HttpPut("{policyId}")]
    public async Task<ActionResult<ConditionalPolicy>> UpdateConditionalPolicy(Guid policyId, [FromBody] UpdateConditionalPolicyCommand command)
    {
        command.PolicyId = policyId;
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Delete a conditional policy
    /// </summary>
    [HttpDelete("{policyId}")]
    public async Task<ActionResult> DeleteConditionalPolicy(Guid policyId)
    {
        var command = new DeleteConditionalPolicyCommand { PolicyId = policyId };
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Get all conditional policies for a tenant with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ConditionalPolicy>>> GetConditionalPolicies(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? conditionType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var query = new GetConditionalPoliciesQuery { TenantId = tenantId, IsActive = isActive, ConditionType = conditionType, Page = page, PageSize = pageSize };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Policy Management

    /// <summary>
    ///     Activate a conditional policy
    /// </summary>
    [HttpPost("{policyId}:activate")]
    public async Task<ActionResult> ActivateConditionalPolicy(Guid policyId)
    {
        var command = new ActivateConditionalPolicyCommand { PolicyId = policyId };
        await _mediator.Send(command).ConfigureAwait(false);

        return Ok(new { message = "Conditional policy activated successfully" });
    }

    /// <summary>
    ///     Deactivate a conditional policy
    /// </summary>
    [HttpPost("{policyId}:deactivate")]
    public async Task<ActionResult> DeactivateConditionalPolicy(Guid policyId)
    {
        var command = new DeactivateConditionalPolicyCommand { PolicyId = policyId };
        await _mediator.Send(command).ConfigureAwait(false);

        return Ok(new { message = "Conditional policy deactivated successfully" });
    }

    /// <summary>
    ///     Clone an existing conditional policy
    /// </summary>
    [HttpPost("{policyId}:clone")]
    public async Task<ActionResult<ConditionalPolicy>> CloneConditionalPolicy(Guid policyId, [FromBody] CloneConditionalPolicyCommand command)
    {
        command.SourcePolicyId = policyId;
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetConditionalPolicy), new { policyId = result.Id }, result);
    }

    /// <summary>
    ///     Update policy priority
    /// </summary>
    [HttpPut("{policyId}/priority")]
    public async Task<ActionResult> UpdateConditionalPolicyPriority(Guid policyId, [FromBody] UpdateConditionalPolicyPriorityCommand command)
    {
        command.PolicyId = policyId;
        await _mediator.Send(command).ConfigureAwait(false);

        return Ok(new { message = "Policy priority updated successfully" });
    }

    #endregion

    #region Policy Templates

    /// <summary>
    ///     Get available conditional policy templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<ConditionalPolicyTemplateDto>>> GetConditionalPolicyTemplates()
    {
        var query = new GetConditionalPolicyTemplatesQuery();
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Create conditional policy from template
    /// </summary>
    [HttpPost("templates/{templateId}:instantiate")]
    public async Task<ActionResult<ConditionalPolicy>> CreateConditionalPolicyFromTemplate(Guid templateId, [FromBody] CreateConditionalPolicyFromTemplateCommand command)
    {
        command.TemplateId = templateId;
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetConditionalPolicy), new { id = result.Id }, result);
    }

    #endregion
}
