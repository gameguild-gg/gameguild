using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Entities;
using GameGuild.Modules.UserProfiles.Queries;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for getting user profile statistics
/// </summary>
public class GetUserProfileStatisticsHandler(ApplicationDbContext context, ILogger<GetUserProfileStatisticsHandler> logger)
  : IQueryHandler<GetUserProfileStatisticsQuery, Common.Result<UserProfileStatistics>> {
  public async Task<Common.Result<UserProfileStatistics>> Handle(GetUserProfileStatisticsQuery request, CancellationToken cancellationToken) {
    try {
      var fromDate = request.FromDate ?? DateTime.MinValue;
      var toDate = request.ToDate ?? DateTime.MaxValue;

      IQueryable<UserProfile> query = context.Resources.OfType<UserProfile>();

      // Apply tenant filter
      if (request.TenantId.HasValue) query = query.Where(up => EF.Property<Guid?>(up, "TenantId") == request.TenantId);

      var statistics = new UserProfileStatistics();

      // Total counts
      if (request.IncludeDeleted) {
        query = query.IgnoreQueryFilters();
        statistics.TotalUserProfiles = await query.CountAsync(cancellationToken);
        statistics.ActiveUserProfiles = await query.Where(up => up.DeletedAt == null).CountAsync(cancellationToken);
        statistics.DeletedUserProfiles = await query.Where(up => up.DeletedAt != null).CountAsync(cancellationToken);
      }
      else {
        statistics.TotalUserProfiles = await query.Where(up => up.DeletedAt == null).CountAsync(cancellationToken);
        statistics.ActiveUserProfiles = statistics.TotalUserProfiles;
        statistics.DeletedUserProfiles = 0;
      }

      // Date range statistics
      statistics.NewUserProfiles = await query
                                         .Where(up => up.CreatedAt >= fromDate && up.CreatedAt <= toDate)
                                         .CountAsync(cancellationToken);

      statistics.UpdatedUserProfiles = await query
                                             .Where(up => up.UpdatedAt >= fromDate && up.UpdatedAt <= toDate && up.UpdatedAt != up.CreatedAt)
                                             .CountAsync(cancellationToken);

      // Calculate average per day
      var daysDiff = (toDate - fromDate).TotalDays;

      if (daysDiff > 0) { statistics.AverageNewUserProfilesPerDay = statistics.NewUserProfiles / daysDiff; }

      // Display name patterns (common prefixes/titles)
      var displayNames = await query
                               .Where(up => up.DisplayName != null)
                               .Select(up => up.DisplayName!)
                               .ToListAsync(cancellationToken);

      var patterns = displayNames
                     .SelectMany(name => name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                     .Where(word => word.Length > 2)
                     .GroupBy(word => word.ToLower())
                     .ToDictionary(g => g.Key, g => g.Count());

      statistics.DisplayNamePatterns = patterns
                                       .OrderByDescending(p => p.Value)
                                       .Take(10)
                                       .ToDictionary(p => p.Key, p => p.Value);

      // Tenant distribution (if multi-tenant)
      if (request.TenantId == null) {
        var tenantCounts = await query
                                 .GroupBy(up => EF.Property<Guid?>(up, "TenantId"))
                                 .Select(g => new { TenantId = g.Key, Count = g.Count() })
                                 .ToListAsync(cancellationToken);

        statistics.TenantDistribution = tenantCounts
          .ToDictionary(
            tc => tc.TenantId?.ToString() ?? "No Tenant",
            tc => tc.Count
          );
      }

      logger.LogDebug(
        "Generated user profile statistics: Total={Total}, Active={Active}, New={New}",
        statistics.TotalUserProfiles,
        statistics.ActiveUserProfiles,
        statistics.NewUserProfiles
      );

      return Result.Success(statistics);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error generating user profile statistics");

      return Result.Failure<UserProfileStatistics>(
        Common.Error.Failure("UserProfile.StatisticsFailed", "Failed to generate user profile statistics")
      );
    }
  }
}
