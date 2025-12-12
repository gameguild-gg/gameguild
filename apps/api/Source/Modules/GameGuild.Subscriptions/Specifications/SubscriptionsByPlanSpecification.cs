using GameGuild.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by plan
/// </summary>
public class SubscriptionsByPlanSpecification : Specification<Subscription>
{
    public SubscriptionsByPlanSpecification(Guid planId) : base(s => s.PlanId == planId) { ApplyOrderByDescending(s => s.CreatedAt); }
}
