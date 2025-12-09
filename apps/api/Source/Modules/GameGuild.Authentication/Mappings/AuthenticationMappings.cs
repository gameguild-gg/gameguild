using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using ApplicationDtos = GameGuild.Authentication.DTOs;
using DomainResponses = GameGuild.Authentication.Models.Responses;

namespace GameGuild.Authentication.Mappings;

/// <summary>
///     Mapping extensions for converting between Domain models and Application DTOs
/// </summary>
public static class AuthenticationMappings
{
    /// <summary>
    ///     Maps Domain SignInResponse to Application SignInResponse DTO
    /// </summary>
    public static async Task<ApplicationDtos.SignInResponse> ToDto(this DomainResponses.SignInResponse domainResponse, IAuthUserRepository authUserRepository, CancellationToken cancellationToken = default)
    {
        // Try to fetch user details from repository
        // Note: In some scenarios (e.g., tests with separate DbContext scopes), the user might not be available yet
        var user = await authUserRepository.GetByIdAsync(domainResponse.UserId, cancellationToken);

        return new ApplicationDtos.SignInResponse
        {
            AccessToken = domainResponse.AccessToken,
            RefreshToken = domainResponse.RefreshToken,
            ExpiresAt = domainResponse.ExpiresAt,
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(domainResponse.ExpiresIn),
            RefreshTokenExpiresAt = domainResponse.ExpiresAt,
            User = new ApplicationDtos.AuthenticatedUserDto
            {
                Id = domainResponse.UserId,
                Email = user?.Email ?? domainResponse.Email ?? string.Empty,
                Username = user?.Username ?? domainResponse.Email ?? string.Empty,
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
    public static ApplicationDtos.RefreshTokenResponse ToDto(this DomainResponses.TokenRefreshResponse domainResponse)
    {
        return new ApplicationDtos.RefreshTokenResponse { AccessToken = domainResponse.AccessToken, RefreshToken = domainResponse.RefreshToken, ExpiresAt = DateTime.UtcNow.AddSeconds(domainResponse.ExpiresIn) };
    }
}
