namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for Google sign-in requests
/// </summary>
public class GoogleSignInRequestDto {
    public string IdToken { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

/// <summary>
/// DTO for password reset requests
/// </summary>
public class PasswordResetRequestDto {
    public string Email { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// DTO for Web3 sign-in requests
/// </summary>
public class Web3SignInRequestDto {
    public string WalletAddress { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for sending verification email requests
/// </summary>
public class SendVerificationEmailRequestDto {
    public string Email { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// DTO for GitHub callback requests
/// </summary>
public class GitHubCallbackRequestDto {
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

/// <summary>
/// DTO for OAuth sign-in requests
/// </summary>
public class OAuthSignInRequestDto {
    public string Provider { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

/// <summary>
/// DTO for Google ID token requests
/// </summary>
public class GoogleIdTokenRequestDto {
    public string IdToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Web3 signature verification
/// </summary>
public class Web3VerifyRequestDto {
    public string WalletAddress { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for email verification requests
/// </summary>
public class SendEmailVerificationRequestDto {
    public string Email { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// DTO for forgot password requests
/// </summary>
public class ForgotPasswordRequestDto {
    public string Email { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// DTO for change password requests
/// </summary>
public class ChangePasswordRequestDto {
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO for email operation responses
/// </summary>
public class EmailOperationResponseDto {
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
}