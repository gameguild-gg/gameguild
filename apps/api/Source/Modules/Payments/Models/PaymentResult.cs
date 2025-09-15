namespace GameGuild.Modules.Payments.Models;

/// <summary>
/// Result of payment processing operations
/// </summary>
public class PaymentResult
{
    /// <summary>
    /// Whether the payment was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Unique payment identifier from the system
    /// </summary>
    public Guid? PaymentId { get; init; }

    /// <summary>
    /// Transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public Money? Amount { get; init; }

    /// <summary>
    /// When the payment was processed
    /// </summary>
    public DateTime? ProcessedAt { get; init; }

    /// <summary>
    /// Reason for payment failure (if any)
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Payment method used
    /// </summary>
    public string? PaymentMethodId { get; init; }

    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; init; }

    /// <summary>
    /// Processing fees applied
    /// </summary>
    public Money? ProcessingFees { get; init; }

    /// <summary>
    /// Net amount after fees
    /// </summary>
    public Money? NetAmount { get; init; }

    /// <summary>
    /// Create a successful payment result
    /// </summary>
    public static PaymentResult Success(
        Guid paymentId,
        string transactionId,
        Money amount,
        string paymentMethodId,
        Money? processingFees = null)
    {
        var netAmount = processingFees != null ? amount - processingFees : amount;

        return new PaymentResult
        {
            Success = true,
            PaymentId = paymentId,
            TransactionId = transactionId,
            Amount = amount,
            ProcessedAt = DateTime.UtcNow,
            PaymentMethodId = paymentMethodId,
            Status = PaymentStatus.Completed,
            ProcessingFees = processingFees,
            NetAmount = netAmount
        };
    }

    /// <summary>
    /// Create a failed payment result
    /// </summary>
    public static PaymentResult Failed(
        string failureReason,
        Guid? paymentId = null,
        string? paymentMethodId = null,
        Money? amount = null)
    {
        return new PaymentResult
        {
            Success = false,
            PaymentId = paymentId,
            FailureReason = failureReason,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Status = PaymentStatus.Failed
        };
    }

    /// <summary>
    /// Create a pending payment result
    /// </summary>
    public static PaymentResult Pending(
        Guid paymentId,
        string? transactionId,
        Money amount,
        string paymentMethodId)
    {
        return new PaymentResult
        {
            Success = false, // Not successful until confirmed
            PaymentId = paymentId,
            TransactionId = transactionId,
            Amount = amount,
            PaymentMethodId = paymentMethodId,
            Status = PaymentStatus.Pending
        };
    }
}
