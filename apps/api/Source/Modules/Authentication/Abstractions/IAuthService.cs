namespace GameGuild.Modules.Authentication;

public interface IAuthService
{
    Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request);

    Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request);

    // Return a full SignInResponse on refresh for consistency with sign-in endpoints
    Task<SignInResponse> RefreshTokenAsync(RefreshTokenRequest request);

    Task RevokeRefreshTokenAsync(string token, string ipAddress);

    Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request);

    Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request);

    Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequestDto request);

    Task<string> GetGitHubAuthUrlAsync(string redirectUri);

    Task<string> GetGoogleAuthUrlAsync(string redirectUri);

    Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request);

    Task<SignInResponse> VerifyWeb3SignatureAsync(Web3AuthenticationVerificationRequest request);

    Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request);

    Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest);

    Task<EmailOperationResponse> ForgotPasswordAsync(ForgotPasswordRequestDto request);

    Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request);

    Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId);
}
