using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;

namespace GameGuild.Modules.Tenants.Events;

/// <summary>
/// Domain event fired when a tenant is created
/// </summary>
public class TenantCreatedEvent : DomainEventBase
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string Slug { get; init; } = string.Empty;

    public TenantCreatedEvent(Guid tenantId, string name, string? description, bool isActive, string slug)
        : base(tenantId, nameof(Tenant))
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = isActive;
        Slug = slug;
    }
}
