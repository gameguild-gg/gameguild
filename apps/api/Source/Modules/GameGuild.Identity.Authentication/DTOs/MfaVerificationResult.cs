namespace GameGuild.Identity.Authentication;

/// <summary>
///     Result of MFA verification operation
/// </summary>
public class MfaVerificationResult
{
    public MfaVerificationResult() { }

    private MfaVerificationResult(bool isSuccess, string? message = null, string[]? backupCodes = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        BackupCodes = backupCodes;
    }

    public bool IsSuccess { get; set; }
    public bool Success { get => IsSuccess; set => IsSuccess = value; }

    public string? Message { get; set; }

    public string[]? BackupCodes { get; set; }
    
    public bool RequiresAdditionalVerification { get; set; }

    public static MfaVerificationResult Successful(string message, string[]? backupCodes = null) { return new MfaVerificationResult(true, message, backupCodes); }

    public static MfaVerificationResult Failure(string message) { return new MfaVerificationResult(false, message); }
}
