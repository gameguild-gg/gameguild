using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to downgrade subscription plan. Promo codes (issue #418) apply only when
///     the downgrade carries a positive net charge (immediate downgrades); a downgrade
///     that issues credit has no charge to discount and the codes are returned rejected.
///     The change attempt also runs the fraud signal evaluation (issue #409: score
///     subscription changes, not just creations): a Block-level assessment refuses the
///     downgrade before anything is mutated; a Review-level assessment flags the
///     subscription with its bounded evidence.
/// </summary>
public sealed record DowngradeSubscriptionPlanCommand(
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
    string? FraudClientTimezone = null) : ICommand<SubscriptionDowngradeResult>;
