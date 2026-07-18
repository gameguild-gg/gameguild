namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of a payment recording operation on a subscription.
///     Provides detailed outcome information including out-of-order rejection details.
/// </summary>
public sealed record PaymentRecordResult
{
    /// <summary>
    ///     Whether the payment was successfully recorded
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     Whether this was an idempotent duplicate (already processed)
    /// </summary>
    public bool IsAlreadyProcessed { get; init; }

    /// <summary>
    ///     Whether the payment was rejected due to out-of-order processing
    /// </summary>
    public bool IsRejectedOutOfOrder { get; init; }

    /// <summary>
    ///     Whether the payment was rejected because the subscription is cancelled/expired.
    ///     Economic invariant: Cannot charge cancelled subscriptions.
    /// </summary>
    public bool IsRejectedCancelled { get; init; }

    /// <summary>
    ///     Whether the payment was rejected because it did not match the subscription's authoritative money.
    /// </summary>
    public bool IsRejectedMoney { get; init; }

    /// <summary>
    ///     The idempotency key for this payment
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    ///     The billing cycle this payment was for (if specified)
    /// </summary>
    public int? RequestedBillingCycle { get; init; }

    /// <summary>
    ///     The last processed billing cycle on the subscription
    /// </summary>
    public int LastProcessedBillingCycle { get; init; }

    /// <summary>
    ///     Error or status message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Creates a successful payment recording result
    /// </summary>
    public static PaymentRecordResult Success(string idempotencyKey, int billingCycle)
        => new()
        {
            IsSuccess = true,
            IsAlreadyProcessed = false,
            IsRejectedOutOfOrder = false,
            IsRejectedCancelled = false,
            IdempotencyKey = idempotencyKey,
            LastProcessedBillingCycle = billingCycle,
            Message = "Payment recorded successfully"
        };

    /// <summary>
    ///     Creates an already-processed (idempotent) result
    /// </summary>
    public static PaymentRecordResult AlreadyProcessed(string idempotencyKey, int lastProcessedCycle)
        => new()
        {
            IsSuccess = false,
            IsAlreadyProcessed = true,
            IsRejectedOutOfOrder = false,
            IsRejectedCancelled = false,
            IdempotencyKey = idempotencyKey,
            LastProcessedBillingCycle = lastProcessedCycle,
            Message = "Payment already processed (idempotent)"
        };

    /// <summary>
    ///     Creates an out-of-order rejection result
    /// </summary>
    public static PaymentRecordResult RejectedOutOfOrder(int requestedCycle, int lastProcessedCycle, string message)
        => new()
        {
            IsSuccess = false,
            IsAlreadyProcessed = false,
            IsRejectedOutOfOrder = true,
            IsRejectedCancelled = false,
            RequestedBillingCycle = requestedCycle,
            LastProcessedBillingCycle = lastProcessedCycle,
            Message = message
        };

    /// <summary>
    ///     Creates a rejection result for cancelled/expired subscriptions.
    ///     Economic invariant: Cannot charge cancelled subscriptions - requires refund.
    /// </summary>
    public static PaymentRecordResult RejectedCancelled(string message)
        => new()
        {
            IsSuccess = false,
            IsAlreadyProcessed = false,
            IsRejectedOutOfOrder = false,
            IsRejectedCancelled = true,
            Message = message
        };

    /// <summary>
    ///     Creates a rejection result for a payment that does not exactly match the amount due.
    /// </summary>
    public static PaymentRecordResult RejectedMoney(string idempotencyKey, int requestedCycle, int lastProcessedCycle, string message)
        => new()
        {
            IsSuccess = false,
            IsAlreadyProcessed = false,
            IsRejectedOutOfOrder = false,
            IsRejectedCancelled = false,
            IsRejectedMoney = true,
            IdempotencyKey = idempotencyKey,
            RequestedBillingCycle = requestedCycle,
            LastProcessedBillingCycle = lastProcessedCycle,
            Message = message
        };
}
