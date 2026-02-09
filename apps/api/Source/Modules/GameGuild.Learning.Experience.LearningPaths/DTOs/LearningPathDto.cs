namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// DTO for learning path summary
/// </summary>
public sealed record LearningPathDto(
    Guid Id,
    Guid? TenantId,
    Guid CreatorId,
    string Title,
    string Slug,
    string? Description,
    string? ImageUrl,
    int EstimatedHours,
    LearningPathDifficulty Difficulty,
    bool IsPublished,
    bool IsFeatured,
    int EnrollmentCount,
    int CompletionCount,
    int CourseCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for learning path with courses
/// </summary>
public sealed record LearningPathDetailDto(
    Guid Id,
    Guid? TenantId,
    Guid CreatorId,
    string Title,
    string Slug,
    string? Description,
    string? ImageUrl,
    int EstimatedHours,
    LearningPathDifficulty Difficulty,
    bool IsPublished,
    bool IsFeatured,
    int EnrollmentCount,
    int CompletionCount,
    IEnumerable<LearningPathCourseDto> Courses,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for a course in a learning path
/// </summary>
public sealed record LearningPathCourseDto(
    Guid CourseId,
    int Order,
    bool IsRequired);

/// <summary>
/// DTO for creating a learning path
/// </summary>
public sealed record CreateLearningPathDto(
    string Title,
    LearningPathDifficulty Difficulty = LearningPathDifficulty.Beginner,
    string? Description = null,
    string? ImageUrl = null,
    int EstimatedHours = 0);

/// <summary>
/// DTO for updating a learning path
/// </summary>
public sealed record UpdateLearningPathDto(
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    int? EstimatedHours = null,
    LearningPathDifficulty? Difficulty = null,
    bool? IsFeatured = null);

/// <summary>
/// DTO for adding a course to a learning path
/// </summary>
public sealed record AddCourseToPathDto(
    Guid CourseId,
    int Order,
    bool IsRequired = true);

/// <summary>
/// DTO for reordering courses in a learning path
/// </summary>
public sealed record ReorderCoursesDto(
    IEnumerable<CourseOrderDto> Courses);

/// <summary>
/// DTO for course order
/// </summary>
public sealed record CourseOrderDto(
    Guid CourseId,
    int Order);

/// <summary>
/// DTO for learning path enrollment
/// </summary>
public sealed record LearningPathEnrollmentDto(
    Guid Id,
    Guid LearningPathId,
    Guid UserId,
    int Progress,
    int CoursesCompleted,
    int TotalCourses,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    LearningPathEnrollmentStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for enrolling in a learning path
/// </summary>
public sealed record EnrollInPathDto(
    Guid LearningPathId);

/// <summary>
/// DTO for updating enrollment progress
/// </summary>
public sealed record UpdatePathProgressDto(
    int CoursesCompleted);

/// <summary>
/// DTO for learning path statistics
/// </summary>
public sealed record LearningPathStatisticsDto(
    Guid LearningPathId,
    int TotalEnrollments,
    int ActiveEnrollments,
    int CompletedEnrollments,
    double CompletionRate,
    double AverageProgress,
    TimeSpan AverageCompletionTime);
