using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to upgrade subscription plan. Promo codes (issue #418) are quoted against
///     the prorated upgrade charge before the plan change is applied; a total rejection
///     fails the command before any mutation, and applied codes are recorded against the
///     subscription after the change succeeds. The change attempt also runs the fraud
///     signal evaluation (issue #409: score subscription changes, not just creations): a
///     Block-level assessment refuses the upgrade before anything is mutated; a
///     Review-level assessment flags the subscription with its bounded evidence.
/// </summary>
public sealed record UpgradeSubscriptionPlanCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDate = null,
    IReadOnlyList<string>? PromoCodes = null,
    string? PromoCodeIdempotencyKey = null,
    string? FraudCountry = null,
    string? FraudSourceIpAddress = null,
    string? FraudDeviceFingerprint = null,
    string? FraudPayerEmail = null,
    string? FraudCardBin = null,
    string? FraudClientTimezone = null) : ICommand<SubscriptionUpgradeResult>;
