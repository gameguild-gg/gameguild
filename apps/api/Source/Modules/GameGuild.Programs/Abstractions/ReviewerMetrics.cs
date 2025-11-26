namespace GameGuild.Modules.Programs.Abstractions;

/// <summary> Reviewer performance metrics </summary>
public class ReviewerMetrics {
    public Guid ReviewerId { get; set; }

    public int TotalReviewsAssigned { get; set; }

    public int TotalReviewsCompleted { get; set; }

    public decimal CompletionRate { get; set; }

    public decimal AverageScore { get; set; }

    public decimal AverageCompletionTimeHours { get; set; }

    public int ReviewsEscalated { get; set; }

    public int ReviewsInConflict { get; set; }

    public decimal ReliabilityScore { get; set; }

    public DateTime LastReviewDate { get; set; }
}