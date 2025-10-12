using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;


namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetSubscriptionsByStatusQuery(SubscriptionStatus Status) : IQuery<IEnumerable<Subscription>>;

