using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionBillingHistoryQuery(Guid SubscriptionId) : IQuery<IEnumerable<BillingHistoryDto>>;
