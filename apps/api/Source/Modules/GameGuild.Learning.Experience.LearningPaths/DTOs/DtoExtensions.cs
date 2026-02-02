namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Extension methods for converting entities to DTOs
/// </summary>
public static class LearningPathDtoExtensions
{
    /// <summary>
    /// Convert LearningPath entity to summary DTO
    /// </summary>
    public static LearningPathDto ToDto(this LearningPath entity) =>
        new(
            Id: entity.Id,
            TenantId: entity.TenantId,
            CreatorId: entity.CreatorId,
            Title: entity.Title,
            Slug: entity.Slug,
            Description: entity.Description,
            ImageUrl: entity.ImageUrl,
            EstimatedHours: entity.EstimatedHours,
            Difficulty: entity.Difficulty,
            IsPublished: entity.IsPublished,
            IsFeatured: entity.IsFeatured,
            EnrollmentCount: entity.EnrollmentCount,
            CompletionCount: entity.CompletionCount,
            CourseCount: entity.Courses.Count,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);

    /// <summary>
    /// Convert LearningPath entity to detail DTO with courses
    /// </summary>
    public static LearningPathDetailDto ToDetailDto(this LearningPath entity) =>
        new(
            Id: entity.Id,
            TenantId: entity.TenantId,
            CreatorId: entity.CreatorId,
            Title: entity.Title,
            Slug: entity.Slug,
            Description: entity.Description,
            ImageUrl: entity.ImageUrl,
            EstimatedHours: entity.EstimatedHours,
            Difficulty: entity.Difficulty,
            IsPublished: entity.IsPublished,
            IsFeatured: entity.IsFeatured,
            EnrollmentCount: entity.EnrollmentCount,
            CompletionCount: entity.CompletionCount,
            Courses: entity.Courses.OrderBy(c => c.Order).Select(c => c.ToDto()),
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);

    /// <summary>
    /// Convert LearningPathCourse entity to DTO
    /// </summary>
    public static LearningPathCourseDto ToDto(this LearningPathCourse entity) =>
        new(
            CourseId: entity.CourseId,
            Order: entity.Order,
            IsRequired: entity.IsRequired);

    /// <summary>
    /// Convert LearningPathEnrollment entity to DTO
    /// </summary>
    public static LearningPathEnrollmentDto ToDto(this LearningPathEnrollment entity) =>
        new(
            Id: entity.Id,
            LearningPathId: entity.LearningPathId,
            UserId: entity.UserId,
            Progress: entity.Progress,
            CoursesCompleted: entity.CoursesCompleted,
            TotalCourses: entity.TotalCourses,
            EnrolledAt: entity.EnrolledAt,
            CompletedAt: entity.CompletedAt,
            Status: entity.Status,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
}
