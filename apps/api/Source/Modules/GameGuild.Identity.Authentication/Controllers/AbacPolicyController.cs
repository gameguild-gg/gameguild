using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// TODO: Reactivate this controller when ABAC policy management is ready for production
/// <summary>
///     API controller for Attribute-Based Access Control (ABAC) policy management
///     Provides comprehensive CRUD operations and policy evaluation capabilities
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/abac-policies")]
[Tags("abac-policies")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AbacPolicyController(IMediator mediator, ILogger<AbacPolicyController> logger) : ControllerBase
{
    private readonly ILogger<AbacPolicyController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Policy CRUD Operations

    /// <summary>
    ///     Create a new ABAC policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AbacPolicy>> CreateAbacPolicy([FromBody] CreateAbacPolicyCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetAbacPolicy), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ABAC policy {PolicyName}", command.Name);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get an ABAC policy by ID
    /// </summary>
    [HttpGet("{policyId}")]
    public async Task<ActionResult<AbacPolicy>> GetAbacPolicy(Guid policyId)
    {
        try
        {
            var query = new GetAbacPolicyQuery { PolicyId = policyId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Update an existing ABAC policy
    /// </summary>
    [HttpPut("{policyId}")]
    public async Task<ActionResult<AbacPolicy>> UpdateAbacPolicy(Guid policyId, [FromBody] UpdateAbacPolicyCommand command)
    {
        try
        {
            command.PolicyId = policyId;
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete an ABAC policy
    /// </summary>
    [HttpDelete("{policyId}")]
    public async Task<ActionResult> DeleteAbacPolicy(Guid policyId)
    {
        try
        {
            var command = new DeleteAbacPolicyCommand { PolicyId = policyId };
            await _mediator.Send(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get all ABAC policies for a tenant with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<Models.PagedResult<AbacPolicy>>> GetAbacPolicies(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        try
        {
            var query = new GetAbacPoliciesQuery { TenantId = tenantId, IsActive = isActive, Category = category, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policies");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Evaluation

    /// <summary>
    ///     Evaluate ABAC policies for a specific context
    /// </summary>
    [HttpPost(":evaluate")]
    public async Task<ActionResult<AbacEvaluationResult>> EvaluateAbacPolicies([FromBody] EvaluateAbacPoliciesCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate ABAC policies for user {UserId}", command.Context.UserAttributes.GetValueOrDefault("userId"));

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk evaluate ABAC policies for multiple contexts
    /// </summary>
    [HttpPost(":evaluate-bulk")]
    public async Task<ActionResult<BulkAbacEvaluationResult>> BulkEvaluateAbacPolicies([FromBody] BulkEvaluateAbacPoliciesCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk evaluate ABAC policies for {ContextCount} contexts", command.Contexts.Count);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Test an ABAC policy expression without saving
    /// </summary>
    [HttpPost(":test-expression")]
    public async Task<ActionResult<AbacExpressionTestResult>> TestAbacExpression([FromBody] TestAbacExpressionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test ABAC expression");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Management

    /// <summary>
    ///     Activate an ABAC policy
    /// </summary>
    [HttpPost("{policyId}:activate")]
    public async Task<ActionResult> ActivateAbacPolicy(Guid policyId)
    {
        try
        {
            var command = new ActivateAbacPolicyCommand { PolicyId = policyId };
            await _mediator.Send(command);

            return Ok(new { message = "ABAC policy activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Deactivate an ABAC policy
    /// </summary>
    [HttpPost("{policyId}:deactivate")]
    public async Task<ActionResult> DeactivateAbacPolicy(Guid policyId)
    {
        try
        {
            var command = new DeactivateAbacPolicyCommand { PolicyId = policyId };
            await _mediator.Send(command);

            return Ok(new { message = "ABAC policy deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Clone an existing ABAC policy
    /// </summary>
    [HttpPost("{policyId}:clone")]
    public async Task<ActionResult<AbacPolicy>> CloneAbacPolicy(Guid policyId, [FromBody] CloneAbacPolicyCommand command)
    {
        try
        {
            command.SourcePolicyId = policyId;
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetAbacPolicy), new { policyId = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone ABAC policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Analytics

    /// <summary>
    ///     Get ABAC policy statistics and performance metrics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AbacPolicyStatisticsDto>> GetAbacPolicyStatistics([FromQuery] Guid? tenantId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetAbacPolicyStatisticsQuery { TenantId = tenantId, FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30), ToDate = toDate ?? DateTime.UtcNow };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy statistics");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get ABAC policy usage analytics
    /// </summary>
    [HttpGet("{policyId}/usage")]
    public async Task<ActionResult<AbacPolicyUsageDto>> GetAbacPolicyUsage(Guid policyId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetAbacPolicyUsageQuery { PolicyId = policyId, FromDate = fromDate ?? DateTime.UtcNow.AddDays(-7), ToDate = toDate ?? DateTime.UtcNow };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy usage for policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get ABAC policy evaluation audit trail
    /// </summary>
    [HttpGet("{policyId}/audit-trail")]
    public async Task<ActionResult<AbacPolicyAuditTrailDto>> GetAbacPolicyAuditTrail(Guid policyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetAbacPolicyAuditTrailQuery { PolicyId = policyId, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy audit trail for policy {PolicyId}", policyId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Validation

    /// <summary>
    ///     Validate ABAC policy syntax and structure
    /// </summary>
    [HttpPost(":validate")]
    public async Task<ActionResult<AbacPolicyValidationResult>> ValidateAbacPolicy([FromBody] ValidateAbacPolicyCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ABAC policy");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get ABAC policy conflicts and overlaps
    /// </summary>
    [HttpGet("conflicts")]
    public async Task<ActionResult<AbacPolicyConflictsDto>> GetAbacPolicyConflicts([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var query = new GetAbacPolicyConflictsQuery { TenantId = tenantId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy conflicts");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Templates

    /// <summary>
    ///     Get available ABAC policy templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<AbacPolicyTemplateDto>>> GetAbacPolicyTemplates()
    {
        try
        {
            var query = new GetAbacPolicyTemplatesQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ABAC policy templates");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Create ABAC policy from template
    /// </summary>
    [HttpPost("templates/{templateId}:instantiate")]
    public async Task<ActionResult<AbacPolicy>> CreateAbacPolicyFromTemplate(Guid templateId, [FromBody] CreateAbacPolicyFromTemplateCommand command)
    {
        try
        {
            command.TemplateId = templateId;
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetAbacPolicy), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ABAC policy from template {TemplateId}", templateId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion
}
