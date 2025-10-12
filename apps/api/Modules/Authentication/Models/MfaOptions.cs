namespace GameGuild.Modules.Authentication;

/// <summary>
/// Configuration options for MFA
/// </summary>
public class MfaOptions
{
    public string Issuer { get; set; } = "GameGuild";

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;

    public int BackupCodeCount { get; set; } = 10;

    public int TotpWindowSeconds { get; set; } = 30;
}
