using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to upgrade subscription plan
/// </summary>
public record UpgradeSubscriptionPlanCommand(Guid SubscriptionId, Guid NewPlanId, DateTime? EffectiveDate = null) : ICommand<SubscriptionUpgradeResult>;
