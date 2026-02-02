namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Base interface for recommendation strategies
/// </summary>
public interface IRecommendationStrategy
{
    /// <summary>
    /// The type of recommendations this strategy produces
    /// </summary>
    RecommendationType Type { get; }

    /// <summary>
    /// Priority of this strategy (higher = evaluated first)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Generate recommendations for a user
    /// </summary>
    /// <param name="userId">The user to generate recommendations for</param>
    /// <param name="tenantId">Optional tenant context</param>
    /// <param name="excludeCourseIds">Course IDs to exclude (already completed/enrolled)</param>
    /// <param name="maxResults">Maximum recommendations to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommendation candidates with scores</returns>
    Task<IEnumerable<RecommendationCandidate>> GenerateAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<Guid> excludeCourseIds,
        int maxResults,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a recommendation candidate before final scoring
/// </summary>
public record RecommendationCandidate(
    Guid CourseId,
    RecommendationType Type,
    double Score,
    string? Reason);
