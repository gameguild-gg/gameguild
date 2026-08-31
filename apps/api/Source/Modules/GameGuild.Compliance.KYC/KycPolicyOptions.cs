namespace GameGuild.Compliance.KYC;

public sealed class KycPolicyOptions
{
    public const string SectionName = "Compliance:KycAml:Policy";

    /// <summary>
    /// Approved evidence is immediately stale when this value is not configured.
    /// </summary>
    public TimeSpan ApprovedEvidenceLifetime { get; set; }
    public TimeSpan ReviewEvidenceLifetime { get; set; }
    public long PolicyVersion { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
}
