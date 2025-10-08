using GameGuild.Shared.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Specifications;

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

