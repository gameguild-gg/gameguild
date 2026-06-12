namespace GameGuild.Identity.Authentication;

/// <summary>
///     Options for local SMS MFA verification delivery.
/// </summary>
public sealed class SmsMfaOptions
{
    public const string SectionName = "Authentication:SmsMfa";

    public bool Enabled { get; set; } = true;

    public int CodeLength { get; set; } = 6;

    public int CodeExpirationSeconds { get; set; } = 300;
}
