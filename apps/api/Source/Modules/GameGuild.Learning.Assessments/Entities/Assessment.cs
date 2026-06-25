
namespace GameGuild.Learning.Assessments;

/// <summary>
/// Represents an assessment (quiz, assignment, project, or peer/self review) within a course.
/// </summary>
public class Assessment : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid? ContentId { get; private set; } // Optional: linked to specific content
    public Guid? AssessmentGroupId { get; private set; }
    public AssessmentGroup? AssessmentGroup { get; private set; }
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
        bool isRequired = true,
        Guid? assessmentGroupId = null)
    {
        return new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            AssessmentGroupId = assessmentGroupId,
            Title = title,
            Type = NormalizeType(type),
            MaxScore = maxScore,
            PassingScore = passingScore,
            IsRequired = isRequired,
            Order = 0
        };
    }

    public bool IsAvailable()
    {
        var now = SystemClock.UtcNow;
        if (AvailableFrom.HasValue && now < AvailableFrom.Value) return false;
        if (AvailableUntil.HasValue && now > AvailableUntil.Value) return false;
        return true;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void SetTimeLimit(int? timeLimitMinutes)
    {
        TimeLimitMinutes = timeLimitMinutes;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void SetMaxAttempts(int? maxAttempts)
    {
        MaxAttempts = maxAttempts;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void SetAvailability(DateTime? availableFrom, DateTime? availableUntil)
    {
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void AssignToGroup(Guid? assessmentGroupId)
    {
        AssessmentGroupId = assessmentGroupId;
        UpdatedAt = SystemClock.UtcNow;
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
        DateTime? availableUntil,
        Guid? contentId = null,
        bool clearContentId = false,
        Guid? assessmentGroupId = null,
        bool clearAssessmentGroupId = false)
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
        if (clearContentId) ContentId = null;
        else if (contentId.HasValue) ContentId = contentId.Value;
        if (clearAssessmentGroupId) AssessmentGroupId = null;
        else if (assessmentGroupId.HasValue) AssessmentGroupId = assessmentGroupId.Value;
        UpdatedAt = SystemClock.UtcNow;
    }

    public static AssessmentType NormalizeType(AssessmentType type)
    {
        return type == AssessmentType.Exam ? AssessmentType.Quiz : type;
    }
}

/// <summary>
/// Groups graded activities into weighted gradebook buckets for a course.
/// </summary>
public class AssessmentGroup : EntityBase
{
    public Guid CourseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal WeightPercent { get; private set; }
    public int Order { get; private set; }

    private AssessmentGroup() { } // EF Core

    public static AssessmentGroup Create(
        Guid courseId,
        string name,
        decimal weightPercent,
        int order = 0,
        string? description = null)
    {
        ValidateName(name);
        ValidateWeight(weightPercent);

        return new AssessmentGroup
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Name = name.Trim(),
            Description = NormalizeDescription(description),
            WeightPercent = weightPercent,
            Order = order
        };
    }

    public void Update(string? name, string? description, decimal? weightPercent, int? order)
    {
        if (name != null)
        {
            ValidateName(name);
            Name = name.Trim();
        }

        if (description != null)
        {
            Description = NormalizeDescription(description);
        }

        if (weightPercent.HasValue)
        {
            ValidateWeight(weightPercent.Value);
            WeightPercent = weightPercent.Value;
        }

        if (order.HasValue)
        {
            Order = order.Value;
        }

        UpdatedAt = SystemClock.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Assessment group name is required.", nameof(name));
        }
    }

    private static void ValidateWeight(decimal weightPercent)
    {
        if (weightPercent < 0 || weightPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(weightPercent), "Weight percent must be between 0 and 100.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
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
            StartedAt = SystemClock.UtcNow,
            Status = SubmissionStatus.InProgress
        };
    }

    public void Submit()
    {
        SubmittedAt = SystemClock.UtcNow;
        Status = SubmissionStatus.Submitted;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Grade(int score, int passingScore, Guid? gradedBy = null, string? feedback = null)
    {
        Score = score;
        Passed = score >= passingScore;
        GradedAt = SystemClock.UtcNow;
        GradedBy = gradedBy;
        Feedback = feedback;
        Status = SubmissionStatus.Graded;
        UpdatedAt = SystemClock.UtcNow;
    }
}

public enum AssessmentType
{
    Quiz = 0,
    // Legacy persisted slot. Public professor UI normalizes this to Quiz.
    Exam = 1,
    Assignment = 2,
    Project = 3,
    PeerReview = 4,
    SelfAssessment = 5
}

public enum SubmissionStatus
{
    InProgress,
    Submitted,
    Graded,
    Returned,
    Late
}
