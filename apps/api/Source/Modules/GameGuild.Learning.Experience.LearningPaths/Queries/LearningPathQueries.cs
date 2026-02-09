using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.LearningPaths;

// ===== LEARNING PATH QUERIES =====

/// <summary>
/// Query to get all published learning paths
/// </summary>
public sealed record GetPublishedPathsQuery(
    Guid? TenantId = null,
    LearningPathDifficulty? Difficulty = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get a learning path by slug
/// </summary>
public sealed record GetPathBySlugQuery(string Slug, Guid? TenantId = null) : IQuery<LearningPath?>;

/// <summary>
/// Query to get a learning path by ID
/// </summary>
public sealed record GetPathByIdQuery(Guid Id, bool IncludeCourses = false) : IQuery<LearningPath?>;

/// <summary>
/// Query to get featured learning paths
/// </summary>
public sealed record GetFeaturedPathsQuery(
    Guid? TenantId = null,
    int Take = 10
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get learning paths by creator
/// </summary>
public sealed record GetPathsByCreatorQuery(
    Guid CreatorId,
    bool IncludeUnpublished = false,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get all learning paths (admin view)
/// </summary>
public sealed record GetAllPathsQuery(
    Guid? TenantId = null,
    bool IncludeUnpublished = true,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to search learning paths
/// </summary>
public sealed record SearchPathsQuery(
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
public sealed record GetUserEnrolledPathsQuery(
    Guid UserId,
    LearningPathEnrollmentStatus? Status = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPathEnrollment>>;

/// <summary>
/// Query to get user's enrollment in a specific path
/// </summary>
public sealed record GetUserPathEnrollmentQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<LearningPathEnrollment?>;

/// <summary>
/// Query to check if user is enrolled in a path
/// </summary>
public sealed record CheckPathEnrollmentQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<bool>;

/// <summary>
/// Query to get enrollments for a learning path (admin)
/// </summary>
public sealed record GetPathEnrollmentsQuery(
    Guid LearningPathId,
    LearningPathEnrollmentStatus? Status = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<LearningPathEnrollment>>;

/// <summary>
/// Query to get user's path progress
/// </summary>
public sealed record GetUserPathProgressQuery(
    Guid UserId,
    Guid LearningPathId
) : IQuery<LearningPathEnrollmentDto?>;

// ===== STATISTICS QUERIES =====

/// <summary>
/// Query to get learning path statistics
/// </summary>
public sealed record GetPathStatisticsQuery(Guid LearningPathId) : IQuery<LearningPathStatisticsDto?>;

/// <summary>
/// Query to get popular learning paths
/// </summary>
public sealed record GetPopularPathsQuery(
    Guid? TenantId = null,
    int DaysBack = 30,
    int Take = 10
) : IQuery<IEnumerable<LearningPath>>;

/// <summary>
/// Query to get recently completed learning paths for a user
/// </summary>
public sealed record GetUserCompletedPathsQuery(
    Guid UserId,
    int Skip = 0,
    int Take = 20
) : IQuery<IEnumerable<LearningPathEnrollment>>;
