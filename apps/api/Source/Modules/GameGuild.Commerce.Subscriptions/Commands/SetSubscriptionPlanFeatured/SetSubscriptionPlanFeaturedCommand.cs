using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SetSubscriptionPlanFeaturedCommand(Guid Id, bool IsFeatured) : ICommand;
