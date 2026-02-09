using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to clone a subscription plan
/// </summary>
public sealed record CloneSubscriptionPlanCommand(Guid SourcePlanId, string NewName, string NewSlug) : ICommand<Guid>;
