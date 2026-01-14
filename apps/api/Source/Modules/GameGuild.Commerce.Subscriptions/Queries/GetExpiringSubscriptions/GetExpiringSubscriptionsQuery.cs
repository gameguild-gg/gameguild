using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get subscriptions that are expiring within the specified number of days
/// </summary>
/// <param name="Days">Number of days to look ahead for expiring subscriptions</param>
public sealed record GetExpiringSubscriptionsQuery(int Days = 30) : IQuery<IEnumerable<Subscription>>;
