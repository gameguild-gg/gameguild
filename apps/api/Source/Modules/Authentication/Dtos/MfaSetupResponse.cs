namespace GameGuild.Modules.Authentication;


public class MfaSetupResponse {
    public string SetupId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = [];
}