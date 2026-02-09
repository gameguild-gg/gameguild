using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionUsageQuery(Guid SubscriptionId) : IQuery<SubscriptionUsageDto>;
