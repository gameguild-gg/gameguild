namespace GameGuild.Commerce.Payments;

/// <summary>
///     Result of payment retry
/// </summary>
public class PaymentRetryResult
{
    public bool Success { get; init; }

    public int RetryAttempt { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public PaymentResult? PaymentResult { get; init; }

    public bool MaxRetriesReached { get; init; }

    public string? FailureReason { get; init; }
}
