using GameGuild.CQRS;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Query to get a tenant by slug
/// </summary>
/// <param name="Slug">Tenant slug</param>
public record GetTenantBySlugQuery(string Slug) : IQuery<Tenant?>;
