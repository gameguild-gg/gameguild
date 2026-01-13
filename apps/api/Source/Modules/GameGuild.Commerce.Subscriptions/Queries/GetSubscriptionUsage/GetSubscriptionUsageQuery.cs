using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionUsageQuery(Guid SubscriptionId) : IQuery<SubscriptionUsageDto>;
