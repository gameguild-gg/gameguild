using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Domain event fired when a tenant is created
/// </summary>
public class TenantCreatedEvent(Guid tenantId, string name, string? description, bool isActive, string slug)
  : DomainEventBase(tenantId, nameof(Tenant)) {
    public Guid TenantId { get; init; } = tenantId;

    public string Name { get; init; } = name;

    public string? Description { get; init; } = description;

    public bool IsActive { get; init; } = isActive;

    public string Slug { get; init; } = slug;
}
