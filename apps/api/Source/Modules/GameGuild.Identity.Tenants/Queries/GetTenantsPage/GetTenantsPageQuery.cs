using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get tenants with pagination support
/// </summary>
public sealed record GetTenantsPageQuery(
    int Page = 1,
    int PageSize = 10,
    bool? IsActive = null,
    bool? IsArchived = null,
    string? SearchTerm = null,
    string? SortBy = "Name",
    bool SortDescending = false
) : IQuery<PagedResult<Tenant>>;
