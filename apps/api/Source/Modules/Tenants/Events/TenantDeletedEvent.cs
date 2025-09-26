using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Domain event fired when a tenant is deleted </summary>
public class TenantDeletedEvent(Guid tenantId, string name, bool isSoftDelete = true) : DomainEventBase(tenantId, nameof(Tenant))
{
    public Guid TenantId { get; init; } = tenantId;

    public string Name { get; init; } = name;

    public bool IsSoftDelete { get; init; } = isSoftDelete;
}
