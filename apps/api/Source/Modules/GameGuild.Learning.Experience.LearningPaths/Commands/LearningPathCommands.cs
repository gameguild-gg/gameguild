using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.LearningPaths;

// ===== LEARNING PATH CRUD COMMANDS =====

/// <summary>
/// Command to create a new learning path
/// </summary>
public record CreateLearningPathCommand(
    Guid CreatorId,
    string Title,
    LearningPathDifficulty Difficulty = LearningPathDifficulty.Beginner,
    Guid? TenantId = null,
    string? Description = null,
    string? ImageUrl = null,
    int EstimatedHours = 0
) : ICommand<LearningPath>;

/// <summary>
/// Command to update a learning path
/// </summary>
public record UpdateLearningPathCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    int? EstimatedHours = null,
    LearningPathDifficulty? Difficulty = null,
    bool? IsFeatured = null
) : ICommand<LearningPath?>;

/// <summary>
/// Command to delete a learning path
/// </summary>
public record DeleteLearningPathCommand(Guid Id) : ICommand<bool>;

// ===== LEARNING PATH LIFECYCLE COMMANDS =====

/// <summary>
/// Command to publish a learning path
/// </summary>
public record PublishLearningPathCommand(Guid Id) : ICommand<LearningPath?>;

/// <summary>
/// Command to unpublish a learning path
/// </summary>
public record UnpublishLearningPathCommand(Guid Id) : ICommand<LearningPath?>;

// ===== COURSE MANAGEMENT COMMANDS =====

/// <summary>
/// Command to add a course to a learning path
/// </summary>
public record AddCourseToPathCommand(
    Guid LearningPathId,
    Guid CourseId,
    int Order,
    bool IsRequired = true
) : ICommand<LearningPath?>;

/// <summary>
/// Command to remove a course from a learning path
/// </summary>
public record RemoveCourseFromPathCommand(
    Guid LearningPathId,
    Guid CourseId
) : ICommand<bool>;

/// <summary>
/// Command to reorder courses in a learning path
/// </summary>
public record ReorderPathCoursesCommand(
    Guid LearningPathId,
    IEnumerable<CourseOrderDto> Courses
) : ICommand<LearningPath?>;

// ===== ENROLLMENT COMMANDS =====

/// <summary>
/// Command to enroll a user in a learning path
/// </summary>
public record EnrollInPathCommand(
    Guid LearningPathId,
    Guid UserId
) : ICommand<LearningPathEnrollment>;

/// <summary>
/// Command to unenroll a user from a learning path
/// </summary>
public record UnenrollFromPathCommand(
    Guid LearningPathId,
    Guid UserId
) : ICommand<bool>;

/// <summary>
/// Command to update user's progress in a learning path
/// </summary>
public record UpdatePathProgressCommand(
    Guid LearningPathId,
    Guid UserId,
    int CoursesCompleted
) : ICommand<LearningPathEnrollment?>;

/// <summary>
/// Command to mark a learning path as completed
/// </summary>
public record CompletePathCommand(
    Guid LearningPathId,
    Guid UserId
) : ICommand<LearningPathEnrollment?>;

/// <summary>
/// Command to abandon a learning path enrollment
/// </summary>
public record AbandonPathCommand(
    Guid LearningPathId,
    Guid UserId
) : ICommand<bool>;
