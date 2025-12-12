using GameGuild.CQRS;

namespace GameGuild.Tenants.Events;

/// <summary>
///     Domain event raised when a tenant is created
/// </summary>
public class TenantCreatedEvent(Guid tenantId, string name, string slug, EmailAddress adminEmail) : DomainEvent
{
    public TenantId TenantId { get; } = tenantId;

    public string Name { get; } = name;

    public string Slug { get; } = slug;

    public EmailAddress AdminEmail { get; } = adminEmail;
}
