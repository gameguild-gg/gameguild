namespace GameGuild.Modules.Programs;

/// <summary> Review statistics </summary>
public class ReviewStatistics {
    public int TotalAssignments { get; set; }

    public int CompletedReviews { get; set; }

    public int PendingReviews { get; set; }

    public int EscalatedReviews { get; set; }

    public decimal AverageScore { get; set; }

    public decimal AverageCompletionTimeHours { get; set; }

    public Dictionary<ReviewStatus, int> ReviewsByStatus { get; set; } = new Dictionary<ReviewStatus, int>();

    public decimal ConsensusRate { get; set; }
}