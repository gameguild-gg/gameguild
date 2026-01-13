using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record SetSubscriptionPlanFeaturedCommand(Guid Id, bool IsFeatured) : ICommand;
