namespace GameGuild.Payments.Models;

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
