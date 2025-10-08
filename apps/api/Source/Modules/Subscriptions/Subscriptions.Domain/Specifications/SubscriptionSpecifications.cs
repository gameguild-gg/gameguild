using GameGuild.Shared.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by various criteria
/// </summary>
public class SubscriptionSpecifications
{
    public static ISpecification<Subscription> ByTenant(Guid tenantId) { return new SubscriptionsByTenantSpecification(tenantId); }

    public static ISpecification<Subscription> ByStatus(SubscriptionStatus status) { return new SubscriptionsByStatusSpecification(status); }

    public static ISpecification<Subscription> ActiveSubscriptions() { return new ActiveSubscriptionsSpecification(); }

    public static ISpecification<Subscription> DueForRenewal(DateTime beforeDate) { return new SubscriptionsDueForRenewalSpecification(beforeDate); }

    public static ISpecification<Subscription> ByPlan(Guid planId) { return new SubscriptionsByPlanSpecification(planId); }

    public static ISpecification<Subscription> ExpiringSoon(int daysFromNow = 30) { return new SubscriptionsExpiringSoonSpecification(daysFromNow); }
}

