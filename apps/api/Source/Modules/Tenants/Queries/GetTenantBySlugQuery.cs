using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get a tenant by slug
/// </summary>
public class GetTenantBySlugQuery : IQuery<Common.Result<Tenant?>>
{
  public string Slug { get; init; } = string.Empty;
  public bool IncludeDeleted { get; init; }

  public GetTenantBySlugQuery(string slug, bool includeDeleted = false)
  {
    Slug = slug;
    IncludeDeleted = includeDeleted;
  }
}
