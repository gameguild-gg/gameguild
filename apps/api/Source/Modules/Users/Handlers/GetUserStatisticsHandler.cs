using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting user statistics
/// </summary>
public class GetUserStatisticsHandler(ApplicationDbContext context) : IRequestHandler<GetUserStatisticsQuery, UserStatistics> {
  public async Task<UserStatistics> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken) {
    var query = context.Users.AsQueryable();

    // Apply date filters if provided
    if (request.FromDate.HasValue) query = query.Where(u => u.CreatedAt >= request.FromDate.Value);

    if (request.ToDate.HasValue) query = query.Where(u => u.CreatedAt <= request.ToDate.Value);

    // Include deleted users if requested
    if (request.IncludeDeleted) query = query.IgnoreQueryFilters();

    var now = DateTime.UtcNow;
    var today = now.Date;
    var weekStart = today.AddDays(-(int)today.DayOfWeek);
    var monthStart = new DateTime(today.Year, today.Month, 1);

    var statistics = new UserStatistics {
      TotalUsers = await query.CountAsync(cancellationToken),
      ActiveUsers = await query.CountAsync(u => u.IsActive && u.DeletedAt == null, cancellationToken),
      InactiveUsers = await query.CountAsync(u => !u.IsActive && u.DeletedAt == null, cancellationToken),
      DeletedUsers = request.IncludeDeleted
                       ? await context.Users.IgnoreQueryFilters().CountAsync(u => u.DeletedAt != null, cancellationToken)
                       : 0,
      TotalBalance = await query.Where(u => u.DeletedAt == null).SumAsync(u => u.Balance, cancellationToken),
      AverageBalance = await query.Where(u => u.DeletedAt == null).AverageAsync(u => u.Balance, cancellationToken),
      UsersCreatedToday = await context.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken),
      UsersCreatedThisWeek = await context.Users.CountAsync(u => u.CreatedAt >= weekStart, cancellationToken),
      UsersCreatedThisMonth = await context.Users.CountAsync(u => u.CreatedAt >= monthStart, cancellationToken),
    };

    return statistics;
  }
}
