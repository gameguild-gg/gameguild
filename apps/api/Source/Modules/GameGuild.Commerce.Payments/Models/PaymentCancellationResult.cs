namespace GameGuild.Commerce.Payments;

/// <summary>
///     Result of a payment cancellation operation
/// </summary>
public sealed class PaymentCancellationResult
{
    /// <summary>
    ///     The unique identifier of the canceled payment
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    ///     The reason for the payment cancellation
    /// </summary>
    public required string CancellationReason { get; init; }

    /// <summary>
    ///     When the payment was canceled
    /// </summary>
    public required DateTime CanceledAt { get; init; }

    /// <summary>
    ///     The user who canceled the payment (optional for system cancellations)
    /// </summary>
    public Guid? CanceledBy { get; init; }

    /// <summary>
    ///     Whether the cancellation was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Any error message if the cancellation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Whether any refund was processed as part of the cancellation
    /// </summary>
    public bool RefundProcessed { get; init; }

    /// <summary>
    ///     The amount refunded if applicable
    /// </summary>
    public decimal? RefundAmount { get; init; }
}
