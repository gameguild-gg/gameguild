namespace GameGuild.Identity.Authentication;

using Authentication_RefreshTokenResponse = RefreshTokenResponse;
using Authentication_SignInResponse = SignInResponse;
using Authentication_UserDto = UserDto;
using Responses_SignInResponse = SignInResponse;
// using Responses_TokenRefreshResponse = TokenRefreshResponse; // Type doesn't exist

/// <summary>
///     Mapping extensions for converting between Domain models and Application DTOs
/// </summary>
public static class AuthenticationMappings
{
    /// <summary>
    ///     Maps Domain SignInResponse to Application SignInResponse DTO
    /// </summary>
    public static async Task<Authentication_SignInResponse> ToDto(this Responses_SignInResponse domainResponse, IAuthUserRepository authUserRepository, CancellationToken cancellationToken = default)
    {
        // Try to fetch user details from repository
        // Note: In some scenarios (e.g., tests with separate DbContext scopes), the user might not be available yet
        var user = await authUserRepository.GetByIdAsync(domainResponse.UserId, cancellationToken);

        return new Authentication_SignInResponse
        {
            AccessToken = domainResponse.AccessToken,
            RefreshToken = domainResponse.RefreshToken,
            ExpiresAt = domainResponse.ExpiresAt,
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(domainResponse.ExpiresIn),
            RefreshTokenExpiresAt = domainResponse.ExpiresAt,
            User = new Authentication_UserDto
            {
                Id = domainResponse.UserId,
                Email = user?.Email ?? domainResponse.Email,
                Username = user?.Username ?? domainResponse.Email,
                FirstName = null, // AuthUser doesn't have these fields
                LastName = null,
                PhoneNumber = null,
                EmailVerified = false, // Not stored in AuthUser
                PhoneNumberVerified = false,
                CreatedAt = user?.CreatedAt ?? DateTime.UtcNow,
                LastLoginAt = null // Not stored in AuthUser
            },
            TenantId = domainResponse.TenantId,
            RequiresMfa = domainResponse.RequiresMfa,
            MfaSessionId = domainResponse.TempToken ?? domainResponse.MfaToken
        };
    }

    /// <summary>
    ///     Maps Domain RefreshTokenResponse to Application RefreshTokenResponse DTO
    /// </summary>
    public static Authentication_RefreshTokenResponse ToDto(this RefreshTokenResponse domainResponse)
    {
        return new Authentication_RefreshTokenResponse { AccessToken = domainResponse.AccessToken, RefreshToken = domainResponse.RefreshToken, ExpiresAt = DateTime.UtcNow.AddSeconds(domainResponse.ExpiresIn) };
    }
}
