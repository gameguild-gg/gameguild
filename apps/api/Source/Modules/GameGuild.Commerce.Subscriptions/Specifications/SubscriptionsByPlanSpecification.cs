using GameGuild.Models;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding subscriptions by plan
/// </summary>
public class SubscriptionsByPlanSpecification : Specification<Subscription>
{
    public SubscriptionsByPlanSpecification(Guid planId) : base(s => s.PlanId == planId) { }
}
