using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get a tenant by slug
/// </summary>
/// <param name="Slug">Tenant slug</param>
public record GetTenantBySlugQuery(string Slug) : IQuery<Tenant?>;
