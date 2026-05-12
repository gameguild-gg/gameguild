using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// PLANNED: Reactivate this controller when access review and compliance features are ready for production
/// <summary>
///     API controller for Access Review Campaign management.
///     Handles campaign CRUD, lifecycle (start/complete), reminders, and templates.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/access-reviews")]
[Microsoft.AspNetCore.Http.Tags("auth/access-reviews")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class AccessReviewCampaignController(IMediator mediator, ILogger<AccessReviewCampaignController> logger)
    : BaseApiController
{
    private readonly ILogger<AccessReviewCampaignController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Access Review Campaigns

    /// <summary>
    ///     Create a new access review campaign
    /// </summary>
    [HttpPost("campaigns")]
    public async Task<ActionResult<AccessReviewCampaign>> CreateAccessReviewCampaign(
        [FromBody] CreateAccessReviewCampaignCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetAccessReviewCampaign), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Get an access review campaign by ID
    /// </summary>
    [HttpGet("campaigns/{campaignId}")]
    public async Task<ActionResult<AccessReviewCampaign>> GetAccessReviewCampaign(Guid campaignId)
    {
        var query = new GetAccessReviewCampaignQuery { CampaignId = campaignId };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Update an existing access review campaign
    /// </summary>
    [HttpPut("campaigns/{campaignId}")]
    public async Task<ActionResult<AccessReviewCampaign>> UpdateAccessReviewCampaign(Guid campaignId,
        [FromBody] UpdateAccessReviewCampaignCommand command)
    {
        var updateCommand = command with { CampaignId = campaignId };
        var result = await _mediator.Send(updateCommand).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Delete an access review campaign
    /// </summary>
    [HttpDelete("campaigns/{campaignId}")]
    public async Task<ActionResult> DeleteAccessReviewCampaign(Guid campaignId)
    {
        var command = new DeleteAccessReviewCampaignCommand { CampaignId = campaignId };
        await _mediator.Send(command).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Get all access review campaigns with optional filtering
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<ActionResult<PagedResult<AccessReviewCampaign>>> GetAccessReviewCampaigns(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var query = new GetAccessReviewCampaignsQuery
        {
            TenantId = tenantId,
            Status = status,
            Type = type,
            Page = page,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Start an access review campaign
    /// </summary>
    [HttpPost("campaigns/{campaignId}:start")]
    public async Task<ActionResult> StartAccessReviewCampaign(Guid campaignId)
    {
        var command = new StartAccessReviewCampaignCommand { CampaignId = campaignId };
        await _mediator.Send(command).ConfigureAwait(false);

        return Ok(new { message = "Access review campaign started successfully" });
    }

    /// <summary>
    ///     Complete an access review campaign
    /// </summary>
    [HttpPost("campaigns/{campaignId}:complete")]
    public async Task<ActionResult<AccessReviewCampaignResult>> CompleteAccessReviewCampaign(Guid campaignId)
    {
        var command = new CompleteAccessReviewCampaignCommand { CampaignId = campaignId };
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Review Reminders

    /// <summary>
    ///     Send review reminders to reviewers
    /// </summary>
    [HttpPost("campaigns/{campaignId}:send-reminders")]
    public async Task<ActionResult<ReminderResult>> SendReviewReminders(Guid campaignId)
    {
        var command = new SendReviewRemindersCommand { CampaignId = campaignId };
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Configure review reminder settings
    /// </summary>
    [HttpPost("reminder-settings")]
    public async Task<ActionResult> ConfigureReminderSettings([FromBody] ConfigureReminderSettingsCommand command)
    {
        await _mediator.Send(command).ConfigureAwait(false);

        return Ok(new { message = "Reminder settings configured successfully" });
    }

    #endregion

    #region Access Review Templates

    /// <summary>
    ///     Get available access review templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<AccessReviewTemplateDto>>> GetAccessReviewTemplates()
    {
        var query = new GetAccessReviewTemplatesQuery();
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Create access review campaign from template
    /// </summary>
    [HttpPost("templates/{templateId}:create-campaign")]
    public async Task<ActionResult<AccessReviewCampaign>> CreateCampaignFromTemplate(Guid templateId,
        [FromBody] CreateCampaignFromTemplateCommand command)
    {
        var createCommand = command with { TemplateId = templateId };
        var result = await _mediator.Send(createCommand).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetAccessReviewCampaign), new { id = result.Id }, result);
    }

    #endregion
}
