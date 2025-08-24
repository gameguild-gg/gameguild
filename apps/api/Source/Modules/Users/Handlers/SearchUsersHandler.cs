using GameGuild.Common;
using GameGuild.Database;


// For EF.Functions.ILike


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for searching users with filtering and pagination
/// </summary>
public class SearchUsersHandler(ApplicationDbContext context) : IRequestHandler<SearchUsersQuery, PagedResult<User>> {
  public async Task<PagedResult<User>> Handle(SearchUsersQuery request, CancellationToken cancellationToken) {
    IQueryable<User> query = context.Users.Include(u => u.Credentials);

    // Apply filters
    query = !request.IncludeDeleted ? query.Where(user => user.DeletedAt == null) : query.IgnoreQueryFilters();

    if (request.IsActive.HasValue) query = query.Where(user => user.IsActive == request.IsActive.Value);

    if (!string.IsNullOrWhiteSpace(request.SearchTerm)) {
      // Use ILIKE for case-insensitive search in PostgreSQL and include username matching
      var term = $"%{request.SearchTerm.Trim()}%";
      query = query.Where(user =>
        EF.Functions.ILike(user.Name, term) ||
        EF.Functions.ILike(user.Email, term) ||
        EF.Functions.ILike(user.Username, term)
      );
    }

    if (request.MinBalance.HasValue) query = query.Where(u => u.Balance >= request.MinBalance.Value);

    if (request.MaxBalance.HasValue) query = query.Where(u => u.Balance <= request.MaxBalance.Value);

    if (request.CreatedAfter.HasValue) query = query.Where(u => u.CreatedAt >= request.CreatedAfter.Value);

    if (request.CreatedBefore.HasValue) query = query.Where(u => u.CreatedAt <= request.CreatedBefore.Value);

    // Get total count before applying pagination
    var totalCount = await query.CountAsync(cancellationToken);

    // Apply pagination
    var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);

    return new PagedResult<User>(items, totalCount, request.Skip, request.Take);
  }
}
