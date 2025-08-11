using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to search tenants with advanced filtering
/// </summary>
public class SearchTenantsCommand : ICommand<Common.Result<IEnumerable<Tenant>>> {
  public string? SearchTerm { get; init; }

  public bool? IsActive { get; init; }

  public bool IncludeDeleted { get; init; }

  public TenantSortField SortBy { get; init; } = TenantSortField.Name;

  public bool SortDescending { get; init; }

  public int? Limit { get; init; }

  public int? Offset { get; init; }

  public SearchTenantsCommand(
    string? searchTerm = null,
    bool? isActive = null,
    bool includeDeleted = false,
    TenantSortField sortBy = TenantSortField.Name,
    bool sortDescending = false,
    int? limit = null,
    int? offset = null
  ) {
    SearchTerm = searchTerm;
    IsActive = isActive;
    IncludeDeleted = includeDeleted;
    SortBy = sortBy;
    SortDescending = sortDescending;
    Limit = limit;
    Offset = offset;
  }
}
