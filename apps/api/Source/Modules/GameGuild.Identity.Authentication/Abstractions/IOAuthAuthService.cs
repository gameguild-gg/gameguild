namespace GameGuild.Identity.Authentication;

/// <summary>
/// Service interface for OAuth authentication: GitHub OAuth, Google OAuth, Google ID token
/// </summary>
public interface IOAuthAuthService
{
    Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequest request, CancellationToken cancellationToken = default);

    Task<string> GetGitHubAuthUrlAsync(string redirectUri);

    Task<string> GetGoogleAuthUrlAsync(string redirectUri);
}
