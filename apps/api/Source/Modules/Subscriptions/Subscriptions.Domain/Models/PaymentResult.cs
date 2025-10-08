using GameGuild.Shared;
namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
///     Result of payment processing
/// </summary>
public class PaymentResult
{
    /// <summary>
    ///     Whether the payment was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Payment status
    /// </summary>
    public PaymentStatus Status { get; init; }

    /// <summary>
    ///     Amount that was processed
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    ///     External payment ID (from payment provider)
    /// </summary>
    public string? PaymentId { get; init; }

    /// <summary>
    ///     Transaction ID
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    ///     Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Error code if payment failed
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Additional metadata from payment provider
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    ///     When the payment was processed
    /// </summary>
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     Creates a successful payment result
    /// </summary>
    public static PaymentResult CreateSuccess(Money amount, string? paymentId = null, string? transactionId = null, Dictionary<string, object>? metadata = null)
    {
        return new PaymentResult
        {
            Success = true,
            Status = PaymentStatus.Succeeded,
            Amount = amount,
            PaymentId = paymentId,
            TransactionId = transactionId,
            Metadata = metadata,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates a failed payment result
    /// </summary>
    public static PaymentResult Failed(string errorMessage, string? errorCode = null, PaymentStatus status = PaymentStatus.Failed)
    {
        return new PaymentResult
        {
            Success = false,
            Status = status,
            Amount = Money.Zero(),
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates a pending payment result
    /// </summary>
    public static PaymentResult Pending(Money amount, string? paymentId = null)
    {
        return new PaymentResult
        {
            Success = false,
            Status = PaymentStatus.Pending,
            Amount = amount,
            PaymentId = paymentId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}

