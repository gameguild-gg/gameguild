using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for searching tenants with advanced filtering
/// </summary>
public class SearchTenantsHandler(
  ApplicationDbContext context,
  ILogger<SearchTenantsHandler> logger
) : IQueryHandler<SearchTenantsQuery, Common.Result<IEnumerable<Tenant>>> {
  public async Task<Common.Result<IEnumerable<Tenant>>> Handle(SearchTenantsQuery request, CancellationToken cancellationToken) {
    try {
      var query = context.Resources.OfType<Tenant>().AsQueryable();

      // Filter by deleted status
      if (!request.IncludeDeleted) query = query.Where(t => t.DeletedAt == null);

      // Filter by active status
      if (request.IsActive.HasValue) query = query.Where(t => t.IsActive == request.IsActive.Value);

      // Search term filtering
      if (!string.IsNullOrWhiteSpace(request.SearchTerm)) {
        var searchTerm = request.SearchTerm.ToLowerInvariant();
        query = query.Where(t =>
                              t.Name.ToLower().Contains(searchTerm) ||
                              (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                              t.Slug.ToLower().Contains(searchTerm)
        );
      }

      // Apply sorting
      query = request.SortBy switch {
        TenantSortField.Name => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
        TenantSortField.Description => request.SortDescending ? query.OrderByDescending(t => t.Description) : query.OrderBy(t => t.Description),
        TenantSortField.IsActive => request.SortDescending ? query.OrderByDescending(t => t.IsActive) : query.OrderBy(t => t.IsActive),
        TenantSortField.Slug => request.SortDescending ? query.OrderByDescending(t => t.Slug) : query.OrderBy(t => t.Slug),
        TenantSortField.CreatedAt => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
        TenantSortField.UpdatedAt => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
        _ => query.OrderBy(t => t.Name),
      };

      // Apply pagination
      if (request.Offset.HasValue) query = query.Skip(request.Offset.Value);

      if (request.Limit.HasValue) query = query.Take(request.Limit.Value);

      var tenants = await query.ToListAsync(cancellationToken);

      logger.LogInformation("Search returned {Count} tenants with term '{SearchTerm}'", tenants.Count, request.SearchTerm ?? "");

      return Result.Success<IEnumerable<Tenant>>(tenants);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error searching tenants with term '{SearchTerm}'", request.SearchTerm);

      return Result.Failure<IEnumerable<Tenant>>(
        Common.Error.Failure("Tenant.SearchFailed", "Failed to search tenants")
      );
    }
  }
}
