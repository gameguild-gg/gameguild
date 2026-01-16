using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to archive a subscription plan
/// </summary>
public record ArchiveSubscriptionPlanCommand(Guid PlanId) : ICommand;
