namespace GameGuild.Modules.Programs;

/// <summary> Consensus calculation result </summary>
public class ConsensusResult {
    public decimal ConsensusScore { get; set; }

    public decimal AverageScore { get; set; }

    public decimal ScoreVariance { get; set; }

    public int TotalReviews { get; set; }

    public int CompletedReviews { get; set; }

    public bool HasConsensus { get; set; }

    public decimal ConfidenceLevel { get; set; }
}