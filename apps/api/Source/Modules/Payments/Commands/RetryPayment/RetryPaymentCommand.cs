using GameGuild.CQRS;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Commands.RetryPayment;

/// <summary>
/// Command to retry a failed payment
/// </summary>
public record RetryPaymentCommand : ICommand<PaymentRetryResult>
{
    /// <summary>
    /// Payment ID to retry
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// New payment method ID to use (optional)
    /// If not provided, will use the original payment method
    /// </summary>
    public string? NewPaymentMethodId { get; init; }

    /// <summary>
    /// Whether this is an automatic retry
    /// </summary>
    public bool IsAutomaticRetry { get; init; } = false;

    /// <summary>
    /// Reason for the retry
    /// </summary>
    public string? RetryReason { get; init; }

    /// <summary>
    /// Force retry even if max attempts exceeded
    /// </summary>
    public bool ForceRetry { get; init; } = false;

    /// <summary>
    /// Override the retry delay
    /// </summary>
    public TimeSpan? RetryDelay { get; init; }

    /// <summary>
    /// Additional metadata for the retry
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to send notifications on retry
    /// </summary>
    public bool SendNotifications { get; init; } = true;

    /// <summary>
    /// Idempotency key for duplicate prevention
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
