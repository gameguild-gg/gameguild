using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Recommendations;

// ===== RECOMMENDATION QUERY HANDLERS =====

public sealed class GetUserRecommendationsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetUserRecommendationsQueryHandler> logger)
    : IQueryHandler<GetUserRecommendationsQuery, IEnumerable<CourseRecommendation>>
{
    public async Task<IEnumerable<CourseRecommendation>> Handle(GetUserRecommendationsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting recommendations for user {UserId}", request.UserId);

        var query = context.Set<CourseRecommendation>()
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .Where(r => !r.IsDismissed)
            .Where(r => r.ExpiresAt > SystemClock.UtcNow);

        if (!request.IncludeViewed)
        {
            query = query.Where(r => !r.IsViewed);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(r => r.Type == request.Type.Value);
        }

        return await query
            .OrderByDescending(r => r.Score)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class GetRecommendationByIdQueryHandler(
    IApplicationDbContext context,
    ILogger<GetRecommendationByIdQueryHandler> logger)
    : IQueryHandler<GetRecommendationByIdQuery, CourseRecommendation?>
{
    public async Task<CourseRecommendation?> Handle(GetRecommendationByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting recommendation {Id} for user {UserId}", request.Id, request.UserId);

        return await context.Set<CourseRecommendation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == request.UserId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class GetRecommendationStatisticsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetRecommendationStatisticsQueryHandler> logger)
    : IQueryHandler<GetRecommendationStatisticsQuery, RecommendationStatisticsDto>
{
    public async Task<RecommendationStatisticsDto> Handle(GetRecommendationStatisticsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting recommendation statistics for user {UserId}", request.UserId);

        var recommendations = await context.Set<CourseRecommendation>()
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byType = recommendations
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return new RecommendationStatisticsDto(
            TotalRecommendations: recommendations.Count,
            ViewedCount: recommendations.Count(r => r.IsViewed),
            DismissedCount: recommendations.Count(r => r.IsDismissed),
            ConvertedCount: 0, // Would need enrollment tracking to compute
            ByType: byType);
    }
}

public sealed class HasPendingRecommendationsQueryHandler(
    IApplicationDbContext context,
    ILogger<HasPendingRecommendationsQueryHandler> logger)
    : IQueryHandler<HasPendingRecommendationsQuery, bool>
{
    public async Task<bool> Handle(HasPendingRecommendationsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking pending recommendations for user {UserId}", request.UserId);

        return await context.Set<CourseRecommendation>()
            .AsNoTracking()
            .AnyAsync(r => r.UserId == request.UserId 
                && !r.IsDismissed 
                && !r.IsViewed 
                && r.ExpiresAt > SystemClock.UtcNow, cancellationToken).ConfigureAwait(false);
    }
}

// ===== USER LEARNING PROFILE QUERY HANDLERS =====

public sealed class GetUserLearningProfileQueryHandler(
    IApplicationDbContext context,
    ILogger<GetUserLearningProfileQueryHandler> logger)
    : IQueryHandler<GetUserLearningProfileQuery, UserLearningProfile?>
{
    public async Task<UserLearningProfile?> Handle(GetUserLearningProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting learning profile for user {UserId}", request.UserId);

        return await context.Set<UserLearningProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class GetOrCreateUserLearningProfileQueryHandler(
    IApplicationDbContext context,
    ILogger<GetOrCreateUserLearningProfileQueryHandler> logger)
    : IQueryHandler<GetOrCreateUserLearningProfileQuery, UserLearningProfile>
{
    public async Task<UserLearningProfile> Handle(GetOrCreateUserLearningProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting or creating learning profile for user {UserId}", request.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(request.UserId);
            context.Set<UserLearningProfile>().Add(profile);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return profile;
    }
}

// ===== POPULAR/TRENDING QUERY HANDLERS =====

public sealed class GetPopularCoursesQueryHandler(
    IApplicationDbContext context,
    ILogger<GetPopularCoursesQueryHandler> logger)
    : IQueryHandler<GetPopularCoursesQuery, IEnumerable<PopularCourseDto>>
{
    public async Task<IEnumerable<PopularCourseDto>> Handle(GetPopularCoursesQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting popular courses with category {Category}", request.Category);

        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == ContentStatus.Published);

        if (request.TenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == request.TenantId);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(p => p.Category.ToString() == request.Category);
        }

        var results = await query
            .OrderByDescending(p => p.ProgramUsers.Count(pu => pu.IsActive))
            .ThenByDescending(p => p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.Thumbnail,
                Category = p.Category.ToString(),
                EnrollmentCount = p.ProgramUsers.Count(pu => pu.IsActive),
                AverageRating = p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m,
                TotalRatings = p.ProgramRatings.Count
            })
            .ToListAsync(cancellationToken);

        return results.Select(p => new PopularCourseDto(
            p.Id,
            p.Title,
            p.Description,
            p.Thumbnail,
            p.Category,
            p.EnrollmentCount,
            p.AverageRating,
            p.TotalRatings)).ToList();
    }
}

public sealed class GetTrendingCoursesQueryHandler(
    IApplicationDbContext context,
    ILogger<GetTrendingCoursesQueryHandler> logger)
    : IQueryHandler<GetTrendingCoursesQuery, IEnumerable<TrendingCourseDto>>
{
    public async Task<IEnumerable<TrendingCourseDto>> Handle(GetTrendingCoursesQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting trending courses for last {DaysWindow} days", request.DaysWindow);

        var cutoff = SystemClock.UtcNow.AddDays(-request.DaysWindow);

        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == ContentStatus.Published);

        if (request.TenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == request.TenantId);
        }

        var results = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.Thumbnail,
                Category = p.Category.ToString(),
                RecentEnrollments = p.ProgramUsers.Count(pu => pu.JoinedAt >= cutoff),
                TotalEnrollments = p.ProgramUsers.Count(pu => pu.IsActive),
                AverageRating = p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m
            })
            .Where(p => p.RecentEnrollments > 0)
            .OrderByDescending(p => p.RecentEnrollments)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return results.Select(p => new TrendingCourseDto(
            p.Id,
            p.Title,
            p.Description,
            p.Thumbnail,
            p.Category,
            p.RecentEnrollments,
            CalculateTrendScore(p.RecentEnrollments, p.TotalEnrollments, (double)p.AverageRating))).ToList();
    }

    private static decimal CalculateTrendScore(int recent, int total, double rating)
    {
        var velocity = recent / 7.0; // Enrollments per day
        var popularity = Math.Log10(total + 1);
        return (decimal)(velocity * 0.5 + popularity * 0.3 + (rating / 5.0) * 0.2);
    }
}

