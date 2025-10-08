using GameGuild.Modules.Payments.Models;
using GameGuild.Shared;

namespace GameGuild.Modules.Payments.Models;

/// <summary>
///     Result of payment processing
/// </summary>
public class PaymentResult
{
    public bool Success { get; init; }

    public string? TransactionId { get; init; }

    // Internal or provider payment identifier (added to align with controller usage)
    public string? PaymentId { get; init; }

    public Money? Amount { get; init; }

    public DateTime? ProcessedAt { get; init; }

    public string? FailureReason { get; init; }

    public string? PaymentMethodId { get; init; }

    public PaymentStatus Status { get; init; }
}

