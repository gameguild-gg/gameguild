using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Response for paged tenant queries
/// </summary>
/// <param name="Items">List of tenants</param>
/// <param name="TotalItems">Total number of items</param>
/// <param name="Page">Current page number</param>
/// <param name="PageSize">Items per page</param>
/// <param name="TotalPages">Total number of pages</param>
public record PagedTenantsResponse(IEnumerable<Tenant> Items, int TotalItems, int Page, int PageSize, int TotalPages);
