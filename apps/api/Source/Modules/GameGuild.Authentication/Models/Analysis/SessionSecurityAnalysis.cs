using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Models.Analysis;

/// <summary>
///     Session security analysis result
/// </summary>
public class SessionSecurityAnalysis
{
    public Guid UserId { get; set; }

    public int ActiveSessionCount { get; set; }

    public int TotalDeviceCount { get; set; }

    public bool UnusualActivityDetected { get; set; }

    public RiskLevel RiskLevel { get; set; }

    public int RiskScore { get; set; }

    public bool IsNewDevice { get; set; }

    public bool IsNewLocation { get; set; }

    public bool IsSuspiciousPattern { get; set; }

    public string[ ] RiskFactors { get; set; } = [];

    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
