using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to upgrade subscription plan. Promo codes (issue #418) are quoted against
///     the prorated upgrade charge before the plan change is applied; a total rejection
///     fails the command before any mutation, and applied codes are recorded against the
///     subscription after the change succeeds.
/// </summary>
public sealed record UpgradeSubscriptionPlanCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDate = null,
    IReadOnlyList<string>? PromoCodes = null,
    string? PromoCodeIdempotencyKey = null) : ICommand<SubscriptionUpgradeResult>;
