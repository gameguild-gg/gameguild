using GameGuild.Infrastructure.Common.ValueObjects;

namespace GameGuild.Modules.Payments.Models;

/// <summary>
/// Result of payment retry operations
/// </summary>
public class PaymentRetryResult
{
    /// <summary>
    /// Whether the retry was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The payment result from retry attempt
    /// </summary>
    public PaymentResult? PaymentResult { get; init; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int AttemptNumber { get; init; }

    /// <summary>
    /// Maximum number of retries allowed
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Whether more retries are available
    /// </summary>
    public bool CanRetryAgain => AttemptNumber < MaxRetries;

    /// <summary>
    /// When the next retry can be attempted (if applicable)
    /// </summary>
    public DateTime? NextRetryAt { get; init; }

    /// <summary>
    /// Reason for retry failure
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Create a successful retry result
    /// </summary>
    public static PaymentRetryResult Success(PaymentResult paymentResult, int attemptNumber, int maxRetries)
    {
        return new PaymentRetryResult
        {
            Success = true,
            PaymentResult = paymentResult,
            AttemptNumber = attemptNumber,
            MaxRetries = maxRetries
        };
    }

    /// <summary>
    /// Create a failed retry result
    /// </summary>
    public static PaymentRetryResult Failed(
        string failureReason,
        int attemptNumber,
        int maxRetries,
        DateTime? nextRetryAt = null)
    {
        return new PaymentRetryResult
        {
            Success = false,
            FailureReason = failureReason,
            AttemptNumber = attemptNumber,
            MaxRetries = maxRetries,
            NextRetryAt = nextRetryAt
        };
    }

    /// <summary>
    /// Create a retry result when max retries exceeded
    /// </summary>
    public static PaymentRetryResult MaxRetriesExceeded(int maxRetries)
    {
        return new PaymentRetryResult
        {
            Success = false,
            FailureReason = "Maximum retry attempts exceeded",
            AttemptNumber = maxRetries,
            MaxRetries = maxRetries
        };
    }
}
