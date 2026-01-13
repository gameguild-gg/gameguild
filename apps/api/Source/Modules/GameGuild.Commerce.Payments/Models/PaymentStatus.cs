namespace GameGuild.Commerce.Payments;

/// <summary>
///     Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    Pending,

    Processing,

    Succeeded,

    Failed,

    Cancelled,

    Refunded
}
