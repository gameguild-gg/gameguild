using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionBillingHistoryQuery(Guid SubscriptionId) : IQuery<IEnumerable<BillingHistoryDto>>;
