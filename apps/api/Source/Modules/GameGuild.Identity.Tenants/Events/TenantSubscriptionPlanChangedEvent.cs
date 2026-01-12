using GameGuild.CQRS;
using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Domain event raised when a tenant's subscription plan is changed
/// </summary>
public class TenantSubscriptionPlanChangedEvent(Guid tenantId, Guid oldPlanId, Guid newPlanId, bool isUpgrade) : DomainEvent
{
    public TenantId TenantId { get; } = tenantId;

    public Guid OldPlanId { get; } = oldPlanId;

    public Guid NewPlanId { get; } = newPlanId;

    public bool IsUpgrade { get; } = isUpgrade;
}
