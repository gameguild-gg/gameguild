using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Models.Analysis;

/// <summary>
///     Record of suspicious authentication activity.
/// </summary>
public abstract class SuspiciousActivity
{
    /// <summary>
    ///     Unique identifier for this activity record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     User ID (if known).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Email or username attempted (if applicable).
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    ///     Type of suspicious activity.
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    ///     Risk level of this activity.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Risk score (0-100).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     IP address from which activity originated.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Geographic location information.
    /// </summary>
    public LocationInfo? Location { get; set; }

    /// <summary>
    ///     Device information.
    /// </summary>
    public DeviceInfo? Device { get; set; }

    /// <summary>
    ///     When the activity occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    ///     Detailed description of the suspicious activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Actions taken in response to this activity.
    /// </summary>
    public List<string> ActionsTaken { get; set; } = new List<string>();

    /// <summary>
    ///     Whether this activity was confirmed as malicious.
    /// </summary>
    public bool? IsConfirmedMalicious { get; set; }

    /// <summary>
    ///     Additional activity metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
