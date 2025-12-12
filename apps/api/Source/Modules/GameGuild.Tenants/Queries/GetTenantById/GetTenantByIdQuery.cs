using GameGuild.CQRS;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Query to get a tenant by ID
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public record GetTenantByIdQuery(Guid TenantId) : IQuery<Tenant?>;
