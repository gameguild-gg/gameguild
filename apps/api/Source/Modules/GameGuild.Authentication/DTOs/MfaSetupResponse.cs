namespace GameGuild.Authentication.DTOs;

/// <summary>
///     MFA setup response
/// </summary>
public class MfaSetupResponse
{
    public string SecretKey { get; set; } = string.Empty;

    public string QrCodeUri { get; set; } = string.Empty;

    public string[ ] BackupCodes { get; set; } = [];
}
