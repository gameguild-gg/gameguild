using GameGuild.Modules.Subscriptions.Entities;


namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding active subscriptions
/// </summary>
public class ActiveSubscriptionsSpecification : SpecificationBase<Subscription>
{
    public ActiveSubscriptionsSpecification() : base(s => s.Status == SubscriptionStatus.Active) 
    { 
        // Apply order by Next Billing Date
        ApplyOrderBy(s => s.NextBillingDate); 
    }
}

