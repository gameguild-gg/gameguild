using GameGuild.Common;
using GameGuild.Modules.Payments.Models;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Queries;
using GameGuild.Modules.Permissions.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Payments.Controllers;

/// <summary>
/// REST API controller for managing payments using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController(IMediator mediator) : ControllerBase 
{
  /// <summary>
  /// Get current user's payment methods
  /// </summary>
  [HttpGet("methods/me")]
  public async Task<ActionResult<IEnumerable<UserFinancialMethod>>> GetMyPaymentMethods() 
  {
    var query = new GetUserPaymentMethodsQuery();
    var methods = await mediator.Send(query);
    return Ok(methods);
  }

  /// <summary>
  /// Create a new payment intent
  /// </summary>
  [HttpPost("intent")]
  public async Task<ActionResult<Payment>> CreatePaymentIntent([FromBody] CreatePaymentCommand command) 
  {
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }

  /// <summary>
  /// Process an existing payment
  /// </summary>
  [HttpPost("{id}/process")]
  public async Task<ActionResult<Payment>> ProcessPayment(Guid id, [FromBody] ProcessPaymentCommand command) 
  {
    command.PaymentId = id;
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }

  /// <summary>
  /// Refund a payment
  /// </summary>
  [HttpPost("{id}/refund")]
  public async Task<ActionResult<Payment>> RefundPayment(Guid id, [FromBody] RefundPaymentCommand command) 
  {
    command.PaymentId = id;
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }

  /// <summary>
  /// Get payment by ID
  /// </summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<Payment>> GetPayment(Guid id)
  {
    var query = new GetPaymentByIdQuery { PaymentId = id };
    var payment = await mediator.Send(query);
    if (payment == null) return NotFound();
    return Ok(payment);
  }

  /// <summary>
  /// Get user's payment history
  /// </summary>
  [HttpGet("user/{userId:guid}")]
  public async Task<ActionResult<IEnumerable<Payment>>> GetUserPayments(Guid userId)
  {
    var query = new GetUserPaymentsQuery { UserId = userId };
    var payments = await mediator.Send(query);
    return Ok(payments);
  }

  /// <summary>
  /// Get payment statistics
  /// </summary>
  [HttpGet("stats")]
  public async Task<ActionResult<PaymentStats>> GetPaymentStats()
  {
    var query = new GetPaymentStatsQuery();
    var stats = await mediator.Send(query);
    return Ok(stats);
  }
}
