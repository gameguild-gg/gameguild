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
public class PaymentController(IMediator mediator) : ControllerBase {
  /// <summary>
  /// Get current user's payment methods
  /// </summary>
  [HttpGet("methods/me")]
  public async Task<ActionResult<IEnumerable<UserFinancialMethod>>> GetMyPaymentMethods() {
    var query = new GetUserPaymentMethodsQuery();
    var methods = await mediator.Send(query);
    return Ok(methods);
  }

  /// <summary>
  /// Create a new payment intent
  /// </summary>
  [HttpPost("intent")]
  public async Task<ActionResult<Payment>> CreatePaymentIntent([FromBody] CreatePaymentCommand command) {
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }

  /// <summary>
  /// Process an existing payment
  /// </summary>
  [HttpPost("{id}/process")]
  public async Task<ActionResult<Payment>> ProcessPayment(Guid id, [FromBody] ProcessPaymentCommand command) {
    command.PaymentId = id;
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }

  /// <summary>
  /// Refund a payment
  /// </summary>
  [HttpPost("{id}/refund")]
  public async Task<ActionResult<Payment>> RefundPayment(Guid id, [FromBody] RefundPaymentCommand command) {
    command.PaymentId = id;
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Payment);
  }
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var paymentMethods = await mediator.Send(new GetUserPaymentMethodsQuery(userId));

    return Ok(paymentMethods);
  }

  /// <summary>
  /// Add a new payment method for current user
  /// </summary>
  [HttpPost("methods")]
  public async Task<ActionResult<UserFinancialMethod>> AddPaymentMethod([FromBody] CreatePaymentMethodDto createDto) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    if (!ModelState.IsValid) return BadRequest(ModelState);

    var paymentMethod = await mediator.Send(new CreatePaymentMethodCommand(userId, createDto));

    return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentMethod.Id }, paymentMethod);
  }

  /// <summary>
  /// Get payment method by ID
  /// </summary>
  [HttpGet("methods/{id:guid}")]
  public async Task<ActionResult<UserFinancialMethod>> GetPaymentMethod(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var paymentMethod = await mediator.Send(new GetPaymentMethodByIdQuery(id, userId));

    if (paymentMethod == null) return NotFound();

    return Ok(paymentMethod);
  }

  /// <summary>
  /// Delete a payment method
  /// </summary>
  [HttpDelete("methods/{id}")]
  public async Task<ActionResult> DeletePaymentMethod(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var result = await mediator.Send(new DeletePaymentMethodCommand(id, userId));

    if (!result) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Get current user's transaction history
  /// </summary>
  [HttpGet("transactions/me")]
  public async Task<ActionResult<IEnumerable<FinancialTransaction>>> GetMyTransactions(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] TransactionType? type = null,
    [FromQuery] TransactionStatus? status = null
  ) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var transactions = await mediator.Send(new GetUserTransactionsQuery(userId, skip, take, type, status));

    return Ok(transactions);
  }

  /// <summary>
  /// Get transaction by ID
  /// </summary>
  [HttpGet("transactions/{id}")]
  public async Task<ActionResult<FinancialTransaction>> GetTransaction(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var transaction = await mediator.Send(new GetTransactionByIdQuery(id, userId));

    if (transaction == null) return NotFound();

    return Ok(transaction);
  }

  /// <summary>
  /// Create a new payment transaction
  /// </summary>
  [HttpPost("transactions")]
  public async Task<ActionResult<FinancialTransaction>> CreateTransaction([FromBody] CreateTransactionDto createDto) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    if (!ModelState.IsValid) return BadRequest(ModelState);

    var transaction = await mediator.Send(new CreateTransactionCommand(userId, createDto));

    return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
  }

  /// <summary>
  /// Process a payment
  /// </summary>
  [HttpPost("transactions/{id}/process")]
  public async Task<ActionResult<FinancialTransaction>> ProcessPayment(Guid id, [FromBody] ProcessPaymentDto processDto) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    if (!ModelState.IsValid) return BadRequest(ModelState);

    var transaction = await mediator.Send(new ProcessPaymentCommand(id, userId, processDto));

    if (transaction == null) return NotFound();

    return Ok(transaction);
  }

  /// <summary>
  /// Get all transactions (admin only)
  /// </summary>
  [HttpGet("transactions")]
  [RequireTenantPermission(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<FinancialTransaction>>> GetAllTransactions(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] TransactionType? type = null,
    [FromQuery] TransactionStatus? status = null
  ) {
    var transactions = await mediator.Send(new GetAllTransactionsQuery(skip, take, type, status));

    return Ok(transactions);
  }

  /// <summary>
  /// Get payment statistics (admin only)
  /// </summary>
  [HttpGet("statistics")]
  [RequireTenantPermission(PermissionType.Read)]
  public async Task<ActionResult<PaymentStatisticsDto>> GetPaymentStatistics(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null
  ) {
    var statistics = await mediator.Send(new GetPaymentStatisticsQuery(fromDate, toDate));

    return Ok(statistics);
  }
}
