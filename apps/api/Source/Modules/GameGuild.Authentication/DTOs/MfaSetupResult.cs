namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Result of MFA setup operation
/// </summary>
public class MfaSetupResult
{
    private MfaSetupResult(bool isSuccess, string? errorMessage = null, string? secretKey = null, string? qrCodeData = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        SecretKey = secretKey;
        QrCodeData = qrCodeData;
    }

    public bool IsSuccess { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? SecretKey { get; private set; }

    public string? QrCodeData { get; private set; }

    public static MfaSetupResult Success(string secretKey, string qrCodeData) { return new MfaSetupResult(true, null, secretKey, qrCodeData); }

    public static MfaSetupResult Failure(string errorMessage) { return new MfaSetupResult(false, errorMessage); }
}
