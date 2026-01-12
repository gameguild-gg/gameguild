namespace GameGuild.Identity.Authentication;

/// <summary>
/// Result of authentication anomaly analysis
/// </summary>
public class AuthenticationAnomalyResult
{
    public bool IsSuspicious { get; set; }
    public bool IsAnomalous { get => IsSuspicious; set => IsSuspicious = value; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public List<string> DetectedAnomalies { get; set; } = new();
    public List<string> RiskFactors { get => DetectedAnomalies; set => DetectedAnomalies = value; }
}
