namespace GameGuild.Modules.Authentication;

/// <summary>
/// Security analysis of a session request
/// </summary>
public class SessionSecurityAnalysis
{
    public bool IsNewLocation { get; set; }

    public bool IsNewDevice { get; set; }

    public int RecentLocationCount { get; set; }

    public int RecentDeviceCount { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public RiskLevel RiskScore { get; set; }
}
