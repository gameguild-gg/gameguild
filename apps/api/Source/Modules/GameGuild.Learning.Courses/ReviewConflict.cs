namespace GameGuild.Programs;

/// <summary> Review conflict detection result </summary>
public class ReviewConflict {
    public Guid SubmissionId { get; set; }

    public IList<PeerReview> ConflictingReviews { get; set; } = new List<PeerReview>();

    public decimal ScoreVariance { get; set; }

    public ConflictSeverity Severity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}