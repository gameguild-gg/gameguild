using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get a tenant by ID
/// </summary>
public class GetTenantByIdQuery(Guid id, bool includeDeleted = false) : IQuery<Common.Result<Tenant?>> {
  public Guid Id { get; init; } = id;

  public bool IncludeDeleted { get; init; } = includeDeleted;
}
