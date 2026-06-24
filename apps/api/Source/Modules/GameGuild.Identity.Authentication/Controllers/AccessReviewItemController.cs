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
///     API controller for Access Review Items and Periodic Reviews.
///     Handles individual review item decisions, bulk reviews, and periodic review schedules.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/access-reviews")]
[Microsoft.AspNetCore.Http.Tags("auth/access-reviews")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class AccessReviewItemController(IMediator mediator, ILogger<AccessReviewItemController> logger)
    : BaseApiController
{
    private readonly ILogger<AccessReviewItemController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

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
        var query = new GetAccessReviewItemsQuery
        {
            CampaignId = campaignId,
            Status = status,
            ReviewerId = reviewerId,
            Page = page,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Review an access review item
    /// </summary>
    [HttpPost("items/{itemId}:review")]
    public async Task<ActionResult> ReviewAccessItem(Guid itemId, [FromBody] ReviewAccessItemCommand command)
    {
        var reviewCommand = command with { ItemId = itemId };
        await _mediator.Send(reviewCommand).ConfigureAwait(false);

        return Ok(new { message = "Access review item reviewed successfully" });
    }

    /// <summary>
    ///     Bulk review access items
    /// </summary>
    [HttpPost("items:bulk-review")]
    public async Task<ActionResult<BulkAccessReviewResult>> BulkReviewAccessItems(
        [FromBody] BulkReviewAccessItemsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get access review item details
    /// </summary>
    [HttpGet("items/{itemId}")]
    public async Task<ActionResult<AccessReviewItemDetails>> GetAccessReviewItemDetails(Guid itemId)
    {
        var query = new GetAccessReviewItemDetailsQuery { ItemId = itemId };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Periodic Reviews

    /// <summary>
    ///     Create a periodic access review schedule
    /// </summary>
    [HttpPost("periodic")]
    public async Task<ActionResult<PeriodicAccessReview>> CreatePeriodicAccessReview(
        [FromBody] CreatePeriodicAccessReviewCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetPeriodicAccessReview), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Get a periodic access review by ID
    /// </summary>
    [HttpGet("periodic/{scheduleId}")]
    public async Task<ActionResult<PeriodicAccessReview>> GetPeriodicAccessReview(Guid scheduleId)
    {
        var query = new GetPeriodicAccessReviewQuery { ReviewId = scheduleId };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
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
        var query = new GetPeriodicAccessReviewsQuery
        {
            TenantId = tenantId,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Trigger a periodic access review execution
    /// </summary>
    [HttpPost("periodic/{scheduleId}:trigger")]
    public async Task<ActionResult<AccessReviewCampaign>> TriggerPeriodicAccessReview(Guid scheduleId)
    {
        var command = new TriggerPeriodicAccessReviewCommand { ReviewId = scheduleId };
        var result = await _mediator.Send(command).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion
}
