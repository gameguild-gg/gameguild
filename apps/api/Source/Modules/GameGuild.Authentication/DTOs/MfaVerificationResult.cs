namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Result of MFA verification operation
/// </summary>
public class MfaVerificationResult
{
    private MfaVerificationResult(bool isSuccess, string? message = null, string[ ]? backupCodes = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        BackupCodes = backupCodes;
    }

    public bool IsSuccess { get; private set; }

    public string? Message { get; private set; }

    public string[ ]? BackupCodes { get; private set; }

    public static MfaVerificationResult Success(string message, string[ ]? backupCodes = null) { return new MfaVerificationResult(true, message, backupCodes); }

    public static MfaVerificationResult Failure(string message) { return new MfaVerificationResult(false, message); }
}
