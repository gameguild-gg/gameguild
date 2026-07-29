using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed class TestingLearningEvidenceCompletedEvent(
    Guid evidenceId,
    Guid testingEventId,
    Guid slotId,
    Guid userId,
    Guid courseId,
    Guid? cohortId,
    Guid learningActivityId,
    TestingLearningCompletionRequirement requirement,
    DateTime completedAt,
    Guid? tenantId) : DomainEvent
{
    public Guid EvidenceId { get; } = evidenceId;

    public Guid TestingEventId { get; } = testingEventId;

    public Guid SlotId { get; } = slotId;

    public Guid UserId { get; } = userId;

    public Guid CourseId { get; } = courseId;

    public Guid? CohortId { get; } = cohortId;

    public Guid LearningActivityId { get; } = learningActivityId;

    public TestingLearningCompletionRequirement Requirement { get; } = requirement;

    public DateTime CompletedAt { get; } = completedAt;

    public Guid? TenantId { get; } = tenantId;
}
