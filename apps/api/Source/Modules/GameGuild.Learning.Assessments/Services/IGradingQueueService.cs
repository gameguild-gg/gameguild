namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service interface for the instructor grading queue (SpeedGrader navigation bundle).
/// </summary>
public interface IGradingQueueService
{
    /// <summary>
    /// Builds the grading queue for an assessment: one item per student (individual) or group ×
    /// attempt, excluding attempts whose only rows are InProgress.
    /// </summary>
    Task<Result<GradingQueueDto>> GetQueueAsync(Guid assessmentId);
}

/// <summary>
/// SpeedGrader navigation bundle: assessment summary plus one queue item per student/group
/// attempt. No peer-review data here — SpeedGrader fetches reviews per submission.
/// </summary>
public sealed record GradingQueueDto(
    GradingQueueAssessmentDto Assessment,
    IReadOnlyList<GradingQueueItemDto> Items,
    int Total,
    int NeedsGrading);

/// <summary>
/// Assessment summary fields the SpeedGrader header and grading panel need.
/// </summary>
public sealed record GradingQueueAssessmentDto(
    Guid Id,
    string Title,
    AssessmentType Type,
    int MaxScore,
    string GradingMethods,
    Guid? GroupSetId,
    int PeerReviewsRequiredCount,
    bool HasRubric,
    RubricDto? Rubric);

/// <summary>
/// One navigable queue entry. SubmissionId is the row the grader opens: the single row for
/// individuals, the canonical Min(Id) row for group attempts (CanonicalSubmissionId mirrors it
/// so clients can address group items by their canonical id).
/// </summary>
public sealed record GradingQueueItemDto(
    Guid SubmissionId,
    Guid CanonicalSubmissionId,
    int AttemptNumber,
    SubmissionStatus Status,
    int? Score,
    bool IsLate,
    DateTime? SubmittedAt,
    bool IsGroup,
    Guid? UserId = null,
    string? DisplayName = null,
    Guid? GroupId = null,
    string? GroupName = null,
    IReadOnlyList<string>? MemberNames = null);
