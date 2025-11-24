using GameGuild.CQRS;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Query to get tenants with pagination support
/// </summary>
public record GetTenantsPageQuery(
    int Page = 1,
    int PageSize = 10,
    bool IncludeInactive = false,
    bool IncludeArchived = false,
    string? SearchTerm = null,
    string? SortBy = "Name",
    bool SortDescending = false
) : IQuery<PagedResult<Tenant>>;
