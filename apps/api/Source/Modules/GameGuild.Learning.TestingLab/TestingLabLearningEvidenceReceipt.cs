using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.TestingLab;

namespace GameGuild.Learning.TestingLab;

[Table("testing_lab_learning_evidence_receipts")]
public sealed class TestingLabLearningEvidenceReceipt : EntityBase
{
    public Guid EvidenceId { get; private set; }

    public Guid RegistrationId { get; private set; }

    public Guid TestingEventId { get; private set; }

    public Guid SlotId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public Guid? CohortId { get; private set; }

    public Guid LearningActivityId { get; private set; }

    public TestingLearningCompletionRequirement Requirement { get; private set; }

    public DateTime EvidenceCompletedAt { get; private set; }

    public DateTime ConsumedAt { get; private set; }

    private TestingLabLearningEvidenceReceipt()
    {
    }

    public static TestingLabLearningEvidenceReceipt Consume(
        TestingLearningEvidenceCompletedEvent evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new TestingLabLearningEvidenceReceipt
        {
            Id = Guid.NewGuid(),
            EvidenceId = evidence.EvidenceId,
            RegistrationId = evidence.EvidenceId,
            TestingEventId = evidence.TestingEventId,
            SlotId = evidence.SlotId,
            UserId = evidence.UserId,
            CourseId = evidence.CourseId,
            CohortId = evidence.CohortId,
            LearningActivityId = evidence.LearningActivityId,
            Requirement = evidence.Requirement,
            EvidenceCompletedAt = evidence.CompletedAt,
            ConsumedAt = SystemClock.UtcNow,
            TenantId = evidence.TenantId
        };
    }
}
