namespace GameGuild.Modules.Authentication;

public class MfaVerificationResponse
{
    public bool IsValid { get; set; }

    public bool IsBackupCode { get; set; }

    public int? RemainingBackupCodes { get; set; }
}
