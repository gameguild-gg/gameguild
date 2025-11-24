namespace GameGuild.Payments.Models;

/// <summary>
///     Result model for payment history query operations
/// </summary>
public sealed record PaymentHistoryResult
{
    /// <summary>
    ///     Unique payment identifier
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    ///     User who made the payment
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Payment amount
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    ///     Currency code
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    ///     Current payment status
    /// </summary>
    public required PaymentStatus Status { get; init; }

    /// <summary>
    ///     Payment method used
    /// </summary>
    public required string PaymentMethod { get; init; }

    /// <summary>
    ///     Description of the payment
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    ///     When the payment was created
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    ///     When the payment was last updated
    /// </summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    ///     Transaction reference number
    /// </summary>
    public string? TransactionReference { get; init; }

    /// <summary>
    ///     Merchant or vendor name
    /// </summary>
    public string? MerchantName { get; init; }

    /// <summary>
    ///     Any refund amount applied
    /// </summary>
    public decimal RefundedAmount { get; init; }

    /// <summary>
    ///     Payment processing fees
    /// </summary>
    public decimal ProcessingFee { get; init; }

    /// <summary>
    ///     Net amount after fees and refunds
    /// </summary>
    public decimal NetAmount { get => Amount - RefundedAmount - ProcessingFee; }

    /// <summary>
    ///     Indicates if payment is completed successfully
    /// </summary>
    public bool IsCompleted { get => Status == PaymentStatus.Succeeded; }

    /// <summary>
    ///     Indicates if payment has been refunded (partially or fully)
    /// </summary>
    public bool HasRefund { get => RefundedAmount > 0; }
}
