using GameGuild.CQRS;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to downgrade subscription plan
/// </summary>
public record DowngradeSubscriptionPlanCommand(Guid SubscriptionId, Guid NewPlanId, DateTime? EffectiveDate = null) : ICommand<SubscriptionDowngradeResult>;
