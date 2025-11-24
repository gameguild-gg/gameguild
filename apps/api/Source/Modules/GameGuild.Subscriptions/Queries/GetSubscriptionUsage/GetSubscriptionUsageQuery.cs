using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionUsageQuery(Guid SubscriptionId) : IQuery<SubscriptionUsageDto>;
