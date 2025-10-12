namespace GameGuild.Modules.Authentication;

/// <summary>
/// Result of MFA verification operation
/// </summary>
public class MfaVerificationResult
{
    public bool IsSuccess { get; private set; }

    public string? Message { get; private set; }

    public string[ ]? BackupCodes { get; private set; }

    private MfaVerificationResult(bool isSuccess, string? message = null, string[ ]? backupCodes = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        BackupCodes = backupCodes;
    }

    public static MfaVerificationResult Success(string message, string[ ]? backupCodes = null) => new(true, message, backupCodes);

    public static MfaVerificationResult Failure(string message) => new(false, message);
}
