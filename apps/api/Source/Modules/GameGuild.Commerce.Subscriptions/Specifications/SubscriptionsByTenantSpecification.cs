using GameGuild.Models;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Specification for finding subscriptions by tenant
/// </summary>
public class SubscriptionsByTenantSpecification : Specification<Subscription>
{
    public SubscriptionsByTenantSpecification(Guid tenantId) : base(s => s.TenantId == tenantId) { }
}
