using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Queries;
using GameGuild.Modules.Payments.Models;
using GameGuild.Common.Interfaces;

namespace GameGuild.Modules.Payments.Controllers;

/// <summary>
/// REST API controller for payment operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        IUserContext userContext,
        ITenantContext tenantContext,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment intent
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreatePaymentResult>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var command = new CreatePaymentCommand
        {
            UserId = request.UserId ?? _userContext.UserId ?? Guid.Empty,
            ProductId = request.ProductId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Method = request.Method,
            Description = request.Description,
            TenantId = request.TenantId ?? _tenantContext.TenantId,
            Metadata = request.Metadata
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetPayment), new { id = result.Payment!.Id }, result);
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var query = new GetPaymentByIdQuery
        {
            PaymentId = id,
            UserId = _userContext.UserId
        };

        var payment = await _mediator.Send(query);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    /// <summary>
    /// Get current user's payments
    /// </summary>
    [HttpGet("my-payments")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetMyPayments(
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (_userContext.UserId == null)
        {
            return Unauthorized();
        }

        var query = new GetUserPaymentsQuery
        {
            UserId = _userContext.UserId.Value,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Skip = skip,
            Take = Math.Min(take, 100)
        };

        var payments = await _mediator.Send(query);
        return Ok(payments);
    }

    /// <summary>
    /// Get payments for a specific user (admin only)
    /// </summary>
    [HttpGet("users/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetUserPayments(
        Guid userId,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetUserPaymentsQuery
        {
            UserId = userId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Skip = skip,
            Take = Math.Min(take, 100)
        };

        var payments = await _mediator.Send(query);
        return Ok(payments);
    }

    /// <summary>
    /// Get payments for a product (admin only)
    /// </summary>
    [HttpGet("products/{productId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetProductPayments(
        Guid productId,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetProductPaymentsQuery
        {
            ProductId = productId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Skip = skip,
            Take = Math.Min(take, 100)
        };

        var payments = await _mediator.Send(query);
        return Ok(payments);
    }

    /// <summary>
    /// Process a payment (webhook endpoint)
    /// </summary>
    [HttpPost("{id}/process")]
    [AllowAnonymous] // Webhooks from payment providers
    public async Task<ActionResult<ProcessPaymentResult>> ProcessPayment(
        Guid id, 
        [FromBody] ProcessPaymentRequest request)
    {
        // TODO: Add webhook signature validation here
        var command = new ProcessPaymentCommand
        {
            PaymentId = id,
            ProviderTransactionId = request.ProviderTransactionId,
            ProviderMetadata = request.ProviderMetadata
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin,Customer")] // Customers can refund their own payments with restrictions
    public async Task<ActionResult<RefundPaymentResult>> RefundPayment(
        Guid id, 
        [FromBody] RefundPaymentRequest request)
    {
        var command = new RefundPaymentCommand
        {
            PaymentId = id,
            RefundAmount = request.RefundAmount,
            Reason = request.Reason ?? "Customer requested refund",
            RefundedBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel a payment
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<CancelPaymentResult>> CancelPayment(
        Guid id, 
        [FromBody] CancelPaymentRequest request)
    {
        var command = new CancelPaymentCommand
        {
            PaymentId = id,
            Reason = request.Reason ?? "Payment cancelled by user",
            CancelledBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get payment statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<PaymentStats>> GetPaymentStats(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPaymentStatsQuery
        {
            UserId = userId,
            ProductId = productId,
            FromDate = fromDate,
            ToDate = toDate,
            TenantId = _tenantContext.TenantId
        };

        var stats = await _mediator.Send(query);
        return Ok(stats);
    }

    /// <summary>
    /// Get revenue report (admin only)
    /// </summary>
    [HttpGet("revenue-report")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RevenueReport>> GetRevenueReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string groupBy = "day",
        [FromQuery] Guid? productId = null)
    {
        var query = new GetRevenueReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            GroupBy = groupBy,
            ProductId = productId,
            TenantId = _tenantContext.TenantId
        };

        var report = await _mediator.Send(query);
        return Ok(report);
    }
}

/// <summary>
/// Request DTOs for REST API
/// </summary>
public record CreatePaymentRequest
{
    public Guid? UserId { get; init; }
    public Guid? ProductId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Description { get; init; }
    public Guid? TenantId { get; init; }
    public IDictionary<string, object>? Metadata { get; init; }
}

public record ProcessPaymentRequest
{
    public string ProviderTransactionId { get; init; } = string.Empty;
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public record RefundPaymentRequest
{
    public decimal? RefundAmount { get; init; }
    public string? Reason { get; init; }
}

public record CancelPaymentRequest
{
    public string? Reason { get; init; }
}
