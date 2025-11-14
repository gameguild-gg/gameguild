using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Subscriptions.Commands;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;
using GameGuild.Subscriptions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Subscriptions.Controllers;

/// <summary>
///     Subscriptions API Controller - RESTful API for subscription management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "subscriptions")]
[Tags("subscriptions")]
public sealed class SubscriptionsController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/subscriptions

    /// <summary>
    ///     Create a new subscription
    /// </summary>
    /// <param name="body">Subscription creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created subscription</returns>
    [AllowAnonymous]
    [HttpPost("api/v{version:apiVersion}/subscriptions")]
    [EndpointSummary("Create a new subscription")]
    [EndpointDescription("Creates a new subscription with the provided information.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(new CreateSubscriptionCommand(body.TenantId, body.PlanId, body.CreatedByUserId, body.BillingCycle, body.Amount, body.StartDate, body.TrialDays), ct);

        return CreatedAtAction(nameof(GetSubscriptionById), new { subscriptionId = id }, new { id });
    }

    /// <summary>
    ///     Get subscriptions with pagination, search, and filtering
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of subscriptions per page (default: 20, max: 100)</param>
    /// <param name="status">Filter by status</param>
    /// <param name="tenantId">Filter by tenant ID</param>
    /// <param name="planId">Filter by plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of subscriptions</returns>
    [AllowAnonymous]
    [HttpGet("api/v{version:apiVersion}/subscriptions")]
    [EndpointSummary("Get subscriptions with pagination, search, and filtering")]
    [EndpointDescription("Retrieves a paginated list of subscriptions with optional filtering.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? planId = null,
        CancellationToken ct = default
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await sender.Send(new GetPagedSubscriptionsQuery(page, pageSize, status, tenantId, planId), ct);
        return Ok(result);
    }

    /// <summary>
    ///     Get subscriptions by tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of subscriptions for the tenant</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/tenant/{tenantId:guid}")]
    [EndpointSummary("Get subscriptions by tenant")]
    [EndpointDescription("Retrieves all subscriptions for a specific tenant.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionsByTenant(Guid tenantId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetTenantSubscriptionsQuery(tenantId), ct));
    }

    /// <summary>
    ///     Get active subscription for tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active subscription for the tenant</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/tenant/{tenantId:guid}/active")]
    [EndpointSummary("Get active subscription for tenant")]
    [EndpointDescription("Retrieves the active subscription for a specific tenant.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveSubscriptionByTenant(Guid tenantId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetActiveTenantSubscriptionQuery(tenantId), ct));
    }

    /// <summary>
    ///     Get subscriptions by plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of subscriptions for the plan</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/plan/{planId:guid}")]
    [EndpointSummary("Get subscriptions by plan")]
    [EndpointDescription("Retrieves all subscriptions for a specific plan.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionsByPlan(Guid planId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetSubscriptionsByPlanQuery(planId), ct));
    }

    /// <summary>
    ///     Get subscriptions by status
    /// </summary>
    /// <param name="status">Subscription status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of subscriptions with the specified status</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/status/{status}")]
    [EndpointSummary("Get subscriptions by status")]
    [EndpointDescription("Retrieves all subscriptions with a specific status.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionsByStatus(SubscriptionStatus status, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetSubscriptionsByStatusQuery(status), ct));
    }

    /// <summary>
    ///     Get subscription metrics
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription metrics</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/metrics")]
    [EndpointSummary("Get subscription metrics")]
    [EndpointDescription("Retrieves subscription metrics and analytics.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionMetrics(CancellationToken ct)
    {
        // TODO: Implement proper metrics functionality
        return Ok(new { TotalSubscriptions = 0, ActiveSubscriptions = 0, Revenue = 0 });
    }

    /// <summary>
    ///     Get expiring subscriptions
    /// </summary>
    /// <param name="days">Number of days to look ahead (default: 30)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of expiring subscriptions</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/expiring")]
    [EndpointSummary("Get expiring subscriptions")]
    [EndpointDescription("Retrieves subscriptions that are expiring within the specified number of days.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringSubscriptions([FromQuery] int days = 30, CancellationToken ct = default)
    {
        // TODO: Implement proper expiring subscriptions functionality
        return Ok(new List<object>());
    }

    #endregion

    #region Individual Item Operations - /v1/subscriptions/{subscriptionId}

    /// <summary>
    ///     Check if subscription exists by ID
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}")]
    [EndpointSummary("Check if subscription exists by ID")]
    [EndpointDescription("Checks if a subscription exists by ID without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckSubscriptionExistsById(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await sender.Send(new GetSubscriptionByIdQuery(subscriptionId), ct);
        return subscription is null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Get subscription by ID
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription details</returns>
    [AllowAnonymous]
    [HttpGet("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}")]
    [EndpointSummary("Get subscription by ID")]
    [EndpointDescription("Retrieves detailed information for a specific subscription.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionById(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await sender.Send(new GetSubscriptionByIdQuery(subscriptionId), ct);
        return subscription is null ? NotFound() : Ok(subscription);
    }

    /// <summary>
    ///     Get subscription usage and limits
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription usage information</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}/usage")]
    [EndpointSummary("Get subscription usage and limits")]
    [EndpointDescription("Retrieves usage information and limits for a specific subscription.")]
    [ProducesResponseType<SubscriptionUsageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionUsage(Guid subscriptionId, CancellationToken ct)
    {
        var usage = await sender.Send(new GetSubscriptionUsageQuery(subscriptionId), ct);
        return usage is null ? NotFound() : Ok(usage);
    }

    /// <summary>
    ///     Get subscription billing history
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Billing history for the subscription</returns>
    [HttpGet("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}/billing-history")]
    [EndpointSummary("Get subscription billing history")]
    [EndpointDescription("Retrieves billing history for a specific subscription.")]
    [ProducesResponseType<IEnumerable<BillingHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionBillingHistory(Guid subscriptionId, CancellationToken ct)
    {
        var history = await sender.Send(new GetSubscriptionBillingHistoryQuery(subscriptionId), ct);
        return Ok(history);
    }

    #endregion

    #region Individual Subscription Actions - /v1/subscriptions/{subscriptionId}:action

    /// <summary>
    ///     Activate subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [AllowAnonymous]
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:activate")]
    [EndpointSummary("Activate subscription")]
    [EndpointDescription("Activates a subscription by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateSubscription(Guid subscriptionId, CancellationToken ct)
    {
        await sender.Send(new ActivateSubscriptionCommand(subscriptionId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Start subscription trial
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Trial configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:start-trial")]
    [EndpointSummary("Start subscription trial")]
    [EndpointDescription("Starts a trial period for a subscription.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartSubscriptionTrial(Guid subscriptionId, [FromBody] StartTrialRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new StartSubscriptionTrialCommand(subscriptionId, body.TrialDays), ct);
        return NoContent();
    }

    /// <summary>
    ///     End subscription trial
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Trial ending configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:end-trial")]
    [EndpointSummary("End subscription trial")]
    [EndpointDescription("Ends a trial period for a subscription.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndSubscriptionTrial(Guid subscriptionId, [FromBody] EndTrialRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new EndSubscriptionTrialCommand(subscriptionId, body.ConvertToPaid), ct);
        return NoContent();
    }

    /// <summary>
    ///     Cancel subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Cancellation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [AllowAnonymous]
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:cancel")]
    [EndpointSummary("Cancel subscription")]
    [EndpointDescription("Cancels a subscription with specified reason and effective date.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSubscription(Guid subscriptionId, [FromBody] CancelRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new CancelSubscriptionCommand(subscriptionId, Enum.Parse<CancellationReason>(body.Reason, true), body.Note, body.EffectiveDate), ct);
        return NoContent();
    }

    /// <summary>
    ///     Suspend subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Suspension details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:suspend")]
    [EndpointSummary("Suspend subscription")]
    [EndpointDescription("Suspends a subscription temporarily.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuspendSubscription(Guid subscriptionId, [FromBody] SuspendRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new SuspendSubscriptionCommand(subscriptionId, body.Reason), ct);
        return NoContent();
    }

    /// <summary>
    ///     Reactivate subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:reactivate")]
    [EndpointSummary("Reactivate subscription")]
    [EndpointDescription("Reactivates a suspended or cancelled subscription.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReactivateSubscription(Guid subscriptionId, CancellationToken ct)
    {
        await sender.Send(new ReactivateSubscriptionCommand(subscriptionId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Upgrade subscription plan
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Upgrade details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Upgrade result</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:upgrade")]
    [EndpointSummary("Upgrade subscription plan")]
    [EndpointDescription("Upgrades a subscription to a higher-tier plan.")]
    [ProducesResponseType<SubscriptionUpgradeResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpgradeSubscription(Guid subscriptionId, [FromBody] UpgradeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new UpgradeSubscriptionPlanCommand(subscriptionId, body.NewPlanId, body.EffectiveDate), ct);
        return Ok(result);
    }

    /// <summary>
    ///     Downgrade subscription plan
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Downgrade details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Downgrade result</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:downgrade")]
    [EndpointSummary("Downgrade subscription plan")]
    [EndpointDescription("Downgrades a subscription to a lower-tier plan.")]
    [ProducesResponseType<SubscriptionDowngradeResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DowngradeSubscription(Guid subscriptionId, [FromBody] DowngradeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new DowngradeSubscriptionPlanCommand(subscriptionId, body.NewPlanId, body.EffectiveDate), ct);
        return Ok(result);
    }

    /// <summary>
    ///     Renew subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:renew")]
    [EndpointSummary("Renew subscription")]
    [EndpointDescription("Manually renews a subscription for another billing cycle.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenewSubscription(Guid subscriptionId, CancellationToken ct)
    {
        await sender.Send(new ProcessSubscriptionRenewalCommand(subscriptionId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Set subscription auto-renew
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Auto-renew configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:auto-renew")]
    [EndpointSummary("Set subscription auto-renew")]
    [EndpointDescription("Enables or disables auto-renewal for a subscription.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSubscriptionAutoRenew(Guid subscriptionId, [FromBody] AutoRenewRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new SetSubscriptionAutoRenewCommand(subscriptionId, body.AutoRenew), ct);
        return NoContent();
    }

    /// <summary>
    ///     Set subscription external IDs
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">External IDs configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("api/v{version:apiVersion}/subscriptions/{subscriptionId:guid}:external-ids")]
    [EndpointSummary("Set subscription external IDs")]
    [EndpointDescription("Sets external system IDs for subscription integration.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSubscriptionExternalIds(Guid subscriptionId, [FromBody] ExternalIdsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new SetSubscriptionExternalIdsCommand(subscriptionId, body.ExternalSubscriptionId, body.ExternalCustomerId), ct);
        return NoContent();
    }

    #endregion

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
