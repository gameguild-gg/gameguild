namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Interface for the recommendation engine that orchestrates multiple strategies
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Generate personalized recommendations for a user using all available strategies
    /// </summary>
    /// <param name="userId">The user to generate recommendations for</param>
    /// <param name="tenantId">Optional tenant context</param>
    /// <param name="maxResults">Maximum recommendations to return</param>
    /// <param name="types">Optional filter for specific recommendation types</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of recommendations</returns>
    Task<IEnumerable<CourseRecommendation>> GenerateRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        int maxResults = 10,
        RecommendationType[]? types = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh recommendations for a user (clear expired, generate new)
    /// </summary>
    Task RefreshRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}
