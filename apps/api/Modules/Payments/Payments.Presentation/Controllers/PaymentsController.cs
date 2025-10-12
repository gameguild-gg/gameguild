using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Modules.Payments.Features.ProcessPayment;
using GameGuild.Modules.Payments.Features.GetPayment;
using GameGuild.Modules.Payments.Features.CalculatePricing;
using GameGuild.Modules.Payments.Features.ManagePayment;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender) { _sender = sender; }

    // GET /payments/{id}
    [HttpGet("{id:guid}", Name = "GetPaymentById")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        PaymentResult? payment = await _sender.Send(new GetPaymentByIdQuery(id), ct);

        return payment is null ? NotFound() : Ok(payment);
    }

    // GET /payments/tenant/{tenantId}?startDate=&endDate=
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> History(Guid tenantId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        return Ok(await _sender.Send(new GetPaymentHistoryQuery(tenantId, startDate, endDate), ct));
    }

    // GET /payments/failed?tenantId=
    [HttpGet("failed")]
    public async Task<IActionResult> Failed([FromQuery] Guid? tenantId, CancellationToken ct) { return Ok(await _sender.Send(new GetFailedPaymentsQuery(tenantId), ct)); }

    // GET /payments/pricing?planId=&tenantId=&discountCode=
    [HttpGet("pricing")]
    public async Task<IActionResult> Pricing([FromQuery] Guid planId, [FromQuery] Guid? tenantId, [FromQuery] string? discountCode, CancellationToken ct)
    {
        return Ok(await _sender.Send(new CalculatePricingQuery(planId, tenantId, discountCode), ct));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Process([FromBody] ProcessPaymentRequest body, CancellationToken ct)
    {
        PaymentResult result = await _sender.Send(new ProcessPaymentCommand(body.TenantId, body.SubscriptionId, body.Amount, body.PaymentMethodId), ct);

        return CreatedAtRoute(
            "GetPaymentById",
            new
            {
                id = result.PaymentId
            },
            result
        );
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        PaymentRetryResult r = await _sender.Send(new RetryPaymentCommand(id), ct);

        return Ok(r);
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest body, CancellationToken ct)
    {
        PaymentResult r = await _sender.Send(new ProcessRefundCommand(id, body.Amount, body.Reason), ct);

        return Ok(r);
    }

    public record ProcessPaymentRequest(Guid TenantId, Guid SubscriptionId, decimal Amount, string PaymentMethodId);

    public record RefundRequest(decimal? Amount, string? Reason);
}

