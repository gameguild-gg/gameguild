namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Service interface for recommendation operations
/// </summary>
public interface IRecommendationService
{
    // ===== RECOMMENDATIONS =====
    
    /// <summary>
    /// Get personalized recommendations for a user
    /// </summary>
    Task<IEnumerable<CourseRecommendation>> GetUserRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        RecommendationType? type = null,
        bool includeViewed = false,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate new recommendations for a user
    /// </summary>
    Task<IEnumerable<CourseRecommendation>> GenerateRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        int maxResults = 10,
        RecommendationType[]? types = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a recommendation as viewed
    /// </summary>
    Task MarkRecommendationViewedAsync(Guid recommendationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismiss a recommendation
    /// </summary>
    Task DismissRecommendationAsync(Guid recommendationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh recommendations (clear expired, generate new)
    /// </summary>
    Task RefreshRecommendationsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recommendation statistics for a user
    /// </summary>
    Task<RecommendationStatisticsDto> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);

    // ===== USER LEARNING PROFILE =====

    /// <summary>
    /// Get user's learning profile
    /// </summary>
    Task<UserLearningProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create user's learning profile
    /// </summary>
    Task<UserLearningProfile> GetOrCreateUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user's learning profile
    /// </summary>
    Task<UserLearningProfile> UpdateUserProfileAsync(
        Guid userId,
        CreateOrUpdateLearningProfileDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a skill to user's profile
    /// </summary>
    Task<UserLearningProfile> AddSkillToProfileAsync(Guid userId, string skill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a skill from user's profile
    /// </summary>
    Task<UserLearningProfile> RemoveSkillFromProfileAsync(Guid userId, string skill, CancellationToken cancellationToken = default);

    // ===== DISCOVERY =====

    /// <summary>
    /// Get popular courses
    /// </summary>
    Task<IEnumerable<PopularCourseDto>> GetPopularCoursesAsync(
        Guid? tenantId = null,
        string? category = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get trending courses
    /// </summary>
    Task<IEnumerable<TrendingCourseDto>> GetTrendingCoursesAsync(
        Guid? tenantId = null,
        int daysWindow = 7,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get courses similar to a specific course
    /// </summary>
    Task<IEnumerable<SimilarCourseDto>> GetSimilarCoursesAsync(
        Guid courseId,
        Guid? tenantId = null,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
