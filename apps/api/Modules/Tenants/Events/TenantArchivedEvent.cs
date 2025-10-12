using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Domain event raised when a tenant is archived
/// </summary>
public class TenantArchivedEvent(Guid tenantId, string reason) : DomainEvent(tenantId, nameof(Tenant))
{
    public Guid TenantId { get; } = tenantId;

    public string Reason { get; } = reason;
}