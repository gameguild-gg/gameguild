namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Result of MFA verification operation
/// </summary>
public class MfaVerificationResult
{
    public bool Success { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsLockedOut { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Message { get; set; }

    public string[ ]? BackupCodes { get; set; }

    public bool RequiresAdditionalVerification { get; set; }

    public SignInResponse? SignInResponse { get; set; }

    public static MfaVerificationResult SuccessResult(bool isEnabled = true, string[ ]? backupCodes = null) { return new MfaVerificationResult { Success = true, IsEnabled = isEnabled, BackupCodes = backupCodes }; }

    public static MfaVerificationResult FailureResult(string errorMessage, int failedAttempts = 0, bool isLockedOut = false, DateTime? lockoutEnd = null)
    {
        return new MfaVerificationResult { Success = false, ErrorMessage = errorMessage, Message = errorMessage, FailedAttempts = failedAttempts, IsLockedOut = isLockedOut, LockoutEnd = lockoutEnd };
    }
}
