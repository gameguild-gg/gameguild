using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get a tenant by ID
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<Tenant?>;
