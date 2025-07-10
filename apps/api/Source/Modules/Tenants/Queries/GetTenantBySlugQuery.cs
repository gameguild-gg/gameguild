using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get a tenant by slug
/// </summary>
public class GetTenantBySlugQuery(string slug, bool includeDeleted = false) : IQuery<Common.Result<Tenant?>> {
  public string Slug { get; init; } = slug;

  public bool IncludeDeleted { get; init; } = includeDeleted;
}
