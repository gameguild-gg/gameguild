using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Events;

/// <summary>
/// Domain event fired when a tenant is restored
/// </summary>
public class TenantRestoredEvent : DomainEventBase
{
  public Guid TenantId { get; init; }
  public string Name { get; init; } = string.Empty;

  public TenantRestoredEvent(Guid tenantId, string name)
    : base(tenantId, nameof(Tenant))
  {
    TenantId = tenantId;
    Name = name;
  }
}