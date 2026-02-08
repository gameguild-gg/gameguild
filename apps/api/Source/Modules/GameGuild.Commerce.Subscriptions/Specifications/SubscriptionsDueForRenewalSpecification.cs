
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding subscriptions due for renewal
/// </summary>
public class SubscriptionsDueForRenewalSpecification : Specification<Subscription>
{
    public SubscriptionsDueForRenewalSpecification(DateTime beforeDate) : base(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate <= beforeDate) { }
}
