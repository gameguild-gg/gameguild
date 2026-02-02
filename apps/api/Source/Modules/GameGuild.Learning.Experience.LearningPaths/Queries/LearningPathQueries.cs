using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.LearningPaths;

// ===== LEARNING PATH QUERIES =====

/// <summary>
/// Query to get all published learning paths
/// </summary>
public record GetPublishedPathsQuery(
    Guid? TenantId = null,
    LearningPathDifficulty? Difficulty = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get a learning path by slug
/// </summary>
public record GetPathBySlugQuery(string Slug, Guid? TenantId = null) : IQuery<LearningPath?>;

/// <summary>
/// Query to get a learning path by ID
/// </summary>
public record GetPathByIdQuery(Guid Id, bool IncludeCourses = false) : IQuery<LearningPath?>;

/// <summary>
/// Query to get featured learning paths
/// </summary>
public record GetFeaturedPathsQuery(
    Guid? TenantId = null,
    int Take = 10
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get learning paths by creator
/// </summary>
public record GetPathsByCreatorQuery(
    Guid CreatorId,
    bool IncludeUnpublished = false,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get all learning paths (admin view)
/// </summary>
public record GetAllPathsQuery(
    Guid? TenantId = null,
    bool IncludeUnpublished = true,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to search learning paths
/// </summary>
public record SearchPathsQuery(
    string SearchTerm,
    Guid? TenantId = null,
    LearningPathDifficulty? Difficulty = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

// ===== ENROLLMENT QUERIES =====

/// <summary>
/// Query to get paths user is enrolled in
/// </summary>
public record GetUserEnrolledPathsQuery(
    Guid UserId,
    LearningPathEnrollmentStatus? Status = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPathEnrollment>>;

/// <summary>
/// Query to get user's enrollment in a specific path
/// </summary>
public record GetUserPathEnrollmentQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<LearningPathEnrollment?>;

/// <summary>
/// Query to check if user is enrolled in a path
/// </summary>
public record CheckPathEnrollmentQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<bool>;

/// <summary>
/// Query to get enrollments for a learning path (admin)
/// </summary>
public record GetPathEnrollmentsQuery(
    Guid LearningPathId,
    LearningPathEnrollmentStatus? Status = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPathEnrollment>>;

/// <summary>
/// Query to get user's path progress
/// </summary>
public record GetUserPathProgressQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<LearningPathEnrollmentDto?>;

// ===== STATISTICS QUERIES =====

/// <summary>
/// Query to get learning path statistics
/// </summary>
public record GetPathStatisticsQuery(Guid LearningPathId) : IQuery<LearningPathStatisticsDto?>;

/// <summary>
/// Query to get popular learning paths
/// </summary>
public record GetPopularPathsQuery(
    Guid? TenantId = null,
    int DaysBack = 30,
    int Take = 10
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get recently completed learning paths for a user
/// </summary>
public record GetUserCompletedPathsQuery(
    Guid UserId,
    int Skip = 0,
    int Take = 20
) : IQuery<IEnumerable<LearningPathEnrollment>>;
