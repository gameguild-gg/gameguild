using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Query handlers for Program data retrieval operations
/// Implements data access logic for program queries with proper filtering and authorization
/// </summary>
public class ProgramQueryHandlers(
  ApplicationDbContext context,
  ILogger<ProgramQueryHandlers> logger
) :
  IRequestHandler<GetAllProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramByIdQuery, Program?>,
  IRequestHandler<GetProgramBySlugQuery, Program?>,
  IRequestHandler<GetPublishedProgramBySlugQuery, Program?>,
  IRequestHandler<SearchProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramsByCategoryQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramsByDifficultyQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramsByCreatorQuery, IEnumerable<Program>>,
  IRequestHandler<GetUserEnrolledProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramEnrollmentsQuery, IEnumerable<ProgramUser>>,
  IRequestHandler<CheckUserEnrollmentQuery, ProgramUser?>,
  IRequestHandler<GetProgramContentQuery, IEnumerable<ProgramContent>>,
  IRequestHandler<GetUserProgramProgressQuery, ProgramUserProgress?>,
  IRequestHandler<GetProgramStatisticsQuery, ProgramStatistics>,
  IRequestHandler<GetGlobalProgramStatisticsQuery, GlobalProgramStatistics>,
  IRequestHandler<GetCreatorProgramStatisticsQuery, CreatorProgramStatistics>,
  IRequestHandler<GetPopularProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetRecentProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetFeaturedProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetRecommendedProgramsQuery, IEnumerable<Program>>,
  IRequestHandler<GetProgramRatingsQuery, IEnumerable<ProgramRating>>,
  IRequestHandler<GetUserProgramRatingQuery, ProgramRating?>,
  IRequestHandler<GetUserWishlistQuery, IEnumerable<Program>>,
  IRequestHandler<CheckProgramInWishlistQuery, bool> {
  // ===== BASIC QUERY HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(GetAllProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting all programs with filters");

    var query = context.Programs.Where(p => p.DeletedAt == null);

    // Apply filters
    if (!string.IsNullOrEmpty(request.Search)) {
      query = query.Where(p => p.Title.Contains(request.Search) ||
                               (p.Description != null && p.Description.Contains(request.Search))
      );
    }

    if (request.Category.HasValue) query = query.Where(p => p.Category == request.Category.Value);

    if (request.Difficulty.HasValue) query = query.Where(p => p.Difficulty == request.Difficulty.Value);

    if (request.Status.HasValue) query = query.Where(p => p.Status == request.Status.Value);

    if (request.Visibility.HasValue) query = query.Where(p => p.Visibility == request.Visibility.Value);

    if (request.EnrollmentStatus.HasValue) query = query.Where(p => p.EnrollmentStatus == (EnrollmentStatus)request.EnrollmentStatus.Value);

    // Remove CreatorId filter since this property doesn't exist in the current Program model
    // if (!string.IsNullOrEmpty(request.CreatorId)) query = query.Where(p => p.CreatorId == request.CreatorId);

    if (!request.IncludeArchived) query = query.Where(p => p.Status != ContentStatus.Archived);

    // Apply sorting
    query = request.SortBy?.ToLower() switch {
      "title" => request.SortDescending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
      "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
      "updatedat" => request.SortDescending ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
      "category" => request.SortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
      "difficulty" => request.SortDescending ? query.OrderByDescending(p => p.Difficulty) : query.OrderBy(p => p.Difficulty),
      _ => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
    };

    var programs = await query
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Retrieved {Count} programs", programs.Count);

    return programs;
  }

  public async Task<Program?> Handle(GetProgramByIdQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting program by ID: {ProgramId}", request.Id);

    var query = context.Programs.Where(p => p.Id == request.Id && p.DeletedAt == null);

    if (request.IncludeContent) query = query.Include(p => p.ProgramContents.Where(pc => !pc.IsDeleted));

    if (request.IncludeEnrollments) query = query.Include(p => p.ProgramUsers.Where(pu => !pu.IsDeleted));

    if (request.IncludeRatings) query = query.Include(p => p.ProgramRatings);

    var program = await query.FirstOrDefaultAsync(cancellationToken);

    if (program != null)
      logger.LogInformation("Found program: {ProgramId}", program.Id);
    else
      logger.LogWarning("Program not found: {ProgramId}", request.Id);

    return program;
  }

  public async Task<Program?> Handle(GetProgramBySlugQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting program by slug: {Slug}", request.Slug);

    var query = context.Programs.Where(p => p.Slug == request.Slug && p.DeletedAt == null);

    if (request.IncludeContent) query = query.Include(p => p.ProgramContents.Where(pc => !pc.IsDeleted));

    if (request.IncludeEnrollments) query = query.Include(p => p.ProgramUsers.Where(pu => !pu.IsDeleted));

    if (request.IncludeRatings) query = query.Include(p => p.ProgramRatings);

    var program = await query.FirstOrDefaultAsync(cancellationToken);

    if (program != null)
      logger.LogInformation("Found program by slug: {Slug}", request.Slug);
    else
      logger.LogWarning("Program not found by slug: {Slug}", request.Slug);

    return program;
  }

  public async Task<Program?> Handle(GetPublishedProgramBySlugQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting published program by slug: {Slug}", request.Slug);

    var query = context.Programs
                       .Where(p => p.Slug == request.Slug &&
                                   p.DeletedAt == null &&
                                   p.Status == ContentStatus.Published &&
                                   p.Visibility == AccessLevel.Public
                       );

    if (request.IncludeContent) query = query.Include(p => p.ProgramContents.Where(pc => !pc.IsDeleted));

    var program = await query.FirstOrDefaultAsync(cancellationToken);

    if (program != null)
      logger.LogInformation("Found published program by slug: {Slug}", request.Slug);
    else
      logger.LogWarning("Published program not found by slug: {Slug}", request.Slug);

    return program;
  }

  // ===== SEARCH AND FILTER HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(SearchProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Searching programs with term: {SearchTerm}", request.SearchTerm);

    var query = context.Programs
                       .Where(p => p.DeletedAt == null &&
                                   p.Status == ContentStatus.Published &&
                                   p.Visibility == AccessLevel.Public
                       );

    // Text search
    query = query.Where(p => p.Title.Contains(request.SearchTerm) ||
                             (p.Description != null && p.Description.Contains(request.SearchTerm))
    );

    // Apply filters
    if (request.Category.HasValue) query = query.Where(p => p.Category == request.Category.Value);

    if (request.Difficulty.HasValue) query = query.Where(p => p.Difficulty == request.Difficulty.Value);

    if (request.MinEstimatedHours.HasValue) query = query.Where(p => p.EstimatedHours >= request.MinEstimatedHours.Value);

    if (request.MaxEstimatedHours.HasValue) query = query.Where(p => p.EstimatedHours <= request.MaxEstimatedHours.Value);

    if (request.MinRating.HasValue) {
      query = query.Where(p => p.ProgramRatings.Any() &&
                               p.ProgramRatings.Average(pr => pr.Rating) >= request.MinRating.Value
      );
    }

    if (request.AvailableForEnrollment) {
      query = query.Where(p => p.EnrollmentStatus == EnrollmentStatus.Open &&
                               (p.MaxEnrollments == null || p.ProgramUsers.Count(pu => pu.IsActive) < p.MaxEnrollments) &&
                               (p.EnrollmentDeadline == null || p.EnrollmentDeadline > DateTime.UtcNow)
      );
    }

    var programs = await query
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs matching search", programs.Count);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByCategoryQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by category: {Category}", request.Category);

    var query = context.Programs.Where(p => p.Category == request.Category && p.DeletedAt == null);

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == AccessLevel.Public); }

    var programs = await query
                         .OrderByDescending(p => p.CreatedAt)
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs in category {Category}", programs.Count, request.Category);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByDifficultyQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by difficulty: {Difficulty}", request.Difficulty);

    var query = context.Programs.Where(p => p.Difficulty == request.Difficulty && p.DeletedAt == null);

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == AccessLevel.Public); }

    var programs = await query
                         .OrderByDescending(p => p.CreatedAt)
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs with difficulty {Difficulty}", programs.Count, request.Difficulty);

    return programs;
  }

  public async Task<IEnumerable<Program>> Handle(GetProgramsByCreatorQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting programs by creator: {CreatorId}", request.CreatorId);

    // CreatorId property doesn't exist in current Program model, return empty for now
    // var query = context.Programs.Where(p => p.CreatorId == request.CreatorId && p.DeletedAt == null);
    var query = context.Programs.Where(p => false); // Return empty until CreatorId is added to model

    if (request.OnlyPublished) { query = query.Where(p => p.Status == ContentStatus.Published && p.Visibility == AccessLevel.Public); }

    var programs = await query
                         .OrderByDescending(p => p.CreatedAt)
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs by creator {CreatorId}", programs.Count, request.CreatorId);

    return programs;
  }

  // ===== ENROLLMENT QUERY HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(GetUserEnrolledProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting enrolled programs for user: {UserId}", request.UserId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid
    var query = context.Programs
                       .Where(p => p.DeletedAt == null &&
                                   p.ProgramUsers.Any(pu => pu.UserId == userGuid && (!request.OnlyActive || pu.IsActive))
                       );

    var programs = await query
                         .OrderByDescending(p => p.ProgramUsers.First(pu => pu.UserId == userGuid).JoinedAt) // Fixed property name
                         .Skip(request.Skip)
                         .Take(request.Take)
                         .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} enrolled programs for user {UserId}", programs.Count, request.UserId);

    return programs;
  }

  public async Task<IEnumerable<ProgramUser>> Handle(GetProgramEnrollmentsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting enrollments for program: {ProgramId}", request.ProgramId);

    var query = context.ProgramUsers.Where(pu => pu.ProgramId == request.ProgramId);

    if (request.OnlyActive) query = query.Where(pu => pu.IsActive);

    var enrollments = await query
                            .OrderByDescending(pu => pu.JoinedAt) // Fixed property name
                            .Skip(request.Skip)
                            .Take(request.Take)
                            .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} enrollments for program {ProgramId}", enrollments.Count, request.ProgramId);

    return enrollments;
  }

  public async Task<ProgramUser?> Handle(CheckUserEnrollmentQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Checking enrollment for user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid
    var enrollment = await context.ProgramUsers
                                  .Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == userGuid)
                                  .FirstOrDefaultAsync(cancellationToken);

    return enrollment;
  }

  // ===== CONTENT QUERY HANDLERS =====

  public async Task<IEnumerable<ProgramContent>> Handle(GetProgramContentQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting content for program: {ProgramId}", request.ProgramId);

    var query = context.ProgramContents
                       .Where(pc => pc.ProgramId == request.ProgramId && !pc.IsDeleted);

    if (request.OnlyVisible) {
      query = query.Where(pc => pc.Visibility == Visibility.Published); // Fixed to use ProgramContent's own properties
    }

    var content = await query
                        .OrderBy(pc => pc.SortOrder) // Fixed property name from Order to SortOrder
                        .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} content items for program {ProgramId}", content.Count, request.ProgramId);

    return content;
  }

  public async Task<ProgramUserProgress?> Handle(GetUserProgramProgressQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting progress for user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid
    var enrollment = await context.ProgramUsers
                                  .Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == userGuid && pu.IsActive)
                                  .FirstOrDefaultAsync(cancellationToken);

    if (enrollment == null) return null;

    var totalContent = await context.ProgramContents
                                    .Where(pc => pc.ProgramId == request.ProgramId && !pc.IsDeleted)
                                    .CountAsync(cancellationToken);

    // Note: This is a simplified implementation. You might want to track actual content completion
    var completedContent = 0; // This would need to be calculated from actual progress tracking
    var timeSpent = TimeSpan.Zero; // This would need to be calculated from actual time tracking
    var lastActivity = enrollment.UpdatedAt;
    var isCompleted = enrollment.CompletedAt.HasValue; // Fixed: use CompletedAt instead of IsCompleted
    var completedAt = enrollment.CompletedAt; // Fixed: use CompletedAt directly

    var progress = new ProgramUserProgress(
      request.ProgramId,
      request.UserId,
      completedContent,
      totalContent,
      totalContent > 0 ? (decimal)completedContent / totalContent * 100 : 0,
      timeSpent,
      lastActivity,
      isCompleted,
      completedAt
    );

    return progress;
  }

  // ===== STATISTICS HANDLERS =====

  public async Task<ProgramStatistics> Handle(GetProgramStatisticsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting statistics for program: {ProgramId}", request.ProgramId);

    var totalEnrollments = await context.ProgramUsers
                                        .Where(pu => pu.ProgramId == request.ProgramId)
                                        .CountAsync(cancellationToken);

    var activeEnrollments = await context.ProgramUsers
                                         .Where(pu => pu.ProgramId == request.ProgramId && pu.IsActive)
                                         .CountAsync(cancellationToken);

    var completedEnrollments = await context.ProgramUsers
                                            .Where(pu => pu.ProgramId == request.ProgramId && pu.CompletedAt.HasValue) // Fixed: use CompletedAt instead of IsCompleted
                                            .CountAsync(cancellationToken);

    var ratings = await context.ProgramRatings
                               .Where(pr => pr.ProgramId == request.ProgramId)
                               .ToListAsync(cancellationToken);

    var averageRating = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;
    var totalRatings = ratings.Count;
    var completionRate = totalEnrollments > 0 ? (decimal)completedEnrollments / totalEnrollments : 0;

    // Note: Average completion time would need actual time tracking
    var averageCompletionTime = TimeSpan.Zero;

    var statistics = new ProgramStatistics(
      request.ProgramId,
      totalEnrollments,
      activeEnrollments,
      completedEnrollments,
      averageRating,
      totalRatings,
      completionRate,
      averageCompletionTime
    );

    return statistics;
  }

  public async Task<GlobalProgramStatistics> Handle(GetGlobalProgramStatisticsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting global program statistics");

    var query = context.Programs.Where(p => p.DeletedAt == null);

    if (request.FromDate.HasValue) query = query.Where(p => p.CreatedAt >= request.FromDate.Value);

    if (request.ToDate.HasValue) query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

    var totalPrograms = await query.CountAsync(cancellationToken);
    var publishedPrograms = await query.Where(p => p.Status == ContentStatus.Published).CountAsync(cancellationToken);

    var enrollmentQuery = context.ProgramUsers.AsQueryable();
    if (request.FromDate.HasValue) enrollmentQuery = enrollmentQuery.Where(pu => pu.JoinedAt >= request.FromDate.Value); // Fixed property name
    if (request.ToDate.HasValue) enrollmentQuery = enrollmentQuery.Where(pu => pu.JoinedAt <= request.ToDate.Value); // Fixed property name

    var totalEnrollments = await enrollmentQuery.CountAsync(cancellationToken);
    var activeEnrollments = await enrollmentQuery.Where(pu => pu.IsActive).CountAsync(cancellationToken);

    var allRatings = await context.ProgramRatings.ToListAsync(cancellationToken);
    var averageRating = allRatings.Count > 0 ? allRatings.Average(r => r.Rating) : 0;
    var totalRatings = allRatings.Count;

    // Most popular category and difficulty (simplified)
    var mostPopularCategory = await context.Programs
                                           .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                                           .GroupBy(p => p.Category)
                                           .OrderByDescending(g => g.Count())
                                           .Select(g => g.Key)
                                           .FirstOrDefaultAsync(cancellationToken);

    var mostPopularDifficulty = await context.Programs
                                             .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                                             .GroupBy(p => p.Difficulty)
                                             .OrderByDescending(g => g.Count())
                                             .Select(g => g.Key)
                                             .FirstOrDefaultAsync(cancellationToken);

    var statistics = new GlobalProgramStatistics(
      totalPrograms,
      publishedPrograms,
      totalEnrollments,
      activeEnrollments,
      averageRating,
      totalRatings,
      mostPopularCategory,
      mostPopularDifficulty
    );

    return statistics;
  }

  public async Task<CreatorProgramStatistics> Handle(GetCreatorProgramStatisticsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting program statistics for creator: {CreatorId}", request.CreatorId);

    // CreatorId property doesn't exist in current Program model, return empty statistics for now
    var query = context.Programs.Where(p => false && p.DeletedAt == null); // Return empty until CreatorId is added to model

    if (request.FromDate.HasValue) query = query.Where(p => p.CreatedAt >= request.FromDate.Value);

    if (request.ToDate.HasValue) query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

    var totalPrograms = await query.CountAsync(cancellationToken);
    var publishedPrograms = await query.Where(p => p.Status == ContentStatus.Published).CountAsync(cancellationToken);

    var programIds = await query.Select(p => p.Id).ToListAsync(cancellationToken);

    var enrollmentQuery = context.ProgramUsers.Where(pu => programIds.Contains(pu.ProgramId));
    if (request.FromDate.HasValue) enrollmentQuery = enrollmentQuery.Where(pu => pu.JoinedAt >= request.FromDate.Value); // Fixed property name
    if (request.ToDate.HasValue) enrollmentQuery = enrollmentQuery.Where(pu => pu.JoinedAt <= request.ToDate.Value); // Fixed property name

    var totalEnrollments = await enrollmentQuery.CountAsync(cancellationToken);
    var activeEnrollments = await enrollmentQuery.Where(pu => pu.IsActive).CountAsync(cancellationToken);

    var ratings = await context.ProgramRatings
                               .Where(pr => programIds.Contains(pr.ProgramId))
                               .ToListAsync(cancellationToken);

    var averageRating = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;
    var totalRatings = ratings.Count;

    // Note: Average completion rate would need actual completion tracking
    var averageCompletionRate = 0m;

    var statistics = new CreatorProgramStatistics(
      request.CreatorId,
      totalPrograms,
      publishedPrograms,
      totalEnrollments,
      activeEnrollments,
      averageRating,
      totalRatings,
      averageCompletionRate
    );

    return statistics;
  }

  // ===== TRENDING AND RECOMMENDATION HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(GetPopularProgramsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting popular programs");

    var sinceDate = DateTime.UtcNow.AddDays(-request.DaysBack);

    var programs = await context.Programs
                                .Where(p => p.DeletedAt == null &&
                                            p.Status == ContentStatus.Published &&
                                            p.Visibility == AccessLevel.Public
                                )
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

    var sinceDate = DateTime.UtcNow.AddDays(-request.DaysBack);

    var programs = await context.Programs
                                .Where(p => p.DeletedAt == null &&
                                            p.Status == ContentStatus.Published &&
                                            p.Visibility == AccessLevel.Public &&
                                            p.CreatedAt >= sinceDate // Fixed: use CreatedAt instead of PublishedAt
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
    var programs = await context.Programs
                                .Where(p => p.DeletedAt == null &&
                                            p.Status == ContentStatus.Published &&
                                            p.Visibility == AccessLevel.Public
                                )
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
    var userEnrollments = await context.ProgramUsers
                                       .Where(pu => pu.UserId == userGuid && pu.IsActive) // Fixed: use converted Guid
                                       .Select(pu => pu.Program)
                                       .ToListAsync(cancellationToken);

    var userCategories = userEnrollments.Select(p => p.Category).Distinct().ToList();
    var userDifficulties = userEnrollments.Select(p => p.Difficulty).Distinct().ToList();

    var enrolledProgramIds = userEnrollments.Select(p => p.Id).ToList();

    var recommendations = await context.Programs
                                       .Where(p => p.DeletedAt == null &&
                                                   p.Status == ContentStatus.Published &&
                                                   p.Visibility == AccessLevel.Public &&
                                                   !enrolledProgramIds.Contains(p.Id) &&
                                                   (userCategories.Contains(p.Category) || userDifficulties.Contains(p.Difficulty))
                                       )
                                       .OrderByDescending(p => p.ProgramRatings.Count > 0 ? p.ProgramRatings.Average(pr => pr.Rating) : 0)
                                       .Take(request.Take)
                                       .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} recommended programs for user {UserId}", recommendations.Count, request.UserId);

    return recommendations;
  }

  // ===== RATING QUERY HANDLERS =====

  public async Task<IEnumerable<ProgramRating>> Handle(GetProgramRatingsQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting ratings for program: {ProgramId}", request.ProgramId);

    var ratings = await context.ProgramRatings
                               .Where(pr => pr.ProgramId == request.ProgramId)
                               .OrderByDescending(pr => pr.CreatedAt)
                               .Skip(request.Skip)
                               .Take(request.Take)
                               .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} ratings for program {ProgramId}", ratings.Count, request.ProgramId);

    return ratings;
  }

  public async Task<ProgramRating?> Handle(GetUserProgramRatingQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.ProgramRatings
                              .Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId) // UserId is string in ProgramRating model
                              .FirstOrDefaultAsync(cancellationToken);

    return rating;
  }

  // ===== WISHLIST QUERY HANDLERS =====

  public async Task<IEnumerable<Program>> Handle(GetUserWishlistQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting wishlist for user: {UserId}", request.UserId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid
    var programs = await context.Programs
                                .Where(p => p.DeletedAt == null &&
                                            p.ProgramWishlists.Any(pw => pw.UserId == userGuid) // Fixed: use converted Guid
                                )
                                .OrderByDescending(p => p.ProgramWishlists.First(pw => pw.UserId == userGuid).CreatedAt) // Fixed: use converted Guid
                                .Skip(request.Skip)
                                .Take(request.Take)
                                .ToListAsync(cancellationToken);

    logger.LogInformation("Found {Count} programs in wishlist for user {UserId}", programs.Count, request.UserId);

    return programs;
  }

  public async Task<bool> Handle(CheckProgramInWishlistQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Checking if program {ProgramId} is in wishlist for user {UserId}", request.ProgramId, request.UserId);

    var userGuid = Guid.Parse(request.UserId); // Convert string UserId to Guid
    var exists = await context.ProgramWishlists
                              .AnyAsync(pw => pw.ProgramId == request.ProgramId && pw.UserId == userGuid, cancellationToken); // Fixed: use converted Guid

    return exists;
  }
}
