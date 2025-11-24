using GameGuild.CQRS;

namespace GameGuild.Tenants.Events;

/// <summary>
///     Domain event raised when a tenant is restored
/// </summary>
public class TenantRestoredEvent(Guid tenantId, string name) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Name { get; } = name;
}
