namespace GameGuild.Identity.Authentication;

/// <summary>
/// Suspicious activity detected during authentication
/// </summary>
public class SuspiciousActivity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Identifier { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public DateTime DetectedAt { get; set; }
    public DateTime OccurredAt { get => DetectedAt; set => DetectedAt = value; }
    public bool? IsConfirmedMalicious { get; set; }
    public List<string> ActionsTaken { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
