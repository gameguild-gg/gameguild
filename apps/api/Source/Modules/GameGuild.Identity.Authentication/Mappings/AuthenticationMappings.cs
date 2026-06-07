using GameGuild.Identity.Users;

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
    public static async Task<Authentication_SignInResponse> ToDto(this Responses_SignInResponse domainResponse, IUserRepository userRepository, CancellationToken cancellationToken = default)
    {
        // Try to fetch user details from repository
        // Note: In some scenarios (e.g., tests with separate DbContext scopes), the user might not be available yet
        var user = await userRepository.GetByIdAsync(domainResponse.UserId, cancellationToken).ConfigureAwait(false);

        var accessTokenExpiresAt = domainResponse.AccessTokenExpiresAt == default
            ? (domainResponse.ExpiresIn > 0 ? SystemClock.UtcNow.AddSeconds(domainResponse.ExpiresIn) : domainResponse.ExpiresAt)
            : domainResponse.AccessTokenExpiresAt;

        var refreshTokenExpiresAt = domainResponse.RefreshTokenExpiresAt == default
            ? domainResponse.ExpiresAt
            : domainResponse.RefreshTokenExpiresAt;

        return new Authentication_SignInResponse
        {
            Success = domainResponse.Success,
            Message = domainResponse.Message,
            AccessToken = domainResponse.AccessToken,
            RefreshToken = domainResponse.RefreshToken,
            ExpiresAt = domainResponse.ExpiresAt,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = domainResponse.ExpiresIn,
            UserId = domainResponse.UserId,
            Email = domainResponse.Email,
            SessionId = domainResponse.SessionId,
            TempToken = domainResponse.TempToken,
            MfaToken = domainResponse.MfaToken,
            User = new Authentication_UserDto
            {
                Id = domainResponse.UserId,
                Email = user?.Email ?? domainResponse.Email,
                Username = user?.Username ?? domainResponse.Email,
                FirstName = user?.Name?.Split(' ').FirstOrDefault(),
                LastName = user?.Name?.Split(' ').Skip(1).FirstOrDefault(),
                PhoneNumber = null,
                EmailVerified = user?.IsEmailVerified ?? false,
                PhoneNumberVerified = false,
                CreatedAt = user?.CreatedAt ?? SystemClock.UtcNow,
                LastLoginAt = user?.LastLoginAt
            },
            TenantId = domainResponse.TenantId,
            AvailableTenants = domainResponse.AvailableTenants,
            RequiresMfa = domainResponse.RequiresMfa,
            MfaSessionId = domainResponse.MfaSessionId,
            RequiresStepUp = domainResponse.RequiresStepUp,
            StepUpToken = domainResponse.StepUpToken,
            StepUpExpiresAt = domainResponse.StepUpExpiresAt,
            RiskLevel = domainResponse.RiskLevel,
            RiskFactors = domainResponse.RiskFactors,
            AvailableMethods = domainResponse.AvailableMethods
        };
    }

    /// <summary>
    ///     Maps Domain RefreshTokenResponse to Application RefreshTokenResponse DTO
    /// </summary>
    public static Authentication_RefreshTokenResponse ToDto(this RefreshTokenResponse domainResponse)
    {
        return new Authentication_RefreshTokenResponse { AccessToken = domainResponse.AccessToken, RefreshToken = domainResponse.RefreshToken, ExpiresAt = SystemClock.UtcNow.AddSeconds(domainResponse.ExpiresIn) };
    }
}
