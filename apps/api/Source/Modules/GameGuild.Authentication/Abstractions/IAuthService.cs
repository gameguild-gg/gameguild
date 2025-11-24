using GameGuild.Authentication.Models.Requests;
using GameGuild.Authentication.Models.Responses;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Core authentication service for local and social authentication
/// </summary>
public interface IAuthService
{
    Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default);

    Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequest request, CancellationToken cancellationToken = default);

    Task<string> GetGitHubAuthUrlAsync(string redirectUri);

    Task<string> GetGoogleAuthUrlAsync(string redirectUri);

    Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> VerifyWeb3SignatureAsync(Web3VerificationRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ForgotPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId, CancellationToken cancellationToken = default);
}
