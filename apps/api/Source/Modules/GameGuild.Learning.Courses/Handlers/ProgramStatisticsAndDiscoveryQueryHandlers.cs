using GameGuild.CQRS;




using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Query handlers for Program discovery operations (popular, recent, featured, recommended).
/// The statistics reads (single program, global, creator) are owned by the dedicated handlers
/// in Queries/GetProgramStatistics, Queries/GetGlobalProgramStatistics and
/// Queries/GetCreatorProgramStatistics (issue #1277: this class previously registered stale
/// duplicates of those three — its creator variant even returned hard-zeroed statistics —
/// which resolved last-wins-silently in DI).
/// </summary>
public sealed class ProgramStatisticsAndDiscoveryQueryHandlers(IApplicationDbContext context, ILogger<ProgramStatisticsAndDiscoveryQueryHandlers> logger) : IRequestHandler<GetPopularProgramsQuery, IEnumerable<Program>>,
                                                                                                                                                      IRequestHandler<GetRecentProgramsQuery, IEnumerable<Program>>,
                                                                                                                                                      IRequestHandler<GetFeaturedProgramsQuery, IEnumerable<Program>>,
                                                                                                                                                      IRequestHandler<GetRecommendedProgramsQuery, IEnumerable<Program>> {
  public async Task<IEnumerable<Program>> Handle(GetPopularProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting popular programs");

    var sinceDate = SystemClock.UtcNow.AddDays(-request.DaysBack);

    var programs = await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public)
                                .OrderByDescending(p => p.ProgramUsers.Count(pu => pu.JoinedAt >= sinceDate)) // Fixed property name
                                .ThenByDescending(p => p.ProgramRatings.Count > 0 ? p.ProgramRatings.Average(pr => pr.Rating) : 0)
                                .Skip(request.Skip)
                                .Take(request.Take)
                                .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} popular programs", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetRecentProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting recent programs");

    var sinceDate = SystemClock.UtcNow.AddDays(-request.DaysBack);

    var programs = await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public && p.CreatedAt >= sinceDate // Fixed: use CreatedAt instead of PublishedAt
                                )
                                .OrderByDescending(p => p.CreatedAt) // Fixed: use CreatedAt instead of PublishedAt
                                .Skip(request.Skip)
                                .Take(request.Take)
                                .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} recent programs", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetFeaturedProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting featured programs");

    // Note: This is a simplified implementation. You might want to add a "Featured" flag to programs
    var programs = await context.Set<Program>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published && p.Visibility == ContentVisibility.Public)
                                .OrderByDescending(p => p.ProgramRatings.Count > 0 ? p.ProgramRatings.Average(pr => pr.Rating) : 0)
                                .ThenByDescending(p => p.ProgramUsers.Count)
                                .Skip(request.Skip)
                                .Take(request.Take)
                                .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} featured programs", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetRecommendedProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting recommended programs for user: {UserId}", request.UserId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid

    // Note: This is a simplified recommendation system. A real implementation would use more sophisticated algorithms
    var userEnrollments = await context.Set<ProgramUser>().Where(pu => pu.UserId == userGuid && pu.IsActive) // Fixed: use converted Guid
                                       .Select(pu => pu.Program)
                                       .ToListAsync(cancellationToken);

    var userCategories = userEnrollments.Select(p => p.Category).Distinct().ToList();
    var userDifficulties = userEnrollments.Select(p => p.Difficulty).Distinct().ToList();

    var enrolledProgramIds = userEnrollments.Select(p => p.Id).ToList();

    var recommendations = await context.Set<Program>()
                                       .Where(p => p.DeletedAt == null &&
                                                   p.Status == ContentStatus.Published &&
                                                   p.Visibility == ContentVisibility.Public &&
                                                   !enrolledProgramIds.Contains(p.Id) &&
                                                   (userCategories.Contains(p.Category) || userDifficulties.Contains(p.Difficulty))
                                       )
                                       .OrderByDescending(p => p.ProgramRatings.Count > 0 ? p.ProgramRatings.Average(pr => pr.Rating) : 0)
                                       .Take(request.Take)
                                       .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} recommended programs for user {UserId}", recommendations.Count, request.UserId);

    return recommendations;
  }
}
