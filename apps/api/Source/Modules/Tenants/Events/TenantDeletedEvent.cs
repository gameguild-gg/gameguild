using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Domain event fired when a tenant is deleted (soft delete)
/// </summary>
public class TenantDeletedEvent(Guid tenantId, string name) : DomainEventBase(tenantId, nameof(Tenant)) {
  public Guid TenantId { get; init; } = tenantId;

  public string Name { get; init; } = name;
}
