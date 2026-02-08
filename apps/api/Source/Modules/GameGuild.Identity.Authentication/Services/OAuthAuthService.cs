using System.Globalization;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// OAuth authentication: GitHub OAuth, Google OAuth, Google ID token
/// </summary>
public class OAuthAuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IOAuthService oauthService,
    IConfiguration configuration,
    IAuthAttemptService authAttemptService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OAuthAuthService> logger
) : IOAuthAuthService
{
    public async Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing GitHub OAuth sign-in");

        var githubUser = await oauthService.GetUserProfileAsync("github", request.AccessToken).ConfigureAwait(false);

        var userId = Guid.NewGuid();
        var email = githubUser.Email ?? throw new UnauthorizedAccessException("Email not available from GitHub profile");
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };
        await refreshTokenRepository.CreateAsync(refreshToken).ConfigureAwait(false);

        logger.LogInformation("GitHub OAuth sign-in successful for {Email}", email);

        return new SignInResponse
        {
            Success = true,
            Message = "GitHub sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public async Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Google OAuth sign-in");

        var googleUser = await oauthService.GetUserProfileAsync("google", request.AccessToken).ConfigureAwait(false);

        var userId = Guid.NewGuid();
        var email = googleUser.Email ?? throw new UnauthorizedAccessException("Email not available from Google profile");
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };
        await refreshTokenRepository.CreateAsync(refreshToken).ConfigureAwait(false);

        logger.LogInformation("Google OAuth sign-in successful for {Email}", email);

        return new SignInResponse
        {
            Success = true,
            Message = "Google sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public async Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.IdToken)) { throw new ArgumentException("ID token is required"); }

            var googleUser = await oauthService.ValidateIdTokenAsync("google", request.IdToken).ConfigureAwait(false);

            var email = googleUser.Email ?? throw new UnauthorizedAccessException("Email not found in ID token");

            var user = await userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                user = User.CreateOAuthUser(email, googleUser.Name ?? email.Split('@')[0]);

                await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
                await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created new user from Google sign-in: {Email}", email);
            }

            var userId = user.Id;
            var roles = new[] { "User" };

            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

            var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
            var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshTokenValue,
                ExpiresAt = refreshTokenExpiresAt,
                IsRevoked = false,
                CreatedByIp = ipAddress
            };
            await refreshTokenRepository.CreateAsync(refreshTokenEntity).ConfigureAwait(false);

            logger.LogInformation("Google ID token sign-in successful for {Email}", email);

            return new SignInResponse
            {
                Success = true,
                Message = "Google ID token sign-in successful",
                AccessToken = jwtToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UserId = userId,
                Email = email,
                SessionId = refreshTokenEntity.Id
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google ID token validation failed");

            throw new UnauthorizedAccessException($"Google ID token validation failed: {ex.Message}", ex);
        }
    }

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:GitHub:ClientId"];
        var scopes = "user:email";
        var state = Guid.NewGuid().ToString();

        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scopes}&state={state}";

        return Task.FromResult(url);
    }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:Google:ClientId"];
        var scopes = "openid email profile";
        var state = Guid.NewGuid().ToString();

        var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}&response_type=code&state={state}";

        return Task.FromResult(url);
    }
}
