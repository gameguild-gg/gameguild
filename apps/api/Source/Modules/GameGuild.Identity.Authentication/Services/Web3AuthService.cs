using System.Diagnostics;
using System.Globalization;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    ILogger<Web3AuthService> logger,
    IOptions<JwtOptions>? jwtOptions = null
) : IWeb3AuthService
{
    public async Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Web3 challenge for wallet {WalletAddress}", request.WalletAddress);

        var challenge = await web3Service.GenerateChallengeAsync(request.WalletAddress, chainId: request.ChainId).ConfigureAwait(false);

        return new Web3ChallengeResponse { Challenge = challenge.Message, Nonce = challenge.Nonce, ExpiresAt = challenge.ExpiresAt };
    }

    public async Task<SignInResponse> VerifyWeb3SignatureAsync(Web3VerificationRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Verifying Web3 signature for wallet {WalletAddress}", request.WalletAddress);

        var isValid = await web3Service.VerifySignatureAsync(
            request.WalletAddress,
            request.Signature,
            request.Challenge,
            request.ChainId,
            request.Nonce).ConfigureAwait(false);

        if (!isValid)
        {
            var failedContext = httpContextAccessor.HttpContext;
            await authAttemptService.RecordFailedAttemptAsync(
                "web3@web3.local",
                null,
                authAttemptService.GetClientIpAddress(failedContext),
                failedContext?.Request.Headers.UserAgent.ToString(),
                "InvalidSignature",
                stopwatch.Elapsed,
                "Web3").ConfigureAwait(false);
            throw new UnauthorizedAccessException("Invalid Web3 signature");
        }

        var userId = Guid.NewGuid();
        var email = $"{request.WalletAddress.ToLowerInvariant()}@web3.local";
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Web3 Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = jwtOptions?.Value.RefreshTokenExpirationDays
                                   ?? int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
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
        await authAttemptService.RecordSuccessfulAttemptAsync(
            "web3@web3.local",
            userId,
            ipAddress ?? "unknown",
            userAgent,
            stopwatch.Elapsed,
            "Web3").ConfigureAwait(false);

        logger.LogInformation("Web3 signature verified for wallet {WalletAddress}", request.WalletAddress);

        var accessTokenExpirationMinutes = jwtOptions?.Value.AccessTokenExpirationMinutes
                                           ?? int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

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
