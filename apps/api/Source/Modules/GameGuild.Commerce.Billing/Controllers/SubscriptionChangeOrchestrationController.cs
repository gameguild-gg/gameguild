using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Orchestration entry point for the subscription plan change saga (issue #396
///     event-driven workflow orchestration): one command drives the multi-step billing
///     flow — proration, invoice, payment, activation — with journaled steps, durable
///     step events and compensating actions on failure. The acting user is resolved from
///     the ambient actor context, never from the request body.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/subscriptions/{subscriptionId:guid}")]
[Microsoft.AspNetCore.Http.Tags("billing/subscription-changes")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
public sealed class SubscriptionChangeOrchestrationController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Run the orchestrated plan change for one subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription to change.</param>
    /// <param name="request">Target plan, required explicit UTC effective date and optional payment method override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The terminal saga state with every executed step and any compensations applied.</returns>
    [HttpPost("plan-change:orchestrate")]
    [EndpointSummary("Run the orchestrated subscription plan change saga")]
    [EndpointDescription(
        "Orchestrates the plan change as a saga: proration, invoice, confirmed gateway payment and activation. EffectiveDateUtc is required as an explicit UTC timestamp and must be reused unchanged on retries. Local state and required billing events commit together. Activation requires provider-verified settlement amount and currency; providers without that evidence require reconciliation. Confirmed business failures trigger compensation; uncertain persistence or provider outcomes require reconciliation and retry with the same effective date and payment parameters.")]
    [ProducesResponseType<SubscriptionChangeSagaResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OrchestratePlanChange(
        Guid subscriptionId,
        [FromBody] OrchestrateSubscriptionPlanChangeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new OrchestrateSubscriptionPlanChangeCommand(
                subscriptionId,
                request.NewPlanId ?? Guid.Empty,
                request.EffectiveDateUtc,
                request.PaymentMethodId),
            ct).ConfigureAwait(false);
        return Ok(result);
    }
}
