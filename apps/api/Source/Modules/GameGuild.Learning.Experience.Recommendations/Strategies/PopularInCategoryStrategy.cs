using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Recommends courses that are popular in the user's preferred categories
/// </summary>
public class PopularInCategoryStrategy(IApplicationDbContext context) : IRecommendationStrategy
{
    public RecommendationType Type => RecommendationType.PopularInCategory;
    public int Priority => 70;

    public async Task<IEnumerable<RecommendationCandidate>> GenerateAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<Guid> excludeCourseIds,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var excludeSet = excludeCourseIds.ToHashSet();

        // Get user's learning profile for preferred categories
        var profile = await context.Set<UserLearningProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);

        var preferredCategories = ParseCategories(profile?.PreferredCategories);

        // Query popular courses in preferred categories
        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == ContentStatus.Published)
            .Where(p => !excludeSet.Contains(p.Id));

        // Apply tenant filter
        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == tenantId);
        }

        // Filter by preferred categories if user has preferences
        if (preferredCategories.Any())
        {
            query = query.Where(p => preferredCategories.Contains(p.Category.ToString()));
        }

        // Order by average rating and enrollment count
        var popularCourses = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Category,
                AverageRating = p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m,
                EnrollmentCount = p.ProgramUsers.Count(pu => pu.IsActive)
            })
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.EnrollmentCount)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        return popularCourses.Select((course, index) => new RecommendationCandidate(
            CourseId: course.Id,
            Type: Type,
            Score: CalculateScore(course.AverageRating, course.EnrollmentCount, maxResults - index),
            Reason: $"Popular in {course.Category} with {course.AverageRating:F1}★ rating"));
    }

    private static List<string> ParseCategories(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        
        try
        {
            var categoryStrings = System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
            return categoryStrings?.ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static double CalculateScore(decimal avgRating, int enrollmentCount, int rankBonus)
    {
        // Score: 40% rating (0-5 scaled to 0-0.4) + 30% enrollment popularity + 30% rank bonus
        var ratingScore = (double)avgRating / 5.0 * 0.4;
        var popularityScore = Math.Min(enrollmentCount / 100.0, 1.0) * 0.3;
        var rankScore = (rankBonus / 10.0) * 0.3;
        return Math.Clamp(ratingScore + popularityScore + rankScore, 0.0, 1.0);
    }
}
