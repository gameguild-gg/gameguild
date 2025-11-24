using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record UpdateSubscriptionPlanCommand(Guid Id, string Name, string? Description, int? SortOrder) : ICommand;
