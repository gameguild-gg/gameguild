using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionBillingHistoryQuery(Guid SubscriptionId) : IQuery<IEnumerable<BillingHistoryDto>>;
