using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Admin controller for promotional campaign management (issue #418): campaign
/// CRUD, lifecycle transitions (activate/pause/complete/cancel/archive), and the
/// campaign performance report (reuses the promo-code campaign performance engine,
/// now with CAC attribution). Reuses the promo-code permission family — campaigns
/// are the managed face of the promo code CampaignId grouping key.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/promo-campaigns")]
[Microsoft.AspNetCore.Http.Tags("promo-campaigns")]
[Authorize]
[RequirePermission(PromoCodesPermission.Keys.Read)]
public class PromoCampaignsController(IMediator mediator) : BaseApiController
{
    /// <summary>
    /// List campaigns (paged) with optional status filter and search
    /// </summary>
    /// <param name="status">Filter by lifecycle status</param>
    /// <param name="searchTerm">Search name, key, or description</param>
    /// <param name="skip">Items to skip</param>
    /// <param name="take">Items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PromoCampaignDto>>> GetCampaigns(
        [FromQuery] PromoCampaignStatus? status = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCampaignsQuery(status, searchTerm, skip, take);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Get a campaign by ID
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{campaignId:guid}")]
    public async Task<ActionResult<PromoCampaignDto>> GetCampaignById(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCampaignByIdQuery(campaignId);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Campaign performance report (uses, discount, conversion, CAC attribution) within an optional window
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="from">Window start (null = unbounded)</param>
    /// <param name="to">Window end (null = unbounded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{campaignId:guid}/performance")]
    public async Task<ActionResult<CampaignPerformanceDto>> GetCampaignPerformance(
        Guid campaignId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve the managed campaign, then run the performance report on its key so
        // the report covers every code grouped under the campaign (managed or not).
        var campaignQuery = new GetPromoCampaignByIdQuery(campaignId);
        var campaign = await mediator.Send(campaignQuery, cancellationToken).ConfigureAwait(false);

        var performance = await mediator.Send(
            new GetCampaignPerformanceQuery(campaign.Key, from, to),
            cancellationToken).ConfigureAwait(false);

        return Ok(performance);
    }

    /// <summary>
    /// Create a campaign (starts in Draft)
    /// </summary>
    [HttpPost]
    [RequirePermission(PromoCodesPermission.Keys.Create)]
    public async Task<ActionResult<PromoCampaignDto>> CreateCampaign(
        [FromBody] CreatePromoCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePromoCampaignCommand(
            request.Name,
            request.Key,
            request.Description,
            request.StartsAt,
            request.EndsAt,
            request.TimeZoneId,
            request.Budget,
            request.AcquisitionSpend,
            request.Currency,
            GetUserId());

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCampaignById), new { campaignId = result.Id }, result);
    }

    /// <summary>
    /// Update a campaign (key and lifecycle status are immutable here)
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{campaignId:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> UpdateCampaign(
        Guid campaignId,
        [FromBody] UpdatePromoCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePromoCampaignCommand(
            campaignId,
            request.Name,
            request.Description,
            request.StartsAt,
            request.EndsAt,
            request.TimeZoneId,
            request.Budget,
            request.AcquisitionSpend,
            request.Currency,
            GetUserId());

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Activate a draft or paused campaign
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{campaignId:guid}:activate")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> ActivateCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ActivatePromoCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Pause an active campaign
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{campaignId:guid}:pause")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> PauseCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new PausePromoCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Complete an active or paused campaign
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{campaignId:guid}:complete")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> CompleteCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CompletePromoCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a draft, active, or paused campaign
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{campaignId:guid}:cancel")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> CancelCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CancelPromoCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Archive a completed or cancelled campaign
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{campaignId:guid}:archive")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCampaignDto>> ArchiveCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ArchivePromoCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Delete a campaign. Fails closed while promo codes still reference its key.
    /// </summary>
    /// <param name="campaignId">The campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{campaignId:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Delete)]
    public async Task<IActionResult> DeleteCampaign(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeletePromoCampaignCommand(campaignId, GetUserId()), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>Request to create a promotional campaign</summary>
public sealed record CreatePromoCampaignRequest(
    string Name,
    string Key,
    string? Description = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TimeZoneId = null,
    decimal? Budget = null,
    decimal? AcquisitionSpend = null,
    string Currency = "USD"
);

/// <summary>Request to update a promotional campaign</summary>
public sealed record UpdatePromoCampaignRequest(
    string? Name = null,
    string? Description = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TimeZoneId = null,
    decimal? Budget = null,
    decimal? AcquisitionSpend = null,
    string? Currency = null
);
