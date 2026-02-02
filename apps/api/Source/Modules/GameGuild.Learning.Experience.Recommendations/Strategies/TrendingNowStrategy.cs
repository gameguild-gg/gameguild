using GameGuild.Abstractions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Recommends courses with highest recent enrollment velocity (trending)
/// </summary>
public class TrendingNowStrategy(IApplicationDbContext context) : IRecommendationStrategy
{
    public RecommendationType Type => RecommendationType.TrendingNow;
    public int Priority => 60;

    private static readonly TimeSpan TrendingWindow = TimeSpan.FromDays(7);

    public async Task<IEnumerable<RecommendationCandidate>> GenerateAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<Guid> excludeCourseIds,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var excludeSet = excludeCourseIds.ToHashSet();
        var trendingCutoff = DateTime.UtcNow.Subtract(TrendingWindow);

        // Get courses with high recent enrollment velocity
        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == GameGuild.Enums.ContentStatus.Published)
            .Where(p => !excludeSet.Contains(p.Id));

        // Apply tenant filter
        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == tenantId);
        }

        var trendingCourses = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Category,
                RecentEnrollments = p.ProgramUsers.Count(pu => pu.JoinedAt >= trendingCutoff),
                TotalEnrollments = p.ProgramUsers.Count(pu => pu.IsActive),
                AverageRating = p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m
            })
            .Where(p => p.RecentEnrollments > 0)
            .OrderByDescending(p => p.RecentEnrollments)
            .ThenByDescending(p => p.AverageRating)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        return trendingCourses.Select((course, index) => new RecommendationCandidate(
            CourseId: course.Id,
            Type: Type,
            Score: CalculateTrendScore(course.RecentEnrollments, course.TotalEnrollments, (double)course.AverageRating, maxResults - index),
            Reason: $"Trending with {course.RecentEnrollments} new enrollments this week"));
    }

    private static double CalculateTrendScore(int recentEnrollments, int totalEnrollments, double avgRating, int rankBonus)
    {
        // Score components:
        // - 40% recent enrollment velocity
        // - 20% overall popularity (log scale)
        // - 20% rating
        // - 20% rank bonus

        var velocityScore = Math.Min(recentEnrollments / 50.0, 1.0) * 0.4;
        var popularityScore = Math.Min(Math.Log10(totalEnrollments + 1) / 3.0, 1.0) * 0.2;
        var ratingScore = (avgRating / 5.0) * 0.2;
        var rankScore = (rankBonus / 10.0) * 0.2;

        return Math.Clamp(velocityScore + popularityScore + ratingScore + rankScore, 0.0, 1.0);
    }
}
