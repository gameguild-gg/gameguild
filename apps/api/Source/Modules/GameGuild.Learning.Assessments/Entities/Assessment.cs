using GameGuild.Entities;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Represents an assessment (quiz, exam, assignment) within a course
/// </summary>
public class Assessment : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid? ContentId { get; private set; } // Optional: linked to specific content
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AssessmentType Type { get; private set; }
    public int MaxScore { get; private set; }
    public int PassingScore { get; private set; }
    public int? TimeLimitMinutes { get; private set; }
    public int? MaxAttempts { get; private set; }
    public bool IsRequired { get; private set; }
    public int Order { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableUntil { get; private set; }

    private Assessment() { } // EF Core

    public static Assessment Create(
        Guid courseId,
        string title,
        AssessmentType type,
        int maxScore,
        int passingScore,
        bool isRequired = true)
    {
        return new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = title,
            Type = type,
            MaxScore = maxScore,
            PassingScore = passingScore,
            IsRequired = isRequired,
            Order = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool IsAvailable()
    {
        var now = DateTime.UtcNow;
        if (AvailableFrom.HasValue && now < AvailableFrom.Value) return false;
        if (AvailableUntil.HasValue && now > AvailableUntil.Value) return false;
        return true;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTimeLimit(int? timeLimitMinutes)
    {
        TimeLimitMinutes = timeLimitMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaxAttempts(int? maxAttempts)
    {
        MaxAttempts = maxAttempts;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvailability(DateTime? availableFrom, DateTime? availableUntil)
    {
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? title,
        string? description,
        int? maxScore,
        int? passingScore,
        int? timeLimitMinutes,
        int? maxAttempts,
        bool? isRequired,
        DateTime? availableFrom,
        DateTime? availableUntil)
    {
        if (title != null) Title = title;
        Description = description;
        if (maxScore.HasValue) MaxScore = maxScore.Value;
        if (passingScore.HasValue) PassingScore = passingScore.Value;
        TimeLimitMinutes = timeLimitMinutes;
        MaxAttempts = maxAttempts;
        if (isRequired.HasValue) IsRequired = isRequired.Value;
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a student's submission/attempt at an assessment
/// </summary>
public class AssessmentSubmission : EntityBase
{
    public Guid AssessmentId { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public Guid UserId { get; private set; }
    public int AttemptNumber { get; private set; }
    public int? Score { get; private set; }
    public bool? Passed { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? GradedAt { get; private set; }
    public Guid? GradedBy { get; private set; }
    public string? Feedback { get; private set; }
    public SubmissionStatus Status { get; private set; }

    private AssessmentSubmission() { } // EF Core

    public static AssessmentSubmission Start(Guid assessmentId, Guid enrollmentId, Guid userId, int attemptNumber)
    {
        return new AssessmentSubmission
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            EnrollmentId = enrollmentId,
            UserId = userId,
            AttemptNumber = attemptNumber,
            StartedAt = DateTime.UtcNow,
            Status = SubmissionStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Submit()
    {
        SubmittedAt = DateTime.UtcNow;
        Status = SubmissionStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Grade(int score, int passingScore, Guid? gradedBy = null, string? feedback = null)
    {
        Score = score;
        Passed = score >= passingScore;
        GradedAt = DateTime.UtcNow;
        GradedBy = gradedBy;
        Feedback = feedback;
        Status = SubmissionStatus.Graded;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AssessmentType
{
    Quiz,
    Exam,
    Assignment,
    Project,
    PeerReview,
    SelfAssessment
}

public enum SubmissionStatus
{
    InProgress,
    Submitted,
    Graded,
    Returned,
    Late
}
