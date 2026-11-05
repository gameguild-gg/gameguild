using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Domain event fired when a tenant is updated
/// </summary>
public class TenantUpdatedEvent(Guid tenantId, string name, string? description) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Name { get; } = name;

    public string? Description { get; } = description;
}
