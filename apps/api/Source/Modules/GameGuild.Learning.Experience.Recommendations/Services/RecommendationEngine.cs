using GameGuild.Abstractions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Recommendation engine that orchestrates multiple strategies to generate personalized recommendations
/// </summary>
public class RecommendationEngine : IRecommendationEngine
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IRecommendationStrategy> _strategies;
    private readonly ILogger<RecommendationEngine> _logger;

    private const int DefaultMaxResults = 10;
    private const int MaxCandidatesPerStrategy = 20;

    public RecommendationEngine(
        IApplicationDbContext context,
        IEnumerable<IRecommendationStrategy> strategies,
        ILogger<RecommendationEngine> logger)
    {
        _context = context;
        _strategies = strategies.OrderByDescending(s => s.Priority);
        _logger = logger;
    }

    public async Task<IEnumerable<CourseRecommendation>> GenerateRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        int maxResults = DefaultMaxResults,
        RecommendationType[]? types = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating recommendations for user {UserId} in tenant {TenantId}, max {MaxResults}",
            userId, tenantId, maxResults);

        // Get courses to exclude (already enrolled, completed, or recently dismissed)
        var excludeCourseIds = await GetExcludedCourseIdsAsync(userId, cancellationToken);

        // Get existing valid recommendations to avoid duplicates
        var existingRecommendations = await _context.Set<CourseRecommendation>()
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Where(r => !r.IsDismissed)
            .Where(r => r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var existingCourseIds = existingRecommendations.Select(r => r.CourseId).ToHashSet();

        // Filter strategies by requested types
        var activeStrategies = types != null && types.Length > 0
            ? _strategies.Where(s => types.Contains(s.Type))
            : _strategies;

        // Collect candidates from all strategies
        var allCandidates = new List<RecommendationCandidate>();

        foreach (var strategy in activeStrategies)
        {
            try
            {
                var candidates = await strategy.GenerateAsync(
                    userId,
                    tenantId,
                    excludeCourseIds.Union(existingCourseIds),
                    MaxCandidatesPerStrategy,
                    cancellationToken);

                allCandidates.AddRange(candidates);
                _logger.LogDebug("Strategy {Strategy} generated {Count} candidates", strategy.Type, candidates.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Strategy {Strategy} failed, continuing with others", strategy.Type);
            }
        }

        // Deduplicate by course ID, keeping highest score
        var deduplicatedCandidates = allCandidates
            .GroupBy(c => c.CourseId)
            .Select(g => g.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .Take(maxResults)
            .ToList();

        // Convert candidates to recommendation entities
        var newRecommendations = deduplicatedCandidates.Select(c =>
            CourseRecommendation.Create(
                userId: userId,
                courseId: c.CourseId,
                type: c.Type,
                score: c.Score,
                reason: c.Reason,
                validFor: TimeSpan.FromDays(7))).ToList();

        // Persist new recommendations
        if (newRecommendations.Any())
        {
            _context.Set<CourseRecommendation>().AddRange(newRecommendations);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created {Count} new recommendations for user {UserId}", newRecommendations.Count, userId);
        }

        // Return combined: existing valid + new recommendations, ordered by score
        return existingRecommendations
            .Concat(newRecommendations)
            .Where(r => r.IsValid())
            .OrderByDescending(r => r.Score)
            .Take(maxResults);
    }

    public async Task RefreshRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing recommendations for user {UserId}", userId);

        // Mark expired recommendations as dismissed
        var expiredRecommendations = await _context.Set<CourseRecommendation>()
            .Where(r => r.UserId == userId)
            .Where(r => r.ExpiresAt <= DateTime.UtcNow)
            .Where(r => !r.IsDismissed)
            .ToListAsync(cancellationToken);

        foreach (var rec in expiredRecommendations)
        {
            rec.Dismiss();
        }

        if (expiredRecommendations.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Generate new recommendations
        await GenerateRecommendationsAsync(userId, tenantId, DefaultMaxResults, null, cancellationToken);
    }

    private async Task<HashSet<Guid>> GetExcludedCourseIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Get enrolled/completed courses
        var enrolledCourseIds = await _context.Set<ProgramUser>()
            .AsNoTracking()
            .Where(pu => pu.UserId == userId && pu.IsActive)
            .Select(pu => pu.ProgramId)
            .ToListAsync(cancellationToken);

        // Get recently dismissed recommendations (within 30 days)
        var dismissedCutoff = DateTime.UtcNow.AddDays(-30);
        var dismissedCourseIds = await _context.Set<CourseRecommendation>()
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Where(r => r.IsDismissed)
            .Where(r => r.UpdatedAt >= dismissedCutoff)
            .Select(r => r.CourseId)
            .ToListAsync(cancellationToken);

        return enrolledCourseIds.Concat(dismissedCourseIds).ToHashSet();
    }
}
