namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of payment retry processing
/// </summary>
public class PaymentRetryResult
{
    /// <summary>
    ///     Whether the retry was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Number of retry attempts made
    /// </summary>
    public int AttemptsCount { get; init; }

    /// <summary>
    ///     Maximum allowed retry attempts
    /// </summary>
    public int MaxAttempts { get; init; }

    /// <summary>
    ///     Payment result from the retry
    /// </summary>
    public PaymentResult? PaymentResult { get; init; }

    /// <summary>
    ///     When the next retry should occur
    /// </summary>
    public DateTime? NextRetryAt { get; init; }

    /// <summary>
    ///     Error message if retry failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Whether all retry attempts have been exhausted
    /// </summary>
    public bool RetriesExhausted { get => AttemptsCount >= MaxAttempts; }

    /// <summary>
    ///     Creates a successful retry result
    /// </summary>
    public static PaymentRetryResult CreateSuccess(int attemptsCount, PaymentResult paymentResult) { return new PaymentRetryResult { Success = true, AttemptsCount = attemptsCount, PaymentResult = paymentResult }; }

    /// <summary>
    ///     Creates a failed retry result with next retry scheduled
    /// </summary>
    public static PaymentRetryResult FailedWithRetry(int attemptsCount, int maxAttempts, DateTime nextRetryAt, string? errorMessage = null)
    {
        return new PaymentRetryResult { Success = false, AttemptsCount = attemptsCount, MaxAttempts = maxAttempts, NextRetryAt = nextRetryAt, ErrorMessage = errorMessage };
    }

    /// <summary>
    ///     Creates a failed retry result with no more retries
    /// </summary>
    public static PaymentRetryResult FailedExhausted(int attemptsCount, int maxAttempts, string? errorMessage = null)
    {
        return new PaymentRetryResult { Success = false, AttemptsCount = attemptsCount, MaxAttempts = maxAttempts, ErrorMessage = errorMessage };
    }
}
