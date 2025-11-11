using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using DomainResponses = GameGuild.Authentication.Models.Responses;
using ApplicationDtos = GameGuild.Authentication.DTOs;

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
        // Fetch user details from repository
        var user = await authUserRepository.GetByIdAsync(domainResponse.UserId, cancellationToken);

        if (user == null) { throw new InvalidOperationException($"User with ID {domainResponse.UserId} not found"); }

        return new ApplicationDtos.SignInResponse
        {
            AccessToken = domainResponse.AccessToken,
            RefreshToken = domainResponse.RefreshToken,
            ExpiresAt = domainResponse.ExpiresAt,
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(domainResponse.ExpiresIn),
            RefreshTokenExpiresAt = domainResponse.ExpiresAt,
            User = new ApplicationDtos.UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username ?? user.Email,
                FirstName = null, // AuthUser doesn't have these fields
                LastName = null,
                PhoneNumber = null,
                EmailVerified = false, // Not stored in AuthUser
                PhoneNumberVerified = false,
                CreatedAt = user.CreatedAt,
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
