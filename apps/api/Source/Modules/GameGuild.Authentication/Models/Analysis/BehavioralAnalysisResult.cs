using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Models.Analysis;

/// <summary>
///     Result of behavioral pattern analysis for authentication attempts.
/// </summary>
public class BehavioralAnalysisResult
{
    /// <summary>
    ///     Overall risk level assessment.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Numerical risk score (0-100).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Whether this matches the user's typical behavior.
    /// </summary>
    public bool MatchesTypicalBehavior { get; set; }

    /// <summary>
    ///     Whether this is an unusual time for the user to log in.
    /// </summary>
    public bool IsUnusualTime { get; set; }

    /// <summary>
    ///     Whether this is a new device for the user.
    /// </summary>
    public bool IsNewDevice { get; set; }

    /// <summary>
    ///     Whether this is a new location for the user.
    /// </summary>
    public bool IsNewLocation { get; set; }

    /// <summary>
    ///     Detected anomalies or suspicious patterns.
    /// </summary>
    public List<string> DetectedAnomalies { get; set; } = new List<string>();

    /// <summary>
    ///     Recommended actions based on the analysis.
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new List<string>();

    /// <summary>
    ///     Confidence level in the analysis (0-1).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    ///     Additional analysis metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
