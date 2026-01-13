using GameGuild.Models;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding subscriptions by status
/// </summary>
public class SubscriptionsByStatusSpecification : Specification<Subscription>
{
    public SubscriptionsByStatusSpecification(SubscriptionStatus status) : base(s => s.Status == status) { }
}
