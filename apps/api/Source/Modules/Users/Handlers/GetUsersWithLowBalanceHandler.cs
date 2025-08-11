using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting users with low balance
/// </summary>
public class GetUsersWithLowBalanceHandler(ApplicationDbContext context) : IRequestHandler<GetUsersWithLowBalanceQuery, PagedResult<User>> {
  public async Task<PagedResult<User>> Handle(GetUsersWithLowBalanceQuery request, CancellationToken cancellationToken) {
    var query = context.Users.AsQueryable();

    // Include deleted users if requested
    if (request.IncludeDeleted) query = query.IgnoreQueryFilters();

    // Filter by low balance
    query = query.Where(u => u.AvailableBalance <= request.ThresholdBalance);

    // Apply search term if provided
    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
      query = query.Where(u =>
                            u.Name.Contains(request.SearchTerm) ||
                            u.Email.Contains(request.SearchTerm)
      );

    // Get total count for pagination
    var totalCount = await query.CountAsync(cancellationToken);

    // Apply pagination and ordering
    var users = await query
                      .OrderBy(u => u.AvailableBalance) // Order by balance ascending (lowest first)
                      .ThenBy(u => u.Name)
                      .Skip(request.Skip)
                      .Take(request.Take)
                      .ToListAsync(cancellationToken);

    return new PagedResult<User>(users, totalCount, request.Skip, request.Take);
  }
}
