using GameGuild.CQRS;


namespace GameGuild.Modules.Payments.Commands.ProcessRefund;

/// <summary>
/// Command to process a refund
/// </summary>
public record ProcessRefundCommand : ICommand<PaymentResult>
{
    /// <summary>
    /// Original payment ID to refund
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// Refund amount (if null, refunds the full amount)
    /// </summary>
    public Money? Amount { get; init; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Whether this is a partial refund
    /// </summary>
    public bool IsPartialRefund { get; init; } = false;

    /// <summary>
    /// Whether to refund processing fees
    /// </summary>
    public bool RefundProcessingFees { get; init; } = false;

    /// <summary>
    /// User requesting the refund (for audit)
    /// </summary>
    public Guid RequestedByUserId { get; init; }

    /// <summary>
    /// Internal notes about the refund
    /// </summary>
    public string? InternalNotes { get; init; }

    /// <summary>
    /// Whether to send refund notification to customer
    /// </summary>
    public bool SendNotification { get; init; } = true;

    /// <summary>
    /// Whether this refund should be processed immediately
    /// </summary>
    public bool ProcessImmediately { get; init; } = true;

    /// <summary>
    /// Scheduled date for processing (if not immediate)
    /// </summary>
    public DateTime? ScheduledDate { get; init; }

    /// <summary>
    /// Refund method (original payment method, store credit, etc.)
    /// </summary>
    public string RefundMethod { get; init; } = "original";

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Idempotency key for duplicate prevention
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Reference to external refund system
    /// </summary>
    public string? ExternalRefundId { get; init; }
}
