namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     MFA setup result
/// </summary>
public class MfaSetupResult
{
    public bool Success { get; set; }

    public string SecretKey { get; set; } = string.Empty;

    public string QrCodeUrl { get; set; } = string.Empty;

    public string QrCodeUri { get; set; } = string.Empty;

    public byte[ ]? QrCodeImage { get; set; }

    public string[ ] BackupCodes { get; set; } = [];

    public string Message { get; set; } = string.Empty;
}
