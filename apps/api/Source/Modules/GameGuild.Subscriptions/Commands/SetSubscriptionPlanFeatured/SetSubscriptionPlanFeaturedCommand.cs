using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record SetSubscriptionPlanFeaturedCommand(Guid Id, bool IsFeatured) : ICommand;
