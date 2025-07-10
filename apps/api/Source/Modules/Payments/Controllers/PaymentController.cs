using GameGuild.Common;
using GameGuild.Modules.Payments.Models;
using GameGuild.Modules.Payments.Services;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Payments.Controllers;

/// <summary>
/// REST API controller for managing payments and financial transactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController(IPaymentService paymentService) : ControllerBase {
  /// <summary>
  /// Get current user's payment methods
  /// </summary>
  [HttpGet("methods/me")]
  public async Task<ActionResult<IEnumerable<UserFinancialMethod>>> GetMyPaymentMethods() {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var paymentMethods = await paymentService.GetUserPaymentMethodsAsync(userId);

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

    var paymentMethod = await paymentService.CreatePaymentMethodAsync(userId, createDto);

    return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentMethod.Id }, paymentMethod);
  }

  /// <summary>
  /// Get payment method by ID
  /// </summary>
  [HttpGet("methods/{id:guid}")]
  public async Task<ActionResult<UserFinancialMethod>> GetPaymentMethod(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var paymentMethod = await paymentService.GetPaymentMethodByIdAsync(id, userId);

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

    var result = await paymentService.DeletePaymentMethodAsync(id, userId);

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

    var transactions = await paymentService.GetUserTransactionsAsync(userId, skip, take, type, status);

    return Ok(transactions);
  }

  /// <summary>
  /// Get transaction by ID
  /// </summary>
  [HttpGet("transactions/{id}")]
  public async Task<ActionResult<FinancialTransaction>> GetTransaction(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "User ID not found in token" });

    var transaction = await paymentService.GetTransactionByIdAsync(id, userId);

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

    var transaction = await paymentService.CreateTransactionAsync(userId, createDto);

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

    var transaction = await paymentService.ProcessPaymentAsync(id, userId, processDto);

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
    var transactions = await paymentService.GetAllTransactionsAsync(skip, take, type, status);

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
    var statistics = await paymentService.GetPaymentStatisticsAsync(fromDate, toDate);

    return Ok(statistics);
  }
}
