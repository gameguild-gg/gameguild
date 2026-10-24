using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to downgrade subscription plan. Promo codes (issue #418) apply only when
///     the downgrade carries a positive net charge (immediate downgrades); a downgrade
///     that issues credit has no charge to discount and the codes are returned rejected.
/// </summary>
public sealed record DowngradeSubscriptionPlanCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDate = null,
    IReadOnlyList<string>? PromoCodes = null,
    string? PromoCodeIdempotencyKey = null) : ICommand<SubscriptionDowngradeResult>;
