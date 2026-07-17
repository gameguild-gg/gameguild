using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Subscription Lifecycle Controller - manages subscription state transitions and plan changes.
///     Handles activation, trials, cancellation, suspension, pausing/resuming,
///     plan upgrades/downgrades, renewal, and external ID integration.
///     All endpoints require authentication.
///     Rate limiting uses ExpensiveOperations policy for mutations.
/// </summary>
[ApiVersion("1.0")]
[Route("api")]
[Microsoft.AspNetCore.Http.Tags("commerce/subscriptions")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class SubscriptionLifecycleController(ISender sender) : BaseApiController
{
    #region Activation & Trial

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
        await sender.Send(new ActivateSubscriptionCommand(subscriptionId), ct).ConfigureAwait(false);
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
        await sender.Send(new StartSubscriptionTrialCommand(subscriptionId, body.TrialDays), ct).ConfigureAwait(false);
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
        await sender.Send(new EndSubscriptionTrialCommand(subscriptionId, body.ConvertToPaid), ct).ConfigureAwait(false);
        return NoContent();
    }

    #endregion

    #region Cancellation & Suspension

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
        await sender.Send(new CancelSubscriptionCommand(subscriptionId, Enum.Parse<CancellationReason>(body.Reason, true), body.Note, body.EffectiveDate), ct).ConfigureAwait(false);
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
        await sender.Send(new SuspendSubscriptionCommand(subscriptionId, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Pause subscription billing
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="body">Pause configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:pause")]
    [EndpointSummary("Pause subscription billing")]
    [EndpointDescription("Pauses billing for a subscription while keeping the subscription active. Useful for temporary payment holds.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PauseSubscription(Guid subscriptionId, [FromBody] PauseSubscriptionRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new PauseSubscriptionCommand(subscriptionId, body.PauseUntil, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Resume paused subscription billing
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscriptions/{subscriptionId:guid}:resume")]
    [EndpointSummary("Resume subscription billing")]
    [EndpointDescription("Resumes billing for a paused subscription.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResumeSubscription(Guid subscriptionId, CancellationToken ct)
    {
        await sender.Send(new ResumeSubscriptionCommand(subscriptionId), ct).ConfigureAwait(false);
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
        await sender.Send(new ReactivateSubscriptionCommand(subscriptionId), ct).ConfigureAwait(false);
        return NoContent();
    }

    #endregion

    #region Plan Changes & Renewal

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
        var result = await sender.Send(new UpgradeSubscriptionPlanCommand(subscriptionId, body.NewPlanId, body.EffectiveDate), ct).ConfigureAwait(false);
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
        var result = await sender.Send(new DowngradeSubscriptionPlanCommand(subscriptionId, body.NewPlanId, body.EffectiveDate), ct).ConfigureAwait(false);
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
        try
        {
            await sender.Send(new ProcessSubscriptionRenewalCommand(subscriptionId), ct).ConfigureAwait(false);
            return NoContent();
        }
        catch (RequestValidationException exception)
        {
            return BadRequest(new { errors = exception.Errors });
        }
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
        await sender.Send(new SetSubscriptionAutoRenewCommand(subscriptionId, body.AutoRenew), ct).ConfigureAwait(false);
        return NoContent();
    }

    #endregion

    #region External Integration

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
        await sender.Send(new SetSubscriptionExternalIdsCommand(subscriptionId, body.ExternalSubscriptionId, body.ExternalCustomerId), ct).ConfigureAwait(false);
        return NoContent();
    }

    #endregion

    #region Request DTOs

    public sealed record StartTrialRequest(int TrialDays);

    public sealed record EndTrialRequest(bool ConvertToPaid);

    public sealed record CancelRequest(string Reason, string? Note, DateTime? EffectiveDate);

    public sealed record SuspendRequest(string? Reason);

    public sealed record UpgradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public sealed record DowngradeRequest(Guid NewPlanId, DateTime? EffectiveDate);

    public sealed record ChangeBillingCycleRequest(BillingCycle BillingCycle);

    public sealed record AutoRenewRequest(bool AutoRenew);

    public sealed record ExternalIdsRequest(string? ExternalSubscriptionId, string? ExternalCustomerId);

    /// <summary>Request to pause subscription billing</summary>
    public sealed record PauseSubscriptionRequest(DateTime? PauseUntil = null, string? Reason = null);

    #endregion
}
