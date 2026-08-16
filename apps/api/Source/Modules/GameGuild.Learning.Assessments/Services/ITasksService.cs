namespace GameGuild.Learning.Assessments;

/// <summary>
/// Actor-scoped cross-course task aggregation (what to grade / do / review next).
/// </summary>
public interface ITasksService
{
    /// <summary>
    /// Aggregated tasks for one actor. Non-enrolled non-manager actors get empty lists, not errors.
    /// </summary>
    Task<TasksDto> GetTasksAsync(Guid actorUserId, Guid? tenantId, bool isSystemAdmin);
}

/// <summary>
/// One task item. Variant fields are null outside their type (GradingQueueItemDto precedent).
/// </summary>
public sealed record TaskItemDto(
    string Type,
    Guid CourseId,
    string CourseTitle,
    Guid AssessmentId,
    string AssessmentTitle,
    DateTime? DueAt,
    int? CountSubmitted = null,
    int? ReviewsCompleted = null,
    int? ReviewsRequired = null);

public sealed record TasksDto(IReadOnlyList<TaskItemDto> Items);
