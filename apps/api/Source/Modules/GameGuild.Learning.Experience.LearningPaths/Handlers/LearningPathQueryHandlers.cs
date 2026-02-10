using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Query handlers for Learning Paths module
/// </summary>
public sealed class LearningPathQueryHandlers(IApplicationDbContext context, ILogger<LearningPathQueryHandlers> logger)
    : IRequestHandler<GetPublishedPathsQuery, IEnumerable<LearningPath>>,
      IRequestHandler<GetPathBySlugQuery, LearningPath?>,
      IRequestHandler<GetPathByIdQuery, LearningPath?>,
      IRequestHandler<GetFeaturedPathsQuery, IEnumerable<LearningPath>>,
      IRequestHandler<GetPathsByCreatorQuery, IEnumerable<LearningPath>>,
      IRequestHandler<GetAllPathsQuery, IEnumerable<LearningPath>>,
      IRequestHandler<SearchPathsQuery, IEnumerable<LearningPath>>,
      IRequestHandler<GetUserEnrolledPathsQuery, IEnumerable<LearningPathEnrollment>>,
      IRequestHandler<GetUserPathEnrollmentQuery, LearningPathEnrollment?>,
      IRequestHandler<CheckPathEnrollmentQuery, bool>,
      IRequestHandler<GetPathEnrollmentsQuery, IEnumerable<LearningPathEnrollment>>,
      IRequestHandler<GetUserPathProgressQuery, LearningPathEnrollmentDto?>,
      IRequestHandler<GetPathStatisticsQuery, LearningPathStatisticsDto?>,
      IRequestHandler<GetPopularPathsQuery, IEnumerable<LearningPath>>,
      IRequestHandler<GetUserCompletedPathsQuery, IEnumerable<LearningPathEnrollment>>
{
    // ===== LEARNING PATH QUERY HANDLERS =====

    public async Task<IEnumerable<LearningPath>> Handle(GetPublishedPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting published learning paths");

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.IsPublished);

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId || lp.TenantId == null);
        }

        if (request.Difficulty.HasValue)
        {
            query = query.Where(lp => lp.Difficulty == request.Difficulty.Value);
        }

        var result = await query
            .OrderByDescending(lp => lp.IsFeatured)
            .ThenByDescending(lp => lp.EnrollmentCount)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Found {Count} published learning paths", result.Count);
        return result;
    }

    public async Task<LearningPath?> Handle(GetPathBySlugQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting learning path by slug: {Slug}", request.Slug);

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.Slug == request.Slug);

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId || lp.TenantId == null);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LearningPath?> Handle(GetPathByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting learning path by ID: {Id}", request.Id);

        var query = context.Set<LearningPath>()
            .Where(lp => lp.DeletedAt == null && lp.Id == request.Id);

        if (request.IncludeCourses)
        {
            query = query.Include(lp => lp.Courses);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<LearningPath>> Handle(GetFeaturedPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting featured learning paths");

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.IsPublished && lp.IsFeatured);

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId || lp.TenantId == null);
        }

        return await query
            .OrderByDescending(lp => lp.EnrollmentCount)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<LearningPath>> Handle(GetPathsByCreatorQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting learning paths by creator: {CreatorId}", request.CreatorId);

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.CreatorId == request.CreatorId);

        if (!request.IncludeUnpublished)
        {
            query = query.Where(lp => lp.IsPublished);
        }

        return await query
            .OrderByDescending(lp => lp.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<LearningPath>> Handle(GetAllPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all learning paths (admin)");

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null);

        if (!request.IncludeUnpublished)
        {
            query = query.Where(lp => lp.IsPublished);
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId);
        }

        return await query
            .OrderByDescending(lp => lp.IsFeatured)
            .ThenByDescending(lp => lp.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<LearningPath>> Handle(SearchPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching learning paths: {SearchTerm}", request.SearchTerm);

        var searchTermLower = request.SearchTerm.ToLower();

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.IsPublished)
            .Where(lp => lp.Title.ToLower().Contains(searchTermLower) ||
                         (lp.Description != null && lp.Description.ToLower().Contains(searchTermLower)));

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId || lp.TenantId == null);
        }

        if (request.Difficulty.HasValue)
        {
            query = query.Where(lp => lp.Difficulty == request.Difficulty.Value);
        }

        var result = await query
            .OrderByDescending(lp => lp.IsFeatured)
            .ThenByDescending(lp => lp.EnrollmentCount)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Found {Count} learning paths for search: {SearchTerm}", result.Count, request.SearchTerm);
        return result;
    }

    // ===== ENROLLMENT QUERY HANDLERS =====

    public async Task<IEnumerable<LearningPathEnrollment>> Handle(GetUserEnrolledPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting enrolled paths for user: {UserId}", request.UserId);

        var query = context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null && e.UserId == request.UserId);

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(e => e.EnrolledAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LearningPathEnrollment?> Handle(GetUserPathEnrollmentQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting enrollment for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);

        return await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.LearningPathId == request.LearningPathId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Handle(CheckPathEnrollmentQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking enrollment for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);

        return await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .AnyAsync(e => e.UserId == request.UserId && e.LearningPathId == request.LearningPathId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<LearningPathEnrollment>> Handle(GetPathEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting enrollments for path: {PathId}", request.LearningPathId);

        var query = context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null && e.LearningPathId == request.LearningPathId);

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(e => e.EnrolledAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LearningPathEnrollmentDto?> Handle(GetUserPathProgressQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting progress for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);

        var enrollment = await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.LearningPathId == request.LearningPathId, cancellationToken).ConfigureAwait(false);

        return enrollment?.ToDto();
    }

    // ===== STATISTICS QUERY HANDLERS =====

    public async Task<LearningPathStatisticsDto?> Handle(GetPathStatisticsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting statistics for path: {PathId}", request.LearningPathId);

        var path = await context.Set<LearningPath>()
            .Where(lp => lp.DeletedAt == null && lp.Id == request.LearningPathId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (path == null) return null;

        var enrollments = await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null && e.LearningPathId == request.LearningPathId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalEnrollments = enrollments.Count;
        var activeEnrollments = enrollments.Count(e => e.Status == LearningPathEnrollmentStatus.InProgress);
        var completedEnrollments = enrollments.Count(e => e.Status == LearningPathEnrollmentStatus.Completed);
        var completionRate = totalEnrollments > 0 ? (double)completedEnrollments / totalEnrollments : 0;
        var averageProgress = enrollments.Count > 0 ? enrollments.Average(e => e.Progress) : 0;

        var completedWithTime = enrollments
            .Where(e => e.Status == LearningPathEnrollmentStatus.Completed && e.CompletedAt.HasValue)
            .Select(e => e.CompletedAt!.Value - e.EnrolledAt)
            .ToList();

        var averageCompletionTime = completedWithTime.Count > 0
            ? TimeSpan.FromTicks((long)completedWithTime.Average(t => t.Ticks))
            : TimeSpan.Zero;

        return new LearningPathStatisticsDto(
            LearningPathId: request.LearningPathId,
            TotalEnrollments: totalEnrollments,
            ActiveEnrollments: activeEnrollments,
            CompletedEnrollments: completedEnrollments,
            CompletionRate: completionRate,
            AverageProgress: averageProgress,
            AverageCompletionTime: averageCompletionTime
        );
    }

    public async Task<IEnumerable<LearningPath>> Handle(GetPopularPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting popular learning paths for last {Days} days", request.DaysBack);

        var cutoffDate = SystemClock.UtcNow.AddDays(-request.DaysBack);

        // Get enrollment counts per path in the time period
        var popularPathIds = await context.Set<LearningPathEnrollment>()
            .Where(e => e.EnrolledAt >= cutoffDate)
            .GroupBy(e => e.LearningPathId)
            .Select(g => new { LearningPathId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(request.Take)
            .Select(x => x.LearningPathId)
            .ToListAsync(cancellationToken);

        var query = context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.IsPublished && popularPathIds.Contains(lp.Id));

        if (request.TenantId.HasValue)
        {
            query = query.Where(lp => lp.TenantId == request.TenantId || lp.TenantId == null);
        }

        var paths = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Order by the enrollment count
        return paths.OrderBy(p => popularPathIds.IndexOf(p.Id));
    }

    public async Task<IEnumerable<LearningPathEnrollment>> Handle(GetUserCompletedPathsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting completed paths for user: {UserId}", request.UserId);

        return await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null && 
                       e.UserId == request.UserId && 
                       e.Status == LearningPathEnrollmentStatus.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);
    }
}
