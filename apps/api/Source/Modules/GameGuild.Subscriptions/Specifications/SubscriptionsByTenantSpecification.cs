using GameGuild.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by tenant
/// </summary>
public class SubscriptionsByTenantSpecification : Specification<Subscription>
{
    public SubscriptionsByTenantSpecification(Guid tenantId) : base(s => s.TenantId == tenantId) { ApplyOrderByDescending(s => s.CreatedAt); }
}
