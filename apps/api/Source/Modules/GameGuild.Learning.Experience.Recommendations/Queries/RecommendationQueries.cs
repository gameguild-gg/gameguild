using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Recommendations;

// ===== RECOMMENDATION QUERIES =====

/// <summary>
/// Get personalized recommendations for a user
/// </summary>
public record GetUserRecommendationsQuery(
    Guid UserId,
    Guid? TenantId = null,
    RecommendationType? Type = null,
    bool IncludeViewed = false,
    int Skip = 0,
    int Take = 10) : IQuery<IEnumerable<CourseRecommendation>>;

/// <summary>
/// Get a specific recommendation by ID
/// </summary>
public record GetRecommendationByIdQuery(Guid Id, Guid UserId) : IQuery<CourseRecommendation?>;

/// <summary>
/// Get recommendation statistics for a user
/// </summary>
public record GetRecommendationStatisticsQuery(Guid UserId) : IQuery<RecommendationStatisticsDto>;

/// <summary>
/// Check if user has any pending recommendations
/// </summary>
public record HasPendingRecommendationsQuery(Guid UserId) : IQuery<bool>;

// ===== USER LEARNING PROFILE QUERIES =====

/// <summary>
/// Get user's learning profile
/// </summary>
public record GetUserLearningProfileQuery(Guid UserId) : IQuery<UserLearningProfile?>;

/// <summary>
/// Get or create user's learning profile
/// </summary>
public record GetOrCreateUserLearningProfileQuery(Guid UserId) : IQuery<UserLearningProfile>;

// ===== POPULAR/TRENDING QUERIES =====

/// <summary>
/// Get popular courses across the platform
/// </summary>
public record GetPopularCoursesQuery(
    Guid? TenantId = null,
    string? Category = null,
    int Skip = 0,
    int Take = 10) : IQuery<IEnumerable<PopularCourseDto>>;

/// <summary>
/// Get trending courses (high recent enrollment velocity)
/// </summary>
public record GetTrendingCoursesQuery(
    Guid? TenantId = null,
    int DaysWindow = 7,
    int Skip = 0,
    int Take = 10) : IQuery<IEnumerable<TrendingCourseDto>>;

/// <summary>
/// Get courses similar to a specific course
/// </summary>
public record GetSimilarCoursesQuery(
    Guid CourseId,
    Guid? TenantId = null,
    int MaxResults = 5) : IQuery<IEnumerable<SimilarCourseDto>>;

/// <summary>
/// Get users who might benefit from a specific course (for admin)
/// </summary>
public record GetPotentialLearnersQuery(
    Guid CourseId,
    Guid? TenantId = null,
    int MaxResults = 20) : IQuery<IEnumerable<Guid>>;
