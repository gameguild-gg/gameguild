using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to downgrade subscription plan
/// </summary>
public sealed record DowngradeSubscriptionPlanCommand(Guid SubscriptionId, Guid NewPlanId, DateTime? EffectiveDate = null) : ICommand<SubscriptionDowngradeResult>;
