using GameGuild.CQRS;

namespace GameGuild.Tenants.Events;

/// <summary>
///     Domain event fired when a tenant is deleted
/// </summary>
public class TenantDeletedEvent(Guid tenantId, string name) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Name { get; } = name;
}
