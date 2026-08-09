using GameGuild.Identity.Users;

namespace GameGuild.TestingLab;

[Table("testing_events")]
public sealed class TestingEvent : EntityBase
{
    [Required, MaxLength(255)]
    public string Name { get; private set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; private set; }

    public TestingEventMode Mode { get; private set; }

    public TestingEventApprovalMode ApprovalMode { get; private set; }

    public TestingEventStatus Status { get; private set; } = TestingEventStatus.Draft;

    public Guid ManagerUserId { get; private set; }

    public User Manager { get; private set; } = null!;

    public DateTime ApplicationsOpenAt { get; private set; }

    public DateTime ApplicationsCloseAt { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public Guid? RecurrenceSeriesId { get; private set; }

    public int? RecurrenceOccurrence { get; private set; }

    public TestingEventRecurrenceFrequency? RecurrenceFrequency { get; private set; }

    public int? RecurrenceInterval { get; private set; }

    [MaxLength(64)]
    public string? RecurrenceDaysOfWeek { get; private set; }

    public DateTime? RecurrenceEndsAt { get; private set; }

    public int? RecurrenceOccurrenceCount { get; private set; }

    public bool RequiresFeedback { get; private set; }

    public TestingLearningCompletionRequirement LearningCompletionRequirement { get; private set; }

    public Guid? CourseId { get; private set; }

    public Guid? CohortId { get; private set; }

    public Guid? LearningActivityId { get; private set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public ICollection<TestingEventSlot> Slots { get; private set; } = new List<TestingEventSlot>();

    public ICollection<TestingProjectApplication> Applications { get; private set; } = new List<TestingProjectApplication>();

    public ICollection<TestingCommitteeMember> CommitteeMembers { get; private set; } = new List<TestingCommitteeMember>();

    private TestingEvent()
    {
    }

    public static TestingEvent Create(
        string name,
        TestingEventMode mode,
        Guid managerUserId,
        DateTime applicationsOpenAt,
        DateTime applicationsCloseAt,
        DateTime startsAt,
        DateTime endsAt,
        bool requiresFeedback,
        TestingEventApprovalMode approvalMode,
        Guid? tenantId,
        string? description = null,
        Guid? recurrenceSeriesId = null,
        int? recurrenceOccurrence = null,
        TestingEventRecurrenceFrequency? recurrenceFrequency = null,
        int? recurrenceInterval = null,
        string? recurrenceDaysOfWeek = null,
        DateTime? recurrenceEndsAt = null,
        int? recurrenceOccurrenceCount = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Event name is required.", nameof(name));
        if (managerUserId == Guid.Empty) throw new ArgumentException("Manager is required.", nameof(managerUserId));
        if (applicationsCloseAt <= applicationsOpenAt) throw new ArgumentException("Application window must end after it opens.");
        if (startsAt < applicationsCloseAt) throw new ArgumentException("Event must start after applications close.");
        if (endsAt <= startsAt) throw new ArgumentException("Event must end after it starts.");

        return new TestingEvent
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Mode = mode,
            ManagerUserId = managerUserId,
            ApplicationsOpenAt = applicationsOpenAt,
            ApplicationsCloseAt = applicationsCloseAt,
            StartsAt = startsAt,
            EndsAt = endsAt,
            RequiresFeedback = requiresFeedback,
            ApprovalMode = approvalMode,
            TenantId = tenantId,
            RecurrenceSeriesId = recurrenceSeriesId,
            RecurrenceOccurrence = recurrenceOccurrence,
            RecurrenceFrequency = recurrenceFrequency,
            RecurrenceInterval = recurrenceInterval,
            RecurrenceDaysOfWeek = recurrenceDaysOfWeek,
            RecurrenceEndsAt = recurrenceEndsAt,
            RecurrenceOccurrenceCount = recurrenceOccurrenceCount,
        };
    }

    public void Update(
        string name,
        string? description,
        TestingEventMode mode,
        TestingEventApprovalMode approvalMode,
        DateTime applicationsOpenAt,
        DateTime applicationsCloseAt,
        DateTime startsAt,
        DateTime endsAt,
        bool requiresFeedback)
    {
        if (Status is TestingEventStatus.Active or TestingEventStatus.Completed or TestingEventStatus.Cancelled)
            throw new InvalidOperationException("Active or terminal events cannot be edited.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Event name is required.", nameof(name));
        if (applicationsCloseAt <= applicationsOpenAt) throw new ArgumentException("Application window must end after it opens.");
        if (startsAt < applicationsCloseAt) throw new ArgumentException("Event must start after applications close.");
        if (endsAt <= startsAt) throw new ArgumentException("Event must end after it starts.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Mode = mode;
        ApprovalMode = approvalMode;
        ApplicationsOpenAt = applicationsOpenAt;
        ApplicationsCloseAt = applicationsCloseAt;
        StartsAt = startsAt;
        EndsAt = endsAt;
        RequiresFeedback = requiresFeedback;
        Touch();
    }
    public void OpenApplications()
    {
        if (Status != TestingEventStatus.Draft) throw new InvalidOperationException("Only draft events can open applications.");
        Status = TestingEventStatus.ApplicationsOpen;
        Touch();
    }

    public void CloseApplications()
    {
        if (Status != TestingEventStatus.ApplicationsOpen) throw new InvalidOperationException("Applications are not open.");
        Status = TestingEventStatus.ApplicationsClosed;
        Touch();
    }

    public void Schedule()
    {
        if (Status != TestingEventStatus.ApplicationsClosed)
            throw new InvalidOperationException("Only events with closed applications can be scheduled.");
        Status = TestingEventStatus.Scheduled;
        Touch();
    }

    public void Activate()
    {
        if (Status != TestingEventStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled events can become active.");
        Status = TestingEventStatus.Active;
        Touch();
    }

    public void Complete()
    {
        if (Status != TestingEventStatus.Active)
            throw new InvalidOperationException("Only active events can be completed.");
        Status = TestingEventStatus.Completed;
        Touch();
    }

    public void Cancel(string reason)
    {
        if (Status is TestingEventStatus.Completed or TestingEventStatus.Cancelled)
            throw new InvalidOperationException("Completed or cancelled events cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        Status = TestingEventStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = SystemClock.UtcNow;
        Touch();
    }

    public void ConfigureLearning(
        Guid courseId,
        Guid? cohortId,
        Guid learningActivityId,
        TestingLearningCompletionRequirement requirement)
    {
        if (courseId == Guid.Empty || learningActivityId == Guid.Empty)
            throw new ArgumentException("Course and learning activity are required.");
        if (Status is TestingEventStatus.Active or TestingEventStatus.Completed or TestingEventStatus.Cancelled)
            throw new InvalidOperationException("Active or terminal events cannot change their Learning configuration.");
        const TestingLearningCompletionRequirement supported =
            TestingLearningCompletionRequirement.Attendance |
            TestingLearningCompletionRequirement.FeedbackSubmitted |
            TestingLearningCompletionRequirement.ProjectPresented;
        if (requirement == TestingLearningCompletionRequirement.None ||
            (requirement & ~supported) != TestingLearningCompletionRequirement.None)
            throw new ArgumentOutOfRangeException(
                nameof(requirement), requirement, "At least one supported Learning requirement is required.");
        CourseId = courseId;
        CohortId = cohortId;
        LearningActivityId = learningActivityId;
        LearningCompletionRequirement = requirement;
        Touch();
    }
}
