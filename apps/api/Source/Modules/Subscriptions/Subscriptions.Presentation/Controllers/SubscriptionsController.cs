using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediatR;
using GameGuild.Shared;
using GameGuild.Modules.Subscriptions.DTOs;
using GameGuild.Modules.Subscriptions.Features.CreateSubscription;
using GameGuild.Modules.Subscriptions.Features.GetSubscription;
using GameGuild.Modules.Subscriptions.Features.ManageSubscription;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionsController(ISender sender)
    {
        _sender = sender;
    }

    // GET /subscriptions
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        // For now, return paged results with default parameters
        var result = await _sender.Send(new GetPagedSubscriptionsQuery(1, 100), ct);
        return Ok(result);
    }

    // GET /subscriptions/{id}
    [HttpGet("{id:guid}", Name = "GetSubscriptionById")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sub = await _sender.Send(new GetSubscriptionByIdQuery(id), ct);

        return sub is null ? NotFound() : Ok(sub);
    }

    // GET /subscriptions/tenant/{tenantId}
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetForTenant(Guid tenantId, CancellationToken ct) { return Ok(await _sender.Send(new GetTenantSubscriptionsQuery(tenantId), ct)); }

    // GET /subscriptions/tenant/{tenantId}/active
    [HttpGet("tenant/{tenantId:guid}/active")]
    public async Task<IActionResult> GetActiveForTenant(Guid tenantId, CancellationToken ct) { return Ok(await _sender.Send(new GetActiveTenantSubscriptionQuery(tenantId), ct)); }

    // GET /subscriptions/plan/{planId}
    [HttpGet("plan/{planId:guid}")]
    public async Task<IActionResult> ByPlan(Guid planId, CancellationToken ct) { return Ok(await _sender.Send(new GetSubscriptionsByPlanQuery(planId), ct)); }

    // GET /subscriptions/status/{status}
    [HttpGet("status/{status}")]
    public async Task<IActionResult> ByStatus(SubscriptionStatus status, CancellationToken ct) { return Ok(await _sender.Send(new GetSubscriptionsByStatusQuery(status), ct)); }

    // GET /subscriptions/paged
    [HttpGet("paged")]
    public async Task<IActionResult> Paged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] SubscriptionStatus? status = null, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? planId = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPagedSubscriptionsQuery(page, pageSize, status, tenantId, planId), ct);

        return Ok(result);
    }

    // GET /subscriptions/metrics
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        // TODO: Implement proper metrics functionality
        return Ok(new { TotalSubscriptions = 0, ActiveSubscriptions = 0, Revenue = 0 });
    }

    // GET /subscriptions/expiring
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring(CancellationToken ct, [FromQuery] int days = 30)
    {
        // TODO: Implement proper expiring subscriptions functionality
        return Ok(new List<object>());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest body, CancellationToken ct)
    {
        var id = await _sender.Send(new CreateSubscriptionCommand(body.TenantId, body.PlanId, body.CreatedByUserId, body.BillingCycle, body.Amount, body.StartDate, body.TrialDays), ct);

        return CreatedAtRoute(
            "GetSubscriptionById",
            new
            {
                id
            },
            new
            {
                id
            }
        );
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ActivateSubscriptionCommand(id), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/start-trial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartTrial(Guid id, [FromBody] StartTrialRequest body, CancellationToken ct)
    {
        await _sender.Send(new StartTrialCommand(id, body.TrialDays), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/end-trial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EndTrial(Guid id, [FromBody] EndTrialRequest body, CancellationToken ct)
    {
        await _sender.Send(new EndTrialCommand(id, body.ConvertToPaid), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest body, CancellationToken ct)
    {
        await _sender.Send(new CancelSubscriptionCommand(id, Enum.Parse<CancellationReason>(body.Reason, true), body.Note, body.EffectiveDate), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendRequest body, CancellationToken ct)
    {
        await _sender.Send(new SuspendSubscriptionCommand(id, body.Reason), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ReactivateSubscriptionCommand(id), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/upgrade")]
    [ProducesResponseType(typeof(SubscriptionUpgradeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upgrade(Guid id, [FromBody] UpgradeRequest body, CancellationToken ct)
    {
        var result = await _sender.Send(new UpgradeSubscriptionPlanCommand(id, body.NewPlanId, body.EffectiveDate), ct);

        return Ok(result);
    }

    [HttpPost("{id:guid}/downgrade")]
    [ProducesResponseType(typeof(SubscriptionDowngradeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Downgrade(Guid id, [FromBody] DowngradeRequest body, CancellationToken ct)
    {
        var result = await _sender.Send(new DowngradeSubscriptionPlanCommand(id, body.NewPlanId, body.EffectiveDate), ct);

        return Ok(result);
    }

    [HttpPost("{id:guid}/change-billing-cycle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeBillingCycle(Guid id, [FromBody] ChangeBillingCycleRequest body, CancellationToken ct)
    {
        await _sender.Send(new ChangeBillingCycleCommand(id, body.BillingCycle), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Renew(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ProcessSubscriptionRenewalCommand(id), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/auto-renew")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetAutoRenew(Guid id, [FromBody] AutoRenewRequest body, CancellationToken ct)
    {
        await _sender.Send(new SetSubscriptionAutoRenewCommand(id, body.AutoRenew), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/external-ids")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetExternalIds(Guid id, [FromBody] ExternalIdsRequest body, CancellationToken ct)
    {
        await _sender.Send(new SetSubscriptionExternalIdsCommand(id, body.ExternalSubscriptionId, body.ExternalCustomerId), ct);

        return NoContent();
    }

    /// <summary>
    ///     Get subscription usage and limits
    /// </summary>
    [HttpGet("{id:guid}/usage")]
    [ProducesResponseType(typeof(SubscriptionUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsage(Guid id, CancellationToken ct)
    {
        var usage = await _sender.Send(new GetSubscriptionUsageQuery(id), ct);

        return usage is null ? NotFound() : Ok(usage);
    }

    /// <summary>
    ///     Get subscription billing history
    /// </summary>
    [HttpGet("{id:guid}/billing-history")]
    [ProducesResponseType(typeof(IEnumerable<BillingHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingHistory(Guid id, CancellationToken ct)
    {
        var history = await _sender.Send(new GetSubscriptionBillingHistoryQuery(id), ct);

        return Ok(history);
    }

    // POST /subscriptions
    public record CreateSubscriptionRequest(Guid TenantId, Guid PlanId, Guid CreatedByUserId, BillingCycle BillingCycle, decimal Amount, string Currency, DateTime? StartDate, int? TrialDays);

    public record StartTrialRequest(int TrialDays);

    public record EndTrialRequest(bool ConvertToPaid);

    public record CancelRequest(string Reason, string? Note, DateTime? EffectiveDate);

    public record SuspendRequest(string? Reason);

    public record UpgradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public record DowngradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public record ChangeBillingCycleRequest(BillingCycle BillingCycle);

    public record AutoRenewRequest(bool AutoRenew);

    public record ExternalIdsRequest(string? ExternalSubscriptionId, string? ExternalCustomerId);
}

