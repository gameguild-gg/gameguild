using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to reactivate a suspended or cancelled subscription — the resubscribe path
///     after a cancellation (issue #409: score cancellations-resubscribe). Carries the
///     fraud metadata of the reactivation attempt; a Block-level assessment refuses the
///     reactivation before billing resumes.
/// </summary>
public sealed record ReactivateSubscriptionCommand(
    Guid SubscriptionId,
    string? FraudCountry = null,
    string? FraudSourceIpAddress = null,
    string? FraudDeviceFingerprint = null,
    string? FraudPayerEmail = null,
    string? FraudCardBin = null,
    string? FraudClientTimezone = null) : ICommand;
