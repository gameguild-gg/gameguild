
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding subscriptions expiring soon
/// </summary>
public class SubscriptionsExpiringSoonSpecification : Specification<Subscription>
{
    public SubscriptionsExpiringSoonSpecification(int daysFromNow) : base(s => s.EndDate.HasValue && s.EndDate.Value <= DateTime.UtcNow.AddDays(daysFromNow) && s.Status == SubscriptionStatus.Active)
    {
    }
}
