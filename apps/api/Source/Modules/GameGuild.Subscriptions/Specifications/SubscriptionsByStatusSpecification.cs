using GameGuild.Abstractions;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by status
/// </summary>
public class SubscriptionsByStatusSpecification : Specification<Subscription>
{
    public SubscriptionsByStatusSpecification(SubscriptionStatus status) : base(s => s.Status == status) { ApplyOrderBy(s => s.NextBillingDate); }
}
