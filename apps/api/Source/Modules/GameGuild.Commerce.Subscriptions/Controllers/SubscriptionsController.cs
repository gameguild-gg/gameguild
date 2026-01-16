using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Subscriptions API Controller - RESTful API for subscription management.
///     All endpoints require authentication to protect sensitive subscription data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Tags("subscriptions")]
[Authorize]
public sealed class SubscriptionsController(ISender sender, IActorContextAccessor actorContextAccessor) : ControllerBase
{
    #region Collection Operations - /v1/subscriptions

    /// <summary>
    ///     Create a new subscription
    /// </summary>
    /// <param name="body">Subscription creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created subscription</returns>
    [HttpPost("v{version:apiVersion}/subscriptions")]
    [EndpointSummary("Create a new subscription")]
    [EndpointDescription("Creates a new subscription with the provided information.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        // SECURITY: Validate TenantId from authenticated context (prevents cross-tenant attack)
        var validationError = ValidateTenantAccess(body.TenantId, "create subscription");
        if (validationError != null) return validationError;

        var id = await sender.Send(new CreateSubscriptionCommand(
            body.TenantId, 
            body.PlanId, 
            body.CreatedByUserId, 
            body.BillingCycle, 
            body.Amount, 
            FulfilledOrderId: body.FulfilledOrderId,
            StartDate: body.StartDate, 
            TrialDays: body.TrialDays), ct);

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
    /// <param name="expiring">Filter for expiring subscriptions (within specified days)</param>
    /// <param name="expiringDays">Number of days to look ahead for expiring subscriptions (default: 30)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of subscriptions</returns>
    [HttpGet("v{version:apiVersion}/subscriptions")]
    [EndpointSummary("Get subscriptions with pagination, search, and filtering")]
    [EndpointDescription("Retrieves a paginated list of subscriptions with optional filtering. Use query parameters: status (active, trialing, cancelled, etc.), tenantId, planId, and expiring=true for expiring subscriptions.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? planId = null,
        [FromQuery] bool? expiring = null,
        [FromQuery] int expiringDays = 30,
        CancellationToken ct = default
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // If expiring filter is set, delegate to expiring subscriptions query
        if (expiring == true)
        {
            var expiringResult = await sender.Send(new GetExpiringSubscriptionsQuery(expiringDays), ct);
            return Ok(expiringResult);
        }

        var result = await sender.Send(new GetPagedSubscriptionsQuery(page, pageSize, status, tenantId, planId), ct);
        return Ok(result);
    }

    /// <summary>
    ///     Get subscription metrics
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription metrics</returns>
    [HttpGet("v{version:apiVersion}/subscriptions:get-metrics")]
    [EndpointSummary("Get subscription metrics")]
    [EndpointDescription("Retrieves subscription metrics and analytics.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionMetrics(CancellationToken ct)
    {
        // Get metrics via repository queries
        var statusCounts = await sender.Send(new GetSubscriptionStatusCountsQuery(), ct);
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Calculate metrics from status counts
        var totalSubscriptions = statusCounts.Values.Sum();
        var activeSubscriptions = statusCounts.GetValueOrDefault(SubscriptionStatus.Active, 0);
        var trialingSubscriptions = statusCounts.GetValueOrDefault(SubscriptionStatus.Trialing, 0);
        var pastDueSubscriptions = statusCounts.GetValueOrDefault(SubscriptionStatus.PastDue, 0);
        var cancelledSubscriptions = statusCounts.GetValueOrDefault(SubscriptionStatus.Cancelled, 0);

        return Ok(new
        {
            TotalSubscriptions = totalSubscriptions,
            ActiveSubscriptions = activeSubscriptions,
            TrialingSubscriptions = trialingSubscriptions,
            PastDueSubscriptions = pastDueSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            StatusBreakdown = statusCounts,
            ReportGeneratedAt = DateTime.UtcNow
        });
    }

    #endregion

    #region Individual Item Operations - /v1/subscriptions/{subscriptionId}

    /// <summary>
    ///     Check if subscription exists by ID
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("v{version:apiVersion}/subscriptions/{subscriptionId:guid}")]
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
    [HttpGet("v{version:apiVersion}/subscriptions/{subscriptionId:guid}")]
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
    [HttpGet("v{version:apiVersion}/subscriptions/{subscriptionId:guid}/usage")]
    [EndpointSummary("Get subscription usage and limits")]
    [EndpointDescription("Retrieves usage information and limits for a specific subscription.")]
    [ProducesResponseType<SubscriptionUsageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionUsage(Guid subscriptionId, CancellationToken ct)
    {
        var usage = await sender.Send(new GetSubscriptionUsageQuery(subscriptionId), ct);
        return Ok(usage);
    }

    /// <summary>
    ///     Get subscription billing history
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Billing history for the subscription</returns>
    [HttpGet("v{version:apiVersion}/subscriptions/{subscriptionId:guid}/billing-history")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:activate")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:start-trial")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:end-trial")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:cancel")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:suspend")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:reactivate")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:upgrade")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:downgrade")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:renew")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:auto-renew")]
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
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:external-ids")]
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
    /// <summary>Request to create a subscription</summary>
    /// <param name="TenantId">The tenant ID</param>
    /// <param name="PlanId">The subscription plan ID</param>
    /// <param name="CreatedByUserId">The user who created the subscription</param>
    /// <param name="BillingCycle">The billing cycle (Monthly, Yearly, etc.)</param>
    /// <param name="Amount">The subscription amount</param>
    /// <param name="Currency">The currency code</param>
    /// <param name="FulfilledOrderId">Optional Order ID that triggered this subscription (Economic Model: Order→Subscription causality)</param>
    /// <param name="StartDate">Optional start date</param>
    /// <param name="TrialDays">Optional trial period in days</param>
    public record CreateSubscriptionRequest(
        Guid TenantId, 
        Guid PlanId, 
        Guid CreatedByUserId, 
        BillingCycle BillingCycle, 
        decimal Amount, 
        string Currency, 
        Guid? FulfilledOrderId = null,
        DateTime? StartDate = null, 
        int? TrialDays = null);

    public record StartTrialRequest(int TrialDays);

    public record EndTrialRequest(bool ConvertToPaid);

    public record CancelRequest(string Reason, string? Note, DateTime? EffectiveDate);

    public record SuspendRequest(string? Reason);

    public record UpgradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public record DowngradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public record ChangeBillingCycleRequest(BillingCycle BillingCycle);

    public record AutoRenewRequest(bool AutoRenew);

    public record ExternalIdsRequest(string? ExternalSubscriptionId, string? ExternalCustomerId);

    #region Private Methods

    /// <summary>
    ///     Validates that the authenticated user has access to the specified tenant.
    ///     Uses shared TenantValidationExtensions for DRY compliance.
    /// </summary>
    private IActionResult? ValidateTenantAccess(Guid requestedTenantId, string operation)
        => actorContextAccessor.ValidateTenantAccessAsActionResult(requestedTenantId, operation);

    #endregion
}
