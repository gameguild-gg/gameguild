namespace GameGuild.Identity.Authentication;

/// <summary>
/// Service interface for local authentication: sign-in, sign-up, refresh token, token revocation
/// </summary>
public interface ILocalAuthService
{
    Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request, CancellationToken cancellationToken = default);

    Task<SignInResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default);
}
