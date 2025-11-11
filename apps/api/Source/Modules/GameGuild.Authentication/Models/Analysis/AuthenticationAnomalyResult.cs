using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Models.Analysis;

/// <summary>
///     Result of authentication anomaly detection analysis.
/// </summary>
public class AuthenticationAnomalyResult
{
    /// <summary>
    ///     Whether any anomalies were detected.
    /// </summary>
    public bool IsAnomalous { get; set; }

    /// <summary>
    ///     Overall risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Numerical risk score (0-100).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Whether impossible travel was detected.
    /// </summary>
    public bool ImpossibleTravelDetected { get; set; }

    /// <summary>
    ///     Whether a brute force attack was detected.
    /// </summary>
    public bool BruteForceDetected { get; set; }

    /// <summary>
    ///     Whether the device is new/unknown.
    /// </summary>
    public bool NewDeviceDetected { get; set; }

    /// <summary>
    ///     Whether the location is new/unusual.
    /// </summary>
    public bool NewLocationDetected { get; set; }

    /// <summary>
    ///     Whether VPN/proxy usage was detected.
    /// </summary>
    public bool VpnProxyDetected { get; set; }

    /// <summary>
    ///     Whether bot/automated behavior was detected.
    /// </summary>
    public bool BotBehaviorDetected { get; set; }

    /// <summary>
    ///     Specific risk factors that contributed to the score.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new List<string>();

    /// <summary>
    ///     Recommended security actions.
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new List<string>();

    /// <summary>
    ///     Whether additional authentication should be required.
    /// </summary>
    public bool ShouldRequireAdditionalAuth { get; set; }

    /// <summary>
    ///     Whether the attempt should be blocked entirely.
    /// </summary>
    public bool ShouldBlock { get; set; }

    /// <summary>
    ///     Additional anomaly metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
