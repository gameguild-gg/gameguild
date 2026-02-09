using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record UpdateSubscriptionPlanCommand(Guid Id, string Name, string? Description, int? SortOrder) : ICommand;
