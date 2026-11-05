using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Domain event fired when a tenant is deactivated
/// </summary>
public class TenantDeactivatedEvent(Guid tenantId, string name) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Name { get; } = name;
}
