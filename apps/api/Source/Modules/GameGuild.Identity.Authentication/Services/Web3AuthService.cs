using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Web3 authentication: challenge generation and signature verification
/// </summary>
public class Web3AuthService(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IWeb3Service web3Service,
    IConfiguration configuration,
    IAuthAttemptService authAttemptService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<Web3AuthService> logger
) : IWeb3AuthService
{
    public async Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Web3 challenge for wallet {WalletAddress}", request.WalletAddress);

        var challenge = await web3Service.GenerateChallengeAsync(request.WalletAddress).ConfigureAwait(false);

        return new Web3ChallengeResponse { Challenge = challenge.Message, ExpiresAt = challenge.ExpiresAt };
    }

    public async Task<SignInResponse> VerifyWeb3SignatureAsync(Web3VerificationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying Web3 signature for wallet {WalletAddress}", request.WalletAddress);

        var isValid = await web3Service.VerifySignatureAsync(request.WalletAddress, request.Signature, request.Challenge).ConfigureAwait(false);

        if (!isValid) { throw new UnauthorizedAccessException("Invalid Web3 signature"); }

        var userId = Guid.NewGuid();
        var email = $"{request.WalletAddress.ToLowerInvariant()}@web3.local";
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Web3 Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshExpiresInDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };
        await refreshTokenRepository.CreateAsync(refreshToken).ConfigureAwait(false);

        logger.LogInformation("Web3 signature verified for wallet {WalletAddress}", request.WalletAddress);

        var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

        return new SignInResponse
        {
            Success = true,
            Message = "Web3 authentication successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }
}
