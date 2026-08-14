using GameGuild.Identity.Users;
using GameGuild.Projects;
using System.Text.Json;

namespace GameGuild.TestingLab;

[Table("testing_project_applications")]
public sealed class TestingProjectApplication : EntityBase
{
    public Guid EventId { get; private set; }

    public TestingEvent Event { get; private set; } = null!;

    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public Guid? ProjectVersionId { get; private set; }

    public ProjectVersion? ProjectVersion { get; private set; }

    public Guid SubmittedByUserId { get; private set; }

    public User SubmittedBy { get; private set; } = null!;

    [MaxLength(10000)]
    public string? SubmittedAssetReferenceIdsJson { get; private set; }

    [NotMapped]
    public IReadOnlyList<Guid> SubmittedAssetReferenceIds => string.IsNullOrWhiteSpace(SubmittedAssetReferenceIdsJson)
        ? []
        : JsonSerializer.Deserialize<Guid[]>(SubmittedAssetReferenceIdsJson) ?? [];

    [MaxLength(1000)]
    public string? PreferredAvailability { get; private set; }

    public TestingApplicationStatus Status { get; private set; } = TestingApplicationStatus.Pending;

    public Guid? AssignedSlotId { get; private set; }

    public TestingEventSlot? AssignedSlot { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public User? DecidedBy { get; private set; }

    [MaxLength(2000)]
    public string? DecisionRationale { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public ICollection<TestingApplicationVote> Votes { get; private set; } = new List<TestingApplicationVote>();

    private TestingProjectApplication()
    {
    }

    public static TestingProjectApplication Submit(
        Guid eventId,
        Guid projectId,
        Guid? projectVersionId,
        Guid submittedByUserId,
        string? preferredAvailability,
        Guid? tenantId,
        IReadOnlyCollection<Guid>? submittedAssetReferenceIds = null)
    {
        if (eventId == Guid.Empty || projectId == Guid.Empty || submittedByUserId == Guid.Empty)
            throw new ArgumentException("Event, project, and applicant are required.");

        return new TestingProjectApplication
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ProjectId = projectId,
            ProjectVersionId = projectVersionId,
            SubmittedByUserId = submittedByUserId,
            SubmittedAssetReferenceIdsJson = SerializeAssetIds(submittedAssetReferenceIds),
            PreferredAvailability = string.IsNullOrWhiteSpace(preferredAvailability) ? null : preferredAvailability.Trim(),
            TenantId = tenantId,
        };
    }

    private static string? SerializeAssetIds(IReadOnlyCollection<Guid>? assetReferenceIds)
    {
        var normalized = assetReferenceIds?.Where(id => id != Guid.Empty).Distinct().Take(100).ToArray();
        return normalized is { Length: > 0 } ? JsonSerializer.Serialize(normalized) : null;
    }

    public void BeginReview()
    {
        if (Status != TestingApplicationStatus.Pending) throw new InvalidOperationException("Only pending applications can enter review.");
        Status = TestingApplicationStatus.UnderReview;
        Touch();
    }

    public void Approve(Guid decidedByUserId, Guid slotId, string? rationale)
    {
        if (Status is not (TestingApplicationStatus.Pending or TestingApplicationStatus.UnderReview or TestingApplicationStatus.Waitlisted))
            throw new InvalidOperationException("Only active applications can be approved.");
        if (decidedByUserId == Guid.Empty || slotId == Guid.Empty)
            throw new ArgumentException("Decision actor and slot are required.");
        if (AssignedSlotId.HasValue) throw new InvalidOperationException("The application already has an assigned slot.");

        Status = TestingApplicationStatus.Approved;
        AssignedSlotId = slotId;
        DecidedByUserId = decidedByUserId;
        DecisionRationale = string.IsNullOrWhiteSpace(rationale) ? null : rationale.Trim();
        DecidedAt = SystemClock.UtcNow;
        Touch();
    }

    public void Reject(Guid decidedByUserId, string rationale)
    {
        if (Status is not (TestingApplicationStatus.Pending or TestingApplicationStatus.UnderReview or TestingApplicationStatus.Waitlisted))
            throw new InvalidOperationException("Only active applications can be rejected.");
        if (decidedByUserId == Guid.Empty) throw new ArgumentException("Decision actor is required.", nameof(decidedByUserId));
        if (string.IsNullOrWhiteSpace(rationale)) throw new ArgumentException("A rejection rationale is required.", nameof(rationale));

        Status = TestingApplicationStatus.Rejected;
        AssignedSlotId = null;
        DecidedByUserId = decidedByUserId;
        DecisionRationale = rationale.Trim();
        DecidedAt = SystemClock.UtcNow;
        Touch();
    }

    public void PlaceOnWaitlist(Guid decidedByUserId, string? rationale)
    {
        if (Status is not (TestingApplicationStatus.Pending or TestingApplicationStatus.UnderReview))
            throw new InvalidOperationException("Only active applications can be waitlisted.");
        Status = TestingApplicationStatus.Waitlisted;
        DecidedByUserId = decidedByUserId;
        DecisionRationale = string.IsNullOrWhiteSpace(rationale) ? null : rationale.Trim();
        DecidedAt = SystemClock.UtcNow;
        Touch();
    }

    public void ReassignSlot(Guid slotId)
    {
        if (Status != TestingApplicationStatus.Approved)
            throw new InvalidOperationException("Only approved applications can be reassigned.");
        if (slotId == Guid.Empty) throw new ArgumentException("Slot is required.", nameof(slotId));
        AssignedSlotId = slotId;
        Touch();
    }
    public void Withdraw()
    {
        if (Status is TestingApplicationStatus.Approved or TestingApplicationStatus.Rejected)
            throw new InvalidOperationException("A decided application cannot be withdrawn.");
        Status = TestingApplicationStatus.Withdrawn;
        Touch();
    }
}
