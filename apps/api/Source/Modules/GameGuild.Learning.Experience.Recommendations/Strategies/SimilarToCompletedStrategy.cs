using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Recommends courses similar to ones the user has completed
/// Based on category, difficulty, and skill tags
/// </summary>
public class SimilarToCompletedStrategy(IApplicationDbContext context) : IRecommendationStrategy
{
    public RecommendationType Type => RecommendationType.SimilarToCompleted;
    public int Priority => 80;

    public async Task<IEnumerable<RecommendationCandidate>> GenerateAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<Guid> excludeCourseIds,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var excludeSet = excludeCourseIds.ToHashSet();

        // Get user's completed courses
        var completedCourses = await context.Set<ProgramUser>()
            .AsNoTracking()
            .Where(pu => pu.UserId == userId && pu.IsActive)
            .Include(pu => pu.Program)
            .Select(pu => pu.Program)
            .Where(p => p != null && p.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (!completedCourses.Any())
        {
            return Enumerable.Empty<RecommendationCandidate>();
        }

        // Extract characteristics from completed courses
        var completedCategories = completedCourses
            .Select(c => c!.Category.ToString())
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        var completedDifficulties = completedCourses
            .Select(c => c!.Difficulty.ToString())
            .Distinct()
            .ToList();

        var completedSkills = completedCourses
            .Where(c => !string.IsNullOrEmpty(c!.SkillsProvided))
            .SelectMany(c => ParseSkills(c!.SkillsProvided!))
            .Distinct()
            .ToList();

        var completedCourseIds = completedCourses.Select(c => c!.Id).ToHashSet();

        // Find similar courses
        var query = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Status == ContentStatus.Published)
            .Where(p => !excludeSet.Contains(p.Id))
            .Where(p => !completedCourseIds.Contains(p.Id));

        // Apply tenant filter
        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == null || p.TenantId == tenantId);
        }

        // Filter by similar categories
        if (completedCategories.Any())
        {
            query = query.Where(p => completedCategories.Contains(p.Category.ToString()));
        }

        var similarCourses = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Category,
                p.Difficulty,
                p.SkillsProvided,
                AverageRating = p.ProgramRatings.Any() ? p.ProgramRatings.Average(r => r.Rating) : 0m
            })
            .Take(maxResults * 2) // Get more for better filtering
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Score and rank by similarity
        var scoredCourses = similarCourses
            .Select(course => new
            {
                Course = course,
                SimilarityScore = CalculateSimilarityScore(
                    course.Category.ToString(), completedCategories,
                    course.Difficulty.ToString(), completedDifficulties,
                    ParseSkills(course.SkillsProvided ?? ""), completedSkills,
                    (double)course.AverageRating)
            })
            .OrderByDescending(x => x.SimilarityScore)
            .Take(maxResults)
            .ToList();

        return scoredCourses.Select(item => new RecommendationCandidate(
            CourseId: item.Course.Id,
            Type: Type,
            Score: item.SimilarityScore,
            Reason: $"Similar to courses you've completed in {item.Course.Category}"));
    }

    private static List<string> ParseSkills(string skillsJson)
    {
        if (string.IsNullOrWhiteSpace(skillsJson)) return new List<string>();
        
        try
        {
            // Could be comma-separated or JSON array
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

    private static double CalculateSimilarityScore(
        string category,
        List<string> preferredCategories,
        string difficulty,
        List<string> preferredDifficulties,
        List<string> courseSkills,
        List<string> completedSkills,
        double avgRating)
    {
        double score = 0.0;

        // Category match: 30%
        if (preferredCategories.Contains(category))
        {
            var categoryRank = preferredCategories.IndexOf(category);
            score += 0.3 * (1.0 - (categoryRank * 0.1)); // Top category gets full 30%
        }

        // Difficulty progression: 20% (prefer next level up or same)
        if (preferredDifficulties.Contains(difficulty))
        {
            score += 0.20;
        }

        // Skill overlap: 30%
        if (courseSkills.Any() && completedSkills.Any())
        {
            var overlap = courseSkills.Count(s => completedSkills.Contains(s, StringComparer.OrdinalIgnoreCase));
            var overlapRatio = (double)overlap / Math.Max(courseSkills.Count, 1);
            score += 0.3 * overlapRatio;
        }

        // Rating bonus: 20%
        score += 0.2 * (avgRating / 5.0);

        return Math.Clamp(score, 0.0, 1.0);
    }
}
