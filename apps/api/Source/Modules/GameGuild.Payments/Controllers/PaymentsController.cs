using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Payments.Commands;
using GameGuild.Payments.Models;
using GameGuild.Payments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Payments.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Retrieve all payment transactions with optional filtering
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="status">Optional payment status filter (pending, completed, failed, cancelled, refunded)</param>
    /// <param name="startDate">Optional start date filter for payments (inclusive)</param>
    /// <param name="endDate">Optional end date filter for payments (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of payment transactions matching the specified criteria</returns>
    /// <remarks>
    ///     Retrieves a paginated list of all payment transactions with support for filtering by tenant, status,
    ///     and date range. This is the primary endpoint for payment administration and reporting.
    ///     Supported status filters:
    ///     - pending: Payments currently being processed
    ///     - completed: Successfully processed payments
    ///     - failed: Payments that encountered errors
    ///     - cancelled: Payments cancelled before completion
    ///     - refunded: Payments that have been refunded
    ///     Query Parameters:
    ///     - tenantId: Filter payments for specific tenant
    ///     - status: Filter by payment status
    ///     - startDate: Include payments from this date onwards (ISO 8601 format)
    ///     - endDate: Include payments up to this date (ISO 8601 format)
    ///     - page: Pagination page number (1-based)
    ///     - pageSize: Items per page (1-100)
    ///     Use cases:
    ///     - Financial reporting and reconciliation
    ///     - Payment monitoring and analytics
    ///     - Administrative payment management
    ///     - Audit trail generation
    /// </remarks>
    [HttpGet]
    [EndpointSummary("Retrieve all payment transactions with optional filtering")]
    [EndpointDescription("Retrieves a paginated list of all payment transactions with support for filtering by tenant, status, and date range. This is the primary endpoint for payment administration and reporting.")]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        return Ok(await sender.Send(new GetAllPaymentsQuery(tenantId, status, startDate, endDate, page, pageSize), ct));
    }

    /// <summary>
    ///     Process a new payment transaction
    /// </summary>
    /// <param name="body">Payment processing request containing tenant ID, subscription ID, amount, and payment method</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment result with transaction ID and processing status</returns>
    /// <remarks>
    ///     Initiates a new payment transaction for a subscription. This endpoint handles the complete payment
    ///     processing workflow including payment method validation, amount verification, and transaction execution.
    ///     Returns the payment result immediately with a transaction ID that can be used to track payment status.
    ///     Processing workflow:
    ///     1. Validate payment method and amount
    ///     2. Verify subscription and tenant information
    ///     3. Process payment through configured payment gateway
    ///     4. Update subscription status based on result
    ///     5. Generate transaction record
    ///     Request body must include:
    ///     - TenantId: Organization identifier
    ///     - SubscriptionId: Target subscription
    ///     - Amount: Payment amount in base currency units
    ///     - PaymentMethodId: Validated payment method identifier
    ///     Returns CreatedAtRoute with payment details for successful transactions.
    /// </remarks>
    [HttpPost]
    [EndpointSummary("Process a new payment transaction")]
    [EndpointDescription(
        "Initiates a new payment transaction for a subscription. This endpoint handles the complete payment processing workflow including payment method validation, amount verification, and transaction execution. Returns the payment result immediately with a transaction ID that can be used to track payment status."
    )]
    [ProducesResponseType<PaymentResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process([FromBody] ProcessPaymentRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new ProcessPaymentCommand(body.TenantId, body.SubscriptionId, body.Amount, body.PaymentMethodId), ct);

        return CreatedAtRoute("GetPaymentById", new { paymentId = result.PaymentId }, result);
    }

    /// <summary>
    ///     Retrieve all canceled payment transactions
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter to get canceled payments for a specific tenant</param>
    /// <param name="cancellationReason">Optional filter by cancellation reason</param>
    /// <param name="startDate">Optional start date filter for cancellation date (inclusive)</param>
    /// <param name="endDate">Optional end date filter for cancellation date (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of canceled payments with cancellation details and reasons</returns>
    /// <remarks>
    ///     Retrieves all payment transactions that have been canceled before completion. This includes payments
    ///     canceled by users, automatic cancellations due to expired sessions, or administrative cancellations.
    ///     Provides comprehensive cancellation information for audit and analysis purposes.
    ///     Common cancellation reasons:
    ///     - User canceled during checkout
    ///     - Session timeout or expiration
    ///     - Administrative cancellation
    ///     - Duplicate transaction prevention
    ///     - Fraud prevention triggers
    ///     - Payment method issues
    ///     Cancellation information includes:
    ///     - Original payment attempt details
    ///     - Cancellation timestamp and reason
    ///     - User or system initiated indicator
    ///     - Associated session or transaction context
    ///     - Recovery or retry recommendations
    ///     Query Parameters:
    ///     - tenantId: Filter cancellations for specific tenant
    ///     - cancellationReason: Filter by cancellation category
    ///     - startDate: Include cancellations from this date onwards
    ///     - endDate: Include cancellations up to this date
    ///     Use cases:
    ///     - Checkout abandonment analysis
    ///     - User experience optimization
    ///     - Fraud prevention review
    ///     - Payment flow troubleshooting
    ///     - Conversion rate analysis
    /// </remarks>
    [HttpGet("canceled")]
    [EndpointSummary("Retrieve all canceled payment transactions")]
    [EndpointDescription(
        "Retrieves all payment transactions that have been canceled before completion. This includes payments canceled by users, automatic cancellations due to expired sessions, or administrative cancellations. Provides comprehensive cancellation information for audit and analysis purposes."
    )]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Canceled([FromQuery] Guid? tenantId, [FromQuery] string? cancellationReason, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        return Ok(await sender.Send(new GetCanceledPaymentsQuery(tenantId, cancellationReason, startDate, endDate), ct));
    }

    /// <summary>
    ///     Retrieve all failed payment transactions
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter to get failed payments for a specific tenant</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of failed payments with error details and failure reasons</returns>
    /// <remarks>
    ///     Retrieves all payment transactions that have failed processing. This includes payments that were
    ///     declined, had insufficient funds, or encountered technical errors. Optionally filter by tenant
    ///     to focus on specific organization's failed transactions for troubleshooting and retry operations.
    ///     Common failure reasons:
    ///     - Insufficient funds
    ///     - Declined by bank/card issuer
    ///     - Expired payment method
    ///     - Network connectivity issues
    ///     - Invalid payment details
    ///     - Fraud detection triggers
    ///     Use this endpoint for:
    ///     - Identifying payment issues requiring attention
    ///     - Generating retry lists for failed payments
    ///     - Monitoring payment success rates
    ///     - Customer support investigations
    /// </remarks>
    [HttpGet("failed")]
    [EndpointSummary("Retrieve all failed payment transactions")]
    [EndpointDescription(
        "Retrieves all payment transactions that have failed processing. Includes payments declined by banks, insufficient funds, technical errors, and fraud detection triggers. Optionally filter by tenant for focused troubleshooting."
    )]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Failed([FromQuery] Guid? tenantId, CancellationToken ct) { return Ok(await sender.Send(new GetFailedPaymentsQuery(tenantId), ct)); }

    /// <summary>
    ///     Retrieve all overdue payment transactions
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter to get overdue payments for a specific tenant</param>
    /// <param name="overdueThreshold">Optional threshold in days to define overdue payments (default: 30 days)</param>
    /// <param name="startDate">Optional start date filter for original payment due date (inclusive)</param>
    /// <param name="endDate">Optional end date filter for original payment due date (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of overdue payments with payment details and overdue information</returns>
    /// <remarks>
    ///     Retrieves all payment transactions that are overdue, meaning they have passed their expected
    ///     completion date without successful payment. This includes subscription renewals, scheduled payments,
    ///     and invoice payments that have exceeded their grace periods or due dates.
    ///     Overdue payment identification criteria:
    ///     - Scheduled payments past their execution date
    ///     - Subscription renewals beyond grace period
    ///     - Invoice payments past due date
    ///     - Failed automatic payments requiring manual intervention
    ///     - Payments with retry attempts that have been exhausted
    ///     Overdue information includes:
    ///     - Original payment due date and current overdue period
    ///     - Number of failed retry attempts and next retry schedule
    ///     - Associated subscription or invoice details
    ///     - Grace period and escalation information
    ///     - Customer notification history and contact attempts
    ///     - Recommended actions for resolution
    ///     Query Parameters:
    ///     - tenantId: Filter overdue payments for specific tenant
    ///     - overdueThreshold: Days past due date to consider overdue (default: 30)
    ///     - startDate: Include payments originally due from this date onwards
    ///     - endDate: Include payments originally due up to this date
    ///     Use cases:
    ///     - Collections and dunning process management
    ///     - Customer account review and intervention
    ///     - Revenue recovery and follow-up campaigns
    ///     - Subscription churn prevention and retention
    ///     - Financial reporting and aging analysis
    ///     - Automated payment retry scheduling
    /// </remarks>
    [HttpGet("overdue")]
    [EndpointSummary("Retrieve all overdue payment transactions")]
    [EndpointDescription(
        "Retrieves all payment transactions that are overdue, meaning they have passed their expected completion date without successful payment. This includes subscription renewals, scheduled payments, and invoice payments that have exceeded their grace periods or due dates."
    )]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Overdue([FromQuery] Guid? tenantId, [FromQuery] int? overdueThreshold, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        return Ok(await sender.Send(new GetOverduePaymentsQuery(tenantId, overdueThreshold ?? 30, startDate, endDate), ct));
    }

    /// <summary>
    ///     Retrieve all refunded payment transactions
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter to get refunded payments for a specific tenant</param>
    /// <param name="refundReason">Optional filter by refund reason</param>
    /// <param name="startDate">Optional start date filter for refund processing date (inclusive)</param>
    /// <param name="endDate">Optional end date filter for refund processing date (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of refunded payments with refund details and processing information</returns>
    /// <remarks>
    ///     Retrieves all payment transactions that have been refunded, either partially or in full.
    ///     Provides comprehensive refund information including original payment details, refund amounts,
    ///     processing dates, and refund reasons for audit and reconciliation purposes.
    ///     Refund information includes:
    ///     - Original payment transaction details
    ///     - Refund amount (partial or full)
    ///     - Refund processing date
    ///     - Refund reason and notes
    ///     - Refund method (original payment method, store credit, etc.)
    ///     - Processing status and timeline
    ///     Query Parameters:
    ///     - tenantId: Filter refunds for specific tenant
    ///     - refundReason: Filter by refund category (chargeback, return, cancellation, etc.)
    ///     - startDate: Include refunds processed from this date onwards
    ///     - endDate: Include refunds processed up to this date
    ///     Use cases:
    ///     - Financial reconciliation and reporting
    ///     - Refund analytics and trend analysis
    ///     - Customer service and dispute resolution
    ///     - Accounting and tax reporting
    ///     - Chargeback management
    /// </remarks>
    [HttpGet("refunded")]
    [EndpointSummary("Retrieve all refunded payment transactions")]
    [EndpointDescription(
        "Retrieves all payment transactions that have been refunded, either partially or in full. Provides comprehensive refund information including original payment details, refund amounts, processing dates, and refund reasons for audit and reconciliation purposes."
    )]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refunded([FromQuery] Guid? tenantId, [FromQuery] string? refundReason, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        return Ok(await sender.Send(new GetRefundedPaymentsQuery(tenantId, refundReason, startDate, endDate), ct));
    }

    /// <summary>
    ///     Retrieve all scheduled payment transactions
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter to get scheduled payments for a specific tenant</param>
    /// <param name="scheduledDate">Optional date filter to get payments scheduled for a specific date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of scheduled payments with their execution dates and details</returns>
    /// <remarks>
    ///     Retrieves all payment transactions that are scheduled for future execution. This includes recurring
    ///     subscription payments, delayed payments, and retry attempts scheduled for later processing.
    ///     Optionally filter by tenant or specific scheduled date.
    ///     Scheduled payment types:
    ///     - Recurring subscription charges
    ///     - Delayed payment processing
    ///     - Retry attempts for failed payments
    ///     - Installment payments
    ///     - Future-dated transactions
    ///     Response includes:
    ///     - Scheduled execution date and time
    ///     - Payment amount and details
    ///     - Associated subscription information
    ///     - Retry attempt count (if applicable)
    ///     - Next execution schedule
    ///     Use this endpoint for:
    ///     - Monitoring upcoming payment executions
    ///     - Financial planning and cash flow projections
    ///     - Managing scheduled payment workflows
    ///     - Troubleshooting recurring payment issues
    /// </remarks>
    [HttpGet("scheduled")]
    [EndpointSummary("Retrieve all scheduled payment transactions")]
    [EndpointDescription(
        "Retrieves all payment transactions that are scheduled for future execution. This includes recurring subscription payments, delayed payments, and retry attempts scheduled for later processing. Optionally filter by tenant or specific scheduled date."
    )]
    [ProducesResponseType<IEnumerable<PaymentResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Scheduled([FromQuery] Guid? tenantId, [FromQuery] DateTime? scheduledDate, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetScheduledPaymentsQuery(tenantId, scheduledDate), ct));
    }

    /// <summary>
    ///     Retrieve a specific payment by its unique identifier
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment details including status, amount, and transaction information</returns>
    /// <remarks>
    ///     Retrieves detailed information about a specific payment transaction, including its current status,
    ///     amount, payment method, and processing details. Use this endpoint to track payment progress
    ///     and verify transaction completion.
    ///     Response includes:
    ///     - Payment ID and transaction details
    ///     - Current payment status (pending, completed, failed, etc.)
    ///     - Payment method information
    ///     - Amount and currency
    ///     - Timestamps for creation and updates
    ///     - Associated subscription and tenant information
    /// </remarks>
    [HttpGet("{paymentId:guid}", Name = "GetPaymentById")]
    [EndpointSummary("Retrieve a specific payment by its unique identifier")]
    [EndpointDescription(
        "Retrieves detailed information about a specific payment transaction, including its current status, amount, payment method, and processing details. Use this endpoint to track payment progress and verify transaction completion."
    )]
    [ProducesResponseType<PaymentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid paymentId, CancellationToken ct)
    {
        var r = await sender.Send(new GetPaymentByIdQuery(paymentId), ct);

        if (r == null) return NotFound();

        return Ok(r);
    }

    /// <summary>
    ///     Cancel a payment transaction
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment to cancel</param>
    /// <param name="body">Cancellation request containing reason and optional metadata</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment cancellation result with cancellation details</returns>
    /// <remarks>
    ///     Cancels a payment transaction that is in progress or pending. This endpoint can be used to
    ///     cancel payments before they are processed, or to handle user-initiated cancellations during checkout.
    ///     Once canceled, a payment cannot be processed and may require a new payment attempt.
    ///     Cancellation scenarios:
    ///     - User abandons checkout process
    ///     - Administrative cancellation
    ///     - Fraud prevention trigger
    ///     - Duplicate transaction prevention
    ///     - Session timeout or expiration
    ///     - Payment method validation failure
    ///     Cancellation handling:
    ///     - Updates payment status to "canceled"
    ///     - Records cancellation reason and timestamp
    ///     - Releases any held resources or reservations
    ///     - Notifies relevant systems of cancellation
    ///     - Provides audit trail for investigation
    ///     Request body options:
    ///     - CancellationReason: Required reason for audit trail
    ///     - CanceledBy: Optional user ID for tracking
    ///     - Notes: Optional additional context
    ///     Important notes:
    ///     - Only pending or processing payments can be canceled
    ///     - Completed payments cannot be canceled (use refund instead)
    ///     - Cancellation is immediate and irreversible
    ///     - Some payment methods may have specific cancellation rules
    /// </remarks>
    [HttpPatch("{paymentId:guid}/cancel")]
    [EndpointSummary("Cancel a payment transaction")]
    [EndpointDescription(
        "Cancels a payment transaction that is in progress or pending. This endpoint can be used to cancel payments before they are processed, or to handle user-initiated cancellations during checkout. Once canceled, a payment cannot be processed and may require a new payment attempt."
    )]
    [ProducesResponseType<PaymentCancellationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid paymentId, [FromBody] CancelPaymentRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new CancelPaymentCommand(paymentId, body.CancellationReason, body.CanceledBy), ct);

        return Ok(result);
    }

    /// <summary>
    ///     Process a refund for a completed payment
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment to refund</param>
    /// <param name="body">Refund request containing optional amount and reason</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Refund processing result with refund ID and status</returns>
    /// <remarks>
    ///     Processes a full or partial refund for a previously completed payment transaction.
    ///     If no amount is specified, a full refund will be processed. The refund reason is optional
    ///     but recommended for record keeping and customer service purposes. Refunds are processed
    ///     back to the original payment method and may take several business days to appear.
    ///     Refund types:
    ///     - Full refund: Refunds the entire payment amount
    ///     - Partial refund: Refunds a specified portion of the payment
    ///     Request body options:
    ///     - Amount: Specific refund amount (null for full refund)
    ///     - Reason: Optional reason for audit and customer service
    ///     Processing notes:
    ///     - Refunds are processed to the original payment method
    ///     - Processing time varies by payment processor (2-10 business days)
    ///     - Refund fees may apply depending on payment method
    ///     - Only successful payments can be refunded
    ///     - Multiple partial refunds allowed up to original amount
    ///     Response includes refund ID for tracking and customer communication.
    /// </remarks>
    [HttpPatch("{paymentId:guid}/refund")]
    [EndpointSummary("Process a refund for a completed payment")]
    [EndpointDescription(
        "Processes a full or partial refund for a previously completed payment transaction. If no amount is specified, a full refund will be processed. The refund reason is optional but recommended for record keeping and customer service purposes. Refunds are processed back to the original payment method and may take several business days to appear."
    )]
    [ProducesResponseType<ProcessRefundResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund(Guid paymentId, [FromBody] RefundRequest body, CancellationToken ct)
    {
        var r = await sender.Send(new ProcessRefundCommand(paymentId, body.Amount ?? 0, body.Reason ?? "No reason provided"), ct);

        return Ok(r);
    }

    /// <summary>
    ///     Retry a failed payment transaction
    /// </summary>
    /// <param name="paymentId">The unique identifier of the failed payment to retry</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment retry result with updated transaction status</returns>
    /// <remarks>
    ///     Attempts to reprocess a previously failed payment transaction using the same payment method and amount.
    ///     This is useful when payments fail due to temporary issues like network problems or insufficient funds
    ///     that have since been resolved. The retry operation creates a new transaction attempt while maintaining
    ///     the link to the original payment record.
    ///     Retry scenarios:
    ///     - Temporary network connectivity issues resolved
    ///     - Insufficient funds now available
    ///     - Payment gateway was temporarily unavailable
    ///     - Rate limiting issues have cleared
    ///     - Card issuer temporary restrictions lifted
    ///     Important notes:
    ///     - Only failed payments can be retried
    ///     - Successful payments will return a 400 Bad Request
    ///     - Retry preserves original payment details
    ///     - New transaction ID is generated for the retry attempt
    ///     - Original payment record maintains audit trail
    /// </remarks>
    [HttpPatch("{paymentId:guid}/retry")]
    [EndpointSummary("Retry a failed payment transaction")]
    [EndpointDescription(
        "Attempts to reprocess a previously failed payment transaction using the same payment method and amount. This is useful when payments fail due to temporary issues like network problems or insufficient funds that have since been resolved. The retry operation creates a new transaction attempt while maintaining the link to the original payment record."
    )]
    [ProducesResponseType<PaymentRetryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Retry(Guid paymentId, CancellationToken ct)
    {
        var r = await sender.Send(new RetryPaymentCommand(paymentId), ct);

        return Ok(r);
    }

    public abstract record ProcessPaymentRequest(Guid TenantId, Guid SubscriptionId, decimal Amount, string PaymentMethodId);

    public abstract record RefundRequest(decimal? Amount, string? Reason);

    public abstract record CancelPaymentRequest(string CancellationReason, Guid? CanceledBy = null, string? Notes = null);
}
