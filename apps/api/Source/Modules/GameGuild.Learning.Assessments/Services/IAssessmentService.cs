
namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service interface for assessment management and submission processing
/// </summary>
public interface IAssessmentService
{
    // ===== ASSESSMENT MANAGEMENT =====

    /// <summary>
    /// Creates a new assessment for a course
    /// </summary>
    Task<Result<Assessment>> CreateAssessmentAsync(CreateAssessmentRequest request);

    /// <summary>
    /// Gets an assessment by ID
    /// </summary>
    Task<Assessment?> GetAssessmentByIdAsync(Guid id);

    /// <summary>
    /// Gets all assessments for a course
    /// </summary>
    Task<IEnumerable<Assessment>> GetCourseAssessmentsAsync(Guid courseId);

    /// <summary>
    /// Updates an existing assessment
    /// </summary>
    Task<Result<Assessment>> UpdateAssessmentAsync(Guid id, UpdateAssessmentRequest request);

    /// <summary>
    /// Deletes an assessment
    /// </summary>
    Task<Result> DeleteAssessmentAsync(Guid id);

    // ===== SUBMISSION MANAGEMENT =====

    /// <summary>
    /// Starts a new assessment submission attempt
    /// </summary>
    Task<Result<AssessmentSubmission>> StartSubmissionAsync(Guid assessmentId, Guid enrollmentId, Guid userId);

    /// <summary>
    /// Submits a completed assessment
    /// </summary>
    Task<Result<AssessmentSubmission>> SubmitAsync(Guid submissionId);

    /// <summary>
    /// Grades a submission
    /// </summary>
    Task<Result<AssessmentSubmission>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request);

    /// <summary>
    /// Gets a submission by ID
    /// </summary>
    Task<AssessmentSubmission?> GetSubmissionByIdAsync(Guid id);

    /// <summary>
    /// Gets all submissions for an assessment
    /// </summary>
    Task<IEnumerable<AssessmentSubmission>> GetAssessmentSubmissionsAsync(Guid assessmentId);

    /// <summary>
    /// Gets all submissions for a user enrollment
    /// </summary>
    Task<IEnumerable<AssessmentSubmission>> GetUserSubmissionsAsync(Guid enrollmentId);

    /// <summary>
    /// Gets the number of attempts a user has made for an assessment
    /// </summary>
    Task<int> GetAttemptCountAsync(Guid assessmentId, Guid enrollmentId);

    /// <summary>
    /// Checks if a user can attempt an assessment
    /// </summary>
    Task<Result<bool>> CanAttemptAsync(Guid assessmentId, Guid enrollmentId);
}

/// <summary>
/// Request to create a new assessment
/// </summary>
public sealed record CreateAssessmentRequest(
    Guid CourseId,
    string Title,
    string? Description,
    AssessmentType Type,
    int MaxScore,
    int PassingScore,
    int? TimeLimitMinutes = null,
    int? MaxAttempts = null,
    bool IsRequired = true,
    DateTime? AvailableFrom = null,
    DateTime? AvailableUntil = null
);

/// <summary>
/// Request to update an assessment
/// </summary>
public sealed record UpdateAssessmentRequest(
    string? Title = null,
    string? Description = null,
    int? MaxScore = null,
    int? PassingScore = null,
    int? TimeLimitMinutes = null,
    int? MaxAttempts = null,
    bool? IsRequired = null,
    DateTime? AvailableFrom = null,
    DateTime? AvailableUntil = null,
    Guid? ContentId = null,
    bool ClearContentId = false
);

/// <summary>
/// Request to grade a submission
/// </summary>
public sealed record GradeSubmissionRequest(
    int Score,
    Guid? GradedBy = null,
    string? Feedback = null
);
