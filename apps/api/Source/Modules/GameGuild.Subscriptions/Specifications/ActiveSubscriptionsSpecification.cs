using GameGuild.Abstractions;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Specifications;

/// <summary>
///     Specification for finding active subscriptions
/// </summary>
public class ActiveSubscriptionsSpecification : Specification<Subscription>
{
    public ActiveSubscriptionsSpecification() : base(s => s.Status == SubscriptionStatus.Active)
    {
        // Apply order by Next Billing Date
        ApplyOrderBy(s => s.NextBillingDate);
    }
}
