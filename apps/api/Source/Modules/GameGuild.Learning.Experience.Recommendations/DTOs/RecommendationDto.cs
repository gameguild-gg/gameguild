namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// DTO for a course recommendation
/// </summary>
public record RecommendationDto(
    Guid Id,
    Guid UserId,
    Guid CourseId,
    RecommendationType Type,
    double Score,
    string? Reason,
    bool IsViewed,
    bool IsDismissed,
    DateTime ExpiresAt,
    DateTime CreatedAt);

/// <summary>
/// DTO for a recommendation with course details
/// </summary>
public record RecommendationDetailDto(
    Guid Id,
    Guid UserId,
    Guid CourseId,
    string CourseTitle,
    string? CourseDescription,
    string? CourseThumbnail,
    string? CourseCategory,
    string? CourseDifficulty,
    int? EstimatedHours,
    RecommendationType Type,
    double Score,
    string? Reason,
    bool IsViewed,
    DateTime ExpiresAt,
    DateTime CreatedAt);

/// <summary>
/// DTO for user learning profile
/// </summary>
public record UserLearningProfileDto(
    Guid Id,
    Guid UserId,
    string[]? PreferredCategories,
    string? PreferredDifficulty,
    string? PreferredDuration,
    string[]? LearningGoals,
    string[]? Skills,
    int TotalCoursesCompleted,
    int TotalHoursLearned,
    DateTime? LastActivityAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for creating/updating user learning profile
/// </summary>
public record CreateOrUpdateLearningProfileDto(
    string[]? PreferredCategories,
    string? PreferredDifficulty,
    string? PreferredDuration,
    string[]? LearningGoals,
    string[]? Skills);

/// <summary>
/// DTO for creating a recommendation (internal use)
/// </summary>
public record CreateRecommendationDto(
    Guid UserId,
    Guid CourseId,
    RecommendationType Type,
    double Score,
    string? Reason,
    TimeSpan? ValidFor);

/// <summary>
/// DTO for recommendation statistics
/// </summary>
public record RecommendationStatisticsDto(
    int TotalRecommendations,
    int ViewedCount,
    int DismissedCount,
    int ConvertedCount,
    Dictionary<RecommendationType, int> ByType);

/// <summary>
/// DTO for popular course result
/// </summary>
public record PopularCourseDto(
    Guid CourseId,
    string Title,
    string? Description,
    string? Thumbnail,
    string? Category,
    int EnrollmentCount,
    decimal AverageRating,
    int TotalRatings);

/// <summary>
/// DTO for trending course result
/// </summary>
public record TrendingCourseDto(
    Guid CourseId,
    string Title,
    string? Description,
    string? Thumbnail,
    string? Category,
    int RecentEnrollments,
    decimal TrendScore);

/// <summary>
/// DTO for similar course result
/// </summary>
public record SimilarCourseDto(
    Guid CourseId,
    string Title,
    string? Description,
    string? Thumbnail,
    string? Category,
    double SimilarityScore,
    string[] MatchingTags);
