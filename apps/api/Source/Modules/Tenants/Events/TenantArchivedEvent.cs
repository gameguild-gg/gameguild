using GameGuild.Core.Domain;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Domain event raised when a tenant is archived
/// </summary>
public class TenantArchivedEvent(Guid tenantId, string reason) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string Reason { get; } = reason;
}