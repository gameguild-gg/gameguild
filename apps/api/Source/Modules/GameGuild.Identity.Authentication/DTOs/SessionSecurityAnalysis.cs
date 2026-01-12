namespace GameGuild.Identity.Authentication;

/// <summary>
/// Security analysis result for a session
/// </summary>
public class SessionSecurityAnalysis
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public bool IsSuspicious { get; set; }
    public bool UnusualActivityDetected { get => IsSuspicious; set => IsSuspicious = value; }
    public int RiskScore { get; set; }
    public int ActiveSessionCount { get; set; }
    public int TotalDeviceCount { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public List<string> SecurityFlags { get; set; } = new();
    public List<string> RiskFactors { get => SecurityFlags; set => SecurityFlags = value; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}
