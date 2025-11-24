using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionsByStatusQuery(SubscriptionStatus Status) : IQuery<IEnumerable<Subscription>>;