public sealed class GetSimilarCoursesQueryHandler(
    IApplicationDbContext context,
    ILogger<GetSimilarCoursesQueryHandler> logger)
    : IQueryHandler<GetSimilarCoursesQuery, IEnumerable<SimilarCourseDto>>
{
    public async Task<IEnumerable<SimilarCourseDto>> Handle(GetSimilarCoursesQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting similar courses for course {CourseId}", request.CourseId);

        // Get the source course
        var sourceCourse = await context.Set<Program>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.CourseId, cancellationToken).ConfigureAwait(false);

        if (sourceCourse == null)
        {
            return Enumerable.Empty<SimilarCourseDto>();
        }

        var sourceSkills = ParseSkills(sourceCourse.SkillsProvided ?? "");

        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == ContentStatus.Published)
            .Where(p => p.Id != request.CourseId)
            .Where(p => p.Category == sourceCourse.Category);

        if (request.TenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == request.TenantId);
        }

        var candidates = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.Thumbnail,
                p.Category,
                p.SkillsProvided
            })
            .Take(request.MaxResults * 3)
            .ToListAsync(cancellationToken);

        // Score by skill overlap
        return candidates
            .Select(c =>
            {
                var courseSkills = ParseSkills(c.SkillsProvided ?? "");
                var matching = sourceSkills.Intersect(courseSkills, StringComparer.OrdinalIgnoreCase).ToArray();
                var score = courseSkills.Count > 0 
                    ? (double)matching.Length / Math.Max(sourceSkills.Count, courseSkills.Count)
                    : 0.5; // Default score for same category

                return new SimilarCourseDto(
                    CourseId: c.Id,
                    Title: c.Title,
                    Description: c.Description,
                    Thumbnail: c.Thumbnail,
                    Category: c.Category.ToString(),
                    SimilarityScore: score,
                    MatchingTags: matching);
            })
            .OrderByDescending(c => c.SimilarityScore)
            .Take(request.MaxResults)
            .ToList();
    }

    private static List<string> ParseSkills(string skillsJson)
    {
        if (string.IsNullOrWhiteSpace(skillsJson)) return new List<string>();
        try
        {
            var trimmed = skillsJson.TrimStart();
            if (trimmed.StartsWith("[") || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
            }
            return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}

public sealed class GetPotentialLearnersQueryHandler(
    IApplicationDbContext context,
    ILogger<GetPotentialLearnersQueryHandler> logger)
    : IQueryHandler<GetPotentialLearnersQuery, IEnumerable<Guid>>
{
    public async Task<IEnumerable<Guid>> Handle(GetPotentialLearnersQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting potential learners for course {CourseId}", request.CourseId);

        // Get course category and skills
        var course = await context.Set<Program>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.CourseId, cancellationToken).ConfigureAwait(false);

        if (course == null)
        {
            return Enumerable.Empty<Guid>();
        }

        // Get already enrolled users
        var enrolledUsers = await context.Set<ProgramUser>()
            .AsNoTracking()
            .Where(pu => pu.ProgramId == request.CourseId)
            .Select(pu => pu.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Find users with matching preferences who aren't enrolled
        var query = context.Set<UserLearningProfile>()
            .AsNoTracking()
            .Where(p => !enrolledUsers.Contains(p.UserId));

        // Filter by category preference (if stored as JSON array)
        var categoryStr = course.Category.ToString();
        query = query.Where(p => p.PreferredCategories != null && p.PreferredCategories.Contains(categoryStr));

        return await query
            .OrderByDescending(p => p.LastActivityAt)
            .Take(request.MaxResults)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
