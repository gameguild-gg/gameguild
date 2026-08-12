using System.Text.Json.Serialization;

namespace GameGuild.Learning.Workspaces;

public sealed record LearnerDashboardDto(
    IReadOnlyList<LearnerCourseSummaryDto> Courses,
    IReadOnlyList<LearnerScheduleEntryDto> Upcoming,
    IReadOnlyList<LearnerAssessmentDeadlineDto> Deadlines,
    IReadOnlyList<LearnerGradeSummaryDto> Grades,
    IReadOnlyList<LearnerCertificateDto> Certificates,
    IReadOnlyList<LearnerAnnouncementDto> Announcements);

public sealed record LearnerCourseSummaryDto(
    Guid CourseId,
    Guid EnrollmentId,
    string Title,
    string Slug,
    string Description,
    string? Thumbnail,
    string Category,
    string Difficulty,
    int? EstimatedHours,
    string EnrollmentStatus,
    string CompletionStatus,
    decimal ProgressPercentage,
    decimal? FinalGrade,
    DateTime EnrolledAt,
    int TotalItems,
    int CompletedItems,
    int RemainingMinutes,
    Guid? CurrentContentId,
    string? CurrentContentTitle,
    string? CurrentContentType);

public sealed record LearnerScheduleEntryDto(
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    Guid CohortId,
    string CohortName,
    Guid ScheduleItemId,
    Guid? ContentId,
    Guid? AssessmentId,
    string Type,
    string Title,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    string? Location,
    string? MeetingUrl,
    string Status);

public sealed record LearnerAssessmentDeadlineDto(
    Guid AssessmentId,
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    Guid? ContentId,
    Guid? GroupId,
    string Title,
    string Type,
    int MaxScore,
    int PassingScore,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    string SubmissionStatus);

public sealed record LearnerGradeSummaryDto(
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    decimal? FinalGrade,
    int GradedAssessments,
    int TotalAssessments,
    decimal? EarnedPoints,
    decimal? PossiblePoints,
    decimal? Percentage,
    IReadOnlyList<LearnerAssessmentGroupDto> Groups,
    IReadOnlyList<LearnerGradeItemDto> Items);

public sealed record LearnerGradeItemDto(
    Guid AssessmentId,
    Guid? ContentId,
    Guid? GroupId,
    string Title,
    string Type,
    int MaxScore,
    int PassingScore,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    string SubmissionStatus,
    int? Score,
    bool? Passed,
    string? Feedback,
    DateTime? GradedAt);

public sealed record LearnerCertificateDto(
    Guid CertificateId,
    Guid EnrollmentId,
    Guid CourseId,
    string CourseName,
    string CertificateNumber,
    string RecipientName,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    string? VerificationUrl,
    string Status);

public sealed record LearnerAnnouncementDto(
    Guid DiscussionId,
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    string Title,
    string Content,
    DateTime CreatedAt,
    DateTime? LastActivityAt);

public sealed record LearnerCourseWorkspaceDto(
    LearnerCourseSummaryDto Course,
    IReadOnlyList<LearnerContentDto> Content,
    IReadOnlyList<LearnerContentProgressDto> Progress,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] LearnerCohortDto? Cohort,
    IReadOnlyList<LearnerScheduleEntryDto> Calendar,
    IReadOnlyList<LearnerAssessmentGroupDto> AssessmentGroups,
    IReadOnlyList<LearnerAssessmentDto> Assessments,
    IReadOnlyList<LearnerAssessmentSubmissionDto> Submissions,
    IReadOnlyList<LearnerDiscussionDto> Discussions,
    IReadOnlyList<LearnerCertificateDto> Certificates);

public sealed record LearnerContentDto(
    Guid ContentId,
    Guid? ParentId,
    string Title,
    string Description,
    string Type,
    string? Body,
    string? LessonFormat,
    string? ActivitySettings,
    int SortOrder,
    bool IsRequired,
    int? EstimatedMinutes,
    string Visibility);

public sealed record LearnerContentProgressDto(
    Guid ContentId,
    string Status,
    decimal ProgressPercentage,
    DateTime? FirstAccessedAt,
    DateTime? LastAccessedAt,
    DateTime? CompletedAt,
    int TimeSpentSeconds,
    decimal? Score,
    decimal? MaxScore,
    int Attempts);

public sealed record LearnerCohortDto(
    Guid CohortId,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxCapacity,
    int CurrentEnrollmentCount,
    string Status,
    Guid? InstructorId,
    string? MeetingSchedule);

public sealed record LearnerAssessmentGroupDto(
    Guid GroupId,
    string Name,
    string? Description,
    decimal WeightPercent,
    int Order);

public sealed record LearnerAssessmentDto(
    Guid AssessmentId,
    Guid? ContentId,
    Guid? GroupId,
    string Title,
    string? Description,
    string Type,
    int MaxScore,
    int PassingScore,
    int? TimeLimitMinutes,
    int? MaxAttempts,
    bool IsRequired,
    int Order,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    bool AllowLateSubmissions,
    DateTime? LateSubmissionDeadline,
    string SubmissionModalities,
    string PresentationMode);

public sealed record LearnerAssessmentSubmissionDto(
    Guid SubmissionId,
    Guid AssessmentId,
    Guid EnrollmentId,
    int AttemptNumber,
    int? Score,
    bool? Passed,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    string? Feedback,
    string Status,
    bool IsLate);

public sealed record LearnerDiscussionDto(
    Guid DiscussionId,
    Guid? ContentId,
    Guid AuthorId,
    string Title,
    string Content,
    bool IsPinned,
    bool IsResolved,
    int ReplyCount,
    int ViewCount,
    DateTime? LastActivityAt,
    DateTime CreatedAt);

public sealed record LearnerSearchResultDto(
    Guid Id,
    Guid CourseId,
    string CourseSlug,
    string Kind,
    string Title,
    string Description,
    string Route);
