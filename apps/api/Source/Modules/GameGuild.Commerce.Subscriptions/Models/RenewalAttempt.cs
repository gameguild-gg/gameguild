
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Individual renewal attempt result
/// </summary>
public abstract class RenewalAttempt
{
    /// <summary>
    ///     Subscription ID
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    ///     Whether the renewal was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Amount charged (if successful)
    /// </summary>
    public Money? Amount { get; init; }

    /// <summary>
    ///     Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     When the attempt was made
    /// </summary>
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;
}
