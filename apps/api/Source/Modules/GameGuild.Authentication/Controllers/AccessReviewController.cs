using Asp.Versioning;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Controllers;

/// <summary>
///     API controller for Access Review and Compliance management
///     Provides comprehensive access review campaigns, periodic reviews, and audit workflows
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/access-reviews")]
public class AccessReviewController(IMediator mediator, ILogger<AccessReviewController> logger) : ControllerBase
{
    private readonly ILogger<AccessReviewController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Access Review Campaigns

    /// <summary>
    ///     Create a new access review campaign
    /// </summary>
    [HttpPost("campaigns")]
    public async Task<ActionResult<AccessReviewCampaign>> CreateAccessReviewCampaign([FromBody] CreateAccessReviewCampaignCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetAccessReviewCampaign), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create access review campaign {CampaignName}", command.Name);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get an access review campaign by ID
    /// </summary>
    [HttpGet("campaigns/{id}")]
    public async Task<ActionResult<AccessReviewCampaign>> GetAccessReviewCampaign(Guid id)
    {
        try
        {
            var query = new GetAccessReviewCampaignQuery { CampaignId = id };
            var result = await _mediator.Send(query);

            if (result == null) return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review campaign {CampaignId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Update an existing access review campaign
    /// </summary>
    [HttpPut("campaigns/{id}")]
    public async Task<ActionResult<AccessReviewCampaign>> UpdateAccessReviewCampaign(Guid id, [FromBody] UpdateAccessReviewCampaignCommand command)
    {
        try
        {
            var updateCommand = command with { CampaignId = id };
            var result = await _mediator.Send(updateCommand);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update access review campaign {CampaignId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete an access review campaign
    /// </summary>
    [HttpDelete("campaigns/{id}")]
    public async Task<ActionResult> DeleteAccessReviewCampaign(Guid id)
    {
        try
        {
            var command = new DeleteAccessReviewCampaignCommand { CampaignId = id };
            await _mediator.Send(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete access review campaign {CampaignId}", id);

            return BadRequest(new { error = ex.Message });
        }
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
        try
        {
            var query = new GetAccessReviewCampaignsQuery { TenantId = tenantId, Status = status, Type = type, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review campaigns");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Start an access review campaign
    /// </summary>
    [HttpPost("campaigns/{id}/start")]
    public async Task<ActionResult> StartAccessReviewCampaign(Guid id)
    {
        try
        {
            var command = new StartAccessReviewCampaignCommand { CampaignId = id };
            await _mediator.Send(command);

            return Ok(new { message = "Access review campaign started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start access review campaign {CampaignId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Complete an access review campaign
    /// </summary>
    [HttpPost("campaigns/{id}/complete")]
    public async Task<ActionResult<AccessReviewCampaignResult>> CompleteAccessReviewCampaign(Guid id)
    {
        try
        {
            var command = new CompleteAccessReviewCampaignCommand { CampaignId = id };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete access review campaign {CampaignId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Access Review Items

    /// <summary>
    ///     Get access review items for a campaign
    /// </summary>
    [HttpGet("campaigns/{campaignId}/items")]
    public async Task<ActionResult<PagedResult<AccessReviewItem>>> GetAccessReviewItems(
        Guid campaignId,
        [FromQuery] string? status = null,
        [FromQuery] Guid? reviewerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        try
        {
            var query = new GetAccessReviewItemsQuery { CampaignId = campaignId, Status = status, ReviewerId = reviewerId, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review items for campaign {CampaignId}", campaignId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Review an access review item
    /// </summary>
    [HttpPost("items/{itemId}/review")]
    public async Task<ActionResult> ReviewAccessItem(Guid itemId, [FromBody] ReviewAccessItemCommand command)
    {
        try
        {
            var reviewCommand = command with { ItemId = itemId };
            await _mediator.Send(reviewCommand);

            return Ok(new { message = "Access review item reviewed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review access item {ItemId}", itemId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk review access items
    /// </summary>
    [HttpPost("items/bulk-review")]
    public async Task<ActionResult<BulkAccessReviewResult>> BulkReviewAccessItems([FromBody] BulkReviewAccessItemsCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk review {ItemCount} access items", command.ItemIds?.Count ?? 0);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get access review item details
    /// </summary>
    [HttpGet("items/{itemId}")]
    public async Task<ActionResult<AccessReviewItemDetails>> GetAccessReviewItemDetails(Guid itemId)
    {
        try
        {
            var query = new GetAccessReviewItemDetailsQuery { ItemId = itemId };
            var result = await _mediator.Send(query);

            if (result == null) return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review item details {ItemId}", itemId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Periodic Reviews

    /// <summary>
    ///     Create a periodic access review schedule
    /// </summary>
    [HttpPost("periodic")]
    public async Task<ActionResult<PeriodicAccessReview>> CreatePeriodicAccessReview([FromBody] CreatePeriodicAccessReviewCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetPeriodicAccessReview), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create periodic access review {ReviewName}", command.Name);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get a periodic access review by ID
    /// </summary>
    [HttpGet("periodic/{id}")]
    public async Task<ActionResult<PeriodicAccessReview>> GetPeriodicAccessReview(Guid id)
    {
        try
        {
            var query = new GetPeriodicAccessReviewQuery { ReviewId = id };
            var result = await _mediator.Send(query);

            if (result == null) return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get periodic access review {ReviewId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get all periodic access reviews
    /// </summary>
    [HttpGet("periodic")]
    public async Task<ActionResult<PagedResult<PeriodicAccessReview>>> GetPeriodicAccessReviews(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        try
        {
            var query = new GetPeriodicAccessReviewsQuery { TenantId = tenantId, IsActive = isActive, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get periodic access reviews");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Trigger a periodic access review execution
    /// </summary>
    [HttpPost("periodic/{id}/trigger")]
    public async Task<ActionResult<AccessReviewCampaign>> TriggerPeriodicAccessReview(Guid id)
    {
        try
        {
            var command = new TriggerPeriodicAccessReviewCommand { ReviewId = id };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger periodic access review {ReviewId}", id);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Access Revocation

    /// <summary>
    ///     Revoke access based on review decisions
    /// </summary>
    [HttpPost("revoke-access")]
    public async Task<ActionResult<AccessRevocationResult>> RevokeAccess([FromBody] RevokeAccessCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke access for user {UserId} on resource {ResourceId}", command.UserId, command.ResourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk revoke access for multiple users
    /// </summary>
    [HttpPost("bulk-revoke-access")]
    public async Task<ActionResult<BulkAccessRevocationResult>> BulkRevokeAccess([FromBody] BulkRevokeAccessCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk revoke access for {RevocationCount} revocations", command.Revocations?.Count ?? 0);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get access revocation history
    /// </summary>
    [HttpGet("revocation-history")]
    public async Task<ActionResult<PagedResult<AccessRevocationRecord>>> GetAccessRevocationHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        try
        {
            var query = new GetAccessRevocationHistoryQuery { UserId = userId, ResourceId = resourceId, FromDate = fromDate, ToDate = toDate, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access revocation history");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Access Review Analytics

    /// <summary>
    ///     Get access review analytics and compliance metrics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AccessReviewAnalyticsDto>> GetAccessReviewAnalytics([FromQuery] Guid? tenantId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetAccessReviewAnalyticsQuery { TenantId = tenantId, FromDate = fromDate ?? DateTime.UtcNow.AddMonths(-3), ToDate = toDate ?? DateTime.UtcNow };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review analytics");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get compliance status report
    /// </summary>
    [HttpGet("compliance-status")]
    public async Task<ActionResult<ComplianceStatusDto>> GetComplianceStatus([FromQuery] Guid tenantId)
    {
        try
        {
            var query = new GetComplianceStatusQuery { TenantId = tenantId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compliance status");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Generate access review report
    /// </summary>
    [HttpPost("generate-report")]
    public async Task<ActionResult<AccessReviewReportDto>> GenerateAccessReviewReport([FromBody] GenerateAccessReviewReportCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate access review report");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Review Reminders

    /// <summary>
    ///     Send review reminders to reviewers
    /// </summary>
    [HttpPost("campaigns/{campaignId}/send-reminders")]
    public async Task<ActionResult<ReminderResult>> SendReviewReminders(Guid campaignId)
    {
        try
        {
            var command = new SendReviewRemindersCommand { CampaignId = campaignId };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review reminders for campaign {CampaignId}", campaignId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Configure review reminder settings
    /// </summary>
    [HttpPost("reminder-settings")]
    public async Task<ActionResult> ConfigureReminderSettings([FromBody] ConfigureReminderSettingsCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return Ok(new { message = "Reminder settings configured successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure reminder settings");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Access Review Templates

    /// <summary>
    ///     Get available access review templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<AccessReviewTemplateDto>>> GetAccessReviewTemplates()
    {
        try
        {
            var query = new GetAccessReviewTemplatesQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access review templates");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Create access review campaign from template
    /// </summary>
    [HttpPost("templates/{templateId}/create-campaign")]
    public async Task<ActionResult<AccessReviewCampaign>> CreateCampaignFromTemplate(Guid templateId, [FromBody] CreateCampaignFromTemplateCommand command)
    {
        try
        {
            var createCommand = command with { TemplateId = templateId };
            var result = await _mediator.Send(createCommand);

            return CreatedAtAction(nameof(GetAccessReviewCampaign), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create campaign from template {TemplateId}", templateId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion
}
