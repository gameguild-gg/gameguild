
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding active subscriptions
/// </summary>
public class ActiveSubscriptionsSpecification : Specification<Subscription>
{
    public ActiveSubscriptionsSpecification() : base(s => s.Status == SubscriptionStatus.Active)
    {
    }
}
