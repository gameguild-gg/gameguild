namespace GameGuild.Identity.Authentication;

/// <summary>
///     Result of initiating SMS MFA setup.
/// </summary>
public class SmsMfaSetupResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? PhoneNumberMasked { get; set; }

    public int ExpiresInSeconds { get; set; }
}
