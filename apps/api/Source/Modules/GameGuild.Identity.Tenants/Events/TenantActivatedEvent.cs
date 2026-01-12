using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Domain event fired when a tenant is activated
/// </summary>
public class TenantActivatedEvent(Guid tenantId, string name) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Name { get; } = name;
}
