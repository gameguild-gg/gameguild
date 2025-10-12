using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get tenants with pagination support </summary>
public class GetTenantsPagedQuery : IQuery<Result<PagedResult<Tenant>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool IncludeInactive { get; init; } = false;
    public bool IncludeArchived { get; init; } = false;
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; } = "Name";
    public bool SortDescending { get; init; } = false;
}

/// <summary> Result wrapper for paged data </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}