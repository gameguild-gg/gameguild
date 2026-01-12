using GameGuild.Identity.Authorization.Commands;
using GameGuild.Identity.Authorization.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authorization.Controllers;

/// <summary>
///     API controller for Access Review operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AccessReviewsController(ISender sender) : ControllerBase
{
    // =========================================================================
    // Campaigns
    // =========================================================================

    /// <summary>
    ///     Create a new access review campaign
    /// </summary>
    [HttpPost("campaigns")]
    [ProducesResponseType(typeof(AccessReviewCampaign), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCampaign(
        [FromBody] CreateAccessReviewCampaignCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCampaignById), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Get a campaign by ID
    /// </summary>
    [HttpGet("campaigns/{id:guid}")]
    [ProducesResponseType(typeof(AccessReviewCampaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaignById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAccessReviewCampaignByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    ///     Get active campaigns
    /// </summary>
    [HttpGet("campaigns/active")]
    [ProducesResponseType(typeof(List<AccessReviewCampaign>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveCampaigns(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken
    )
    {
        var query = new GetActiveAccessReviewCampaignsQuery(tenantId);
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Start a campaign
    /// </summary>
    [HttpPost("campaigns/{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartCampaign(Guid id, CancellationToken cancellationToken)
    {
        var command = new StartAccessReviewCampaignCommand(id);
        var result = await sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Complete a campaign
    /// </summary>
    [HttpPost("campaigns/{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteCampaign(
        Guid id,
        [FromBody] CompleteCampaignRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new CompleteAccessReviewCampaignCommand(id, request.CompletedBy);
        var result = await sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Cancel a campaign
    /// </summary>
    [HttpPost("campaigns/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelCampaign(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelAccessReviewCampaignCommand(id);
        var result = await sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    ///     Send reminders for a campaign
    /// </summary>
    [HttpPost("campaigns/{id:guid}/reminders")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendReminders(Guid id, CancellationToken cancellationToken)
    {
        var command = new SendAccessReviewRemindersCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { RemindersSent = result });
    }

    // =========================================================================
    // Review Items
    // =========================================================================

    /// <summary>
    ///     Get pending review items for a reviewer
    /// </summary>
    [HttpGet("items/pending")]
    [ProducesResponseType(typeof(List<AccessReviewItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingItems(
        [FromQuery] Guid reviewerId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken
    )
    {
        var query = new GetPendingReviewItemsQuery(reviewerId, tenantId);
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Approve an access review item
    /// </summary>
    [HttpPost("items/{id:guid}/approve")]
    [ProducesResponseType(typeof(AccessReviewItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveItem(
        Guid id,
        [FromBody] ApproveItemRequest? request,
        CancellationToken cancellationToken
    )
    {
        var command = new ApproveAccessReviewItemCommand(id, request?.Reason, request?.Notes);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Revoke access for a review item
    /// </summary>
    [HttpPost("items/{id:guid}/revoke")]
    [ProducesResponseType(typeof(AccessReviewItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeItem(
        Guid id,
        [FromBody] RevokeItemRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new RevokeAccessReviewItemCommand(id, request.Reason, request.Notes);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Process expired campaigns (admin only)
    /// </summary>
    [HttpPost("campaigns/process-expired")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessExpiredCampaigns(CancellationToken cancellationToken)
    {
        var command = new ProcessExpiredCampaignsCommand();
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { ProcessedCount = result });
    }
}

// Request DTOs
public record CompleteCampaignRequest(Guid CompletedBy);
public record ApproveItemRequest(string? Reason = null, string? Notes = null);
public record RevokeItemRequest(string Reason, string? Notes = null);
