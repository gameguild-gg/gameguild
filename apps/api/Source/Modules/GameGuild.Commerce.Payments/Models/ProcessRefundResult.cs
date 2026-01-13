namespace GameGuild.Commerce.Payments;

/// <summary>
///     Result model for refund processing operations
/// </summary>
public sealed record ProcessRefundResult
{
    /// <summary>
    ///     Unique identifier for the refund transaction
    /// </summary>
    public required Guid RefundId { get; init; }

    /// <summary>
    ///     Reference to the original payment ID
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    ///     Amount that was refunded
    /// </summary>
    public required decimal RefundedAmount { get; init; }

    /// <summary>
    ///     Currency of the refunded amount
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    ///     Status of the refund operation
    /// </summary>
    public required TransactionStatus Status { get; init; }

    /// <summary>
    ///     Reason for the refund
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    ///     Timestamp when the refund was processed
    /// </summary>
    public required DateTime ProcessedAt { get; init; }

    /// <summary>
    ///     Reference number for tracking the refund
    /// </summary>
    public string? ReferenceNumber { get; init; }

    /// <summary>
    ///     Estimated time for refund to be completed
    /// </summary>
    public DateTime? EstimatedCompletionDate { get; init; }

    /// <summary>
    ///     Any additional processing fees
    /// </summary>
    public decimal ProcessingFee { get; init; }

    /// <summary>
    ///     Success indicator
    /// </summary>
    public bool IsSuccessful { get => Status == TransactionStatus.Completed; }
}
