namespace GameGuild.Identity.Authentication;

/// <summary>
/// Result of behavioral pattern analysis
/// </summary>
public class BehavioralAnalysisResult
{
    public bool MatchesTypicalPattern { get; set; }
    public bool MatchesTypicalBehavior { get => MatchesTypicalPattern; set => MatchesTypicalPattern = value; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public double Confidence { get; set; } = 0.5;
    public List<string> Deviations { get; set; } = new();
    public List<string> DetectedAnomalies { get => Deviations; set => Deviations = value; }
}
