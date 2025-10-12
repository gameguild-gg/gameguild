namespace GameGuild.Modules.Authentication;

/// <summary>
/// Configuration options for user enumeration protection
/// </summary>
public class UserEnumerationProtectionOptions
{
    public const string SectionName = "Authentication:UserEnumerationProtection";

    /// <summary>
    /// Whether user enumeration protection is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum processing time in milliseconds
    /// </summary>
    public int MinProcessingTimeMs { get; set; } = 200;

    /// <summary>
    /// Maximum processing time in milliseconds
    /// </summary>
    public int MaxProcessingTimeMs { get; set; } = 800;

    /// <summary>
    /// Target processing time in milliseconds
    /// </summary>
    public int TargetProcessingTimeMs { get; set; } = 400;

    /// <summary>
    /// Whether to log timing information for analysis
    /// </summary>
    public bool LogTimingAnalysis { get; set; } = false;

    /// <summary>
    /// Custom error message to use (if not set, uses default)
    /// </summary>
    public string? CustomErrorMessage { get; set; }

    /// <summary>
    /// Whether to perform dummy password hashing for non-existent users
    /// </summary>
    public bool PerformDummyHashing { get; set; } = true;

    /// <summary>
    /// Additional delay variance in milliseconds to add randomness
    /// </summary>
    public int DelayVarianceMs { get; set; } = 100;
}
