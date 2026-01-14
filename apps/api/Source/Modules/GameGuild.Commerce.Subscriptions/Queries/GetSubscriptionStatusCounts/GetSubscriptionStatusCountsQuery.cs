using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get subscription counts by status
/// </summary>
public sealed record GetSubscriptionStatusCountsQuery : IQuery<Dictionary<SubscriptionStatus, int>>;
