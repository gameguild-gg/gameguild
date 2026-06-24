using Asp.Versioning;
using GameGuild.Commerce;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Payments;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/charges")]
[Microsoft.AspNetCore.Http.Tags("billing/charges")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class BillingChargesController(ISender sender, IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("List billing charges")]
    [EndpointDescription("Compatibility billing endpoint backed by the persisted payment query model.")]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCharges(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        NormalizePaging(ref page, ref pageSize);

        var result = await sender.Send(new GetAllPaymentsQuery(tenantId, status, startDate, endDate, page, pageSize), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("{chargeId:guid}", Name = "GetBillingChargeById")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Get billing charge")]
    [ProducesResponseType<PaymentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChargeById(Guid chargeId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentByIdQuery(chargeId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [EndpointSummary("Create billing charge")]
    [EndpointDescription("Processes a subscription charge through the configured payment command path.")]
    [ProducesResponseType<PaymentResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCharge([FromBody] CreateBillingChargeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.TenantId == Guid.Empty) return BadRequest(new { error = "TenantId cannot be empty" });
        if (body.SubscriptionId == Guid.Empty) return BadRequest(new { error = "SubscriptionId cannot be empty" });
        if (body.Amount <= 0) return BadRequest(new { error = "Amount must be greater than zero" });
        if (string.IsNullOrWhiteSpace(body.PaymentMethodId)) return BadRequest(new { error = "PaymentMethodId is required" });

        var validationError = actorContextAccessor.ValidateTenantAccessAsActionResult(body.TenantId, "create billing charge");
        if (validationError != null) return validationError;

        var result = await sender.Send(new ProcessPaymentCommand(body.TenantId, body.SubscriptionId, body.Amount, body.PaymentMethodId), ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetChargeById), new { chargeId = result.PaymentId }, result);
    }

    [HttpPost("{chargeId:guid}:retry")]
    [EndpointSummary("Retry billing charge")]
    [ProducesResponseType<PaymentRetryResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryCharge(Guid chargeId, CancellationToken ct)
    {
        var result = await sender.Send(new RetryPaymentCommand(chargeId), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("{chargeId:guid}:refund")]
    [EndpointSummary("Refund billing charge")]
    [ProducesResponseType<ProcessRefundResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefundCharge(Guid chargeId, [FromBody] RefundBillingChargeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new ProcessRefundCommand(chargeId, body.Amount ?? 0, body.Reason ?? "No reason provided"), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("{chargeId:guid}:cancel")]
    [EndpointSummary("Cancel billing charge")]
    [ProducesResponseType<PaymentCancellationResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelCharge(Guid chargeId, [FromBody] CancelBillingChargeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new CancelPaymentCommand(chargeId, body.CancellationReason, body.CanceledBy), ct).ConfigureAwait(false);
        return Ok(result);
    }

    private static void NormalizePaging(ref int page, ref int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
    }

    public sealed record CreateBillingChargeRequest(Guid TenantId, Guid SubscriptionId, decimal Amount, string PaymentMethodId);

    public sealed record RefundBillingChargeRequest(decimal? Amount, string? Reason);

    public sealed record CancelBillingChargeRequest(string CancellationReason, Guid? CanceledBy = null);
}
