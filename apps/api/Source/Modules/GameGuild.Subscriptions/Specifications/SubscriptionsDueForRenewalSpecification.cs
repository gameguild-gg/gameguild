using GameGuild.Abstractions;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions due for renewal
/// </summary>
public class SubscriptionsDueForRenewalSpecification : Specification<Subscription>
{
    public SubscriptionsDueForRenewalSpecification(DateTime beforeDate) : base(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate <= beforeDate) { ApplyOrderBy(s => s.NextBillingDate); }
}
