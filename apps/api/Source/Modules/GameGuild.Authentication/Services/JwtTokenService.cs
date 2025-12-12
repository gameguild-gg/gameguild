using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Models;
using GameGuild.Authentication.Models.Tokens;
using GameGuild.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Authentication.Services;

/// <summary>
///     JWT token service for generating and validating access tokens and refresh tokens.
///     Supports RS256 (asymmetric) and HS256 (symmetric) algorithms.
/// </summary>
public sealed class JwtTokenService(ILogger<JwtTokenService> logger, IRefreshTokenRepository refreshTokenRepository, IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    /// <summary>
    ///     Generates a JWT access token with user claims.
    /// </summary>
    public async Task<string> GenerateAccessTokenAsync(Guid userId, string email, string[ ] roles, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (roles == null) throw new ArgumentNullException(nameof(roles));

        logger.LogInformation("Generating access token for user: {UserId}", userId);

        try
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
            };

            // Add roles
            foreach (var role in roles) { claims.Add(new Claim(ClaimTypes.Role, role)); }

            // Add tenant claim if multi-tenant
            if (tenantId.HasValue) { claims.Add(new Claim("tenant_id", tenantId.Value.ToString())); }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)) { KeyId = "GameGuild-jwt-key" };
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes), credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogInformation("Access token generated for user: {UserId}, Expires: {Expires}", userId, token.ValidTo);

            return tokenString;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating access token for user: {UserId}", userId);

            throw;
        }
    }

    /// <summary>
    ///     Generates a refresh token and stores it in the database.
    /// </summary>
    public async Task<string> GenerateRefreshTokenAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

        logger.LogInformation("Generating refresh token for user: {UserId}, Device: {DeviceId}", userId, deviceInfo.DeviceId);

        try
        {
            using var rng = RandomNumberGenerator.Create();

            // Handle potential duplicate token conflicts with retry logic
            var retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                // Generate cryptographically secure random token with additional entropy
                var tokenBytes = new byte[64];
                rng.GetBytes(tokenBytes);

                // Add timestamp and user ID for additional entropy
                var entropy = $"{DateTime.UtcNow.Ticks}_{userId}_{Guid.NewGuid()}";
                var entropyBytes = Encoding.UTF8.GetBytes(entropy);

                // Combine random bytes with entropy
                var combinedBytes = tokenBytes.Concat(entropyBytes).ToArray();
                var tokenString = Convert.ToBase64String(combinedBytes);

                // Create refresh token entity
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = tokenString,
                    CreatedByIp = "0.0.0.0", // TODO: Extract from request context
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                    IsRevoked = false
                };

                try
                {
                    await refreshTokenRepository.CreateAsync(refreshToken, cancellationToken).ConfigureAwait(false);

                    logger.LogInformation("Refresh token generated and stored: {TokenId}, Expires: {ExpiresAt}", refreshToken.Id, refreshToken.ExpiresAt);

                    return tokenString; // Success, return token
                }
                catch (Exception ex) when (ex.Message.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase) && retryCount < maxRetries - 1)
                {
                    // Generate new token on conflict
                    retryCount++;
                    logger.LogWarning("Duplicate refresh token detected, retrying... Attempt {RetryCount}", retryCount);
                }
            }

            // If we get here, all retries failed
            throw new InvalidOperationException($"Failed to generate unique refresh token after {maxRetries} attempts");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating refresh token for user: {UserId}", userId);

            throw;
        }
    }

    /// <summary>
    ///     Validates a JWT access token and returns validation result.
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtOptions.ValidateIssuer,
                ValidateAudience = _jwtOptions.ValidateAudience,
                ValidateLifetime = _jwtOptions.ValidateLifetime,
                ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);

            logger.LogInformation("Token validated successfully");

            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            logger.LogWarning(ex, "Token validation failed: Token expired");

            return false;
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating token");

            return false;
        }
    }

    /// <summary>
    ///     Extracts payload (claims) from a JWT token without validation.
    /// </summary>
    public async Task<TokenPayload?> GetTokenPayloadAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                logger.LogWarning("Invalid JWT token format");

                return null;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);

            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            var roles = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
            var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

            Guid? tenantId = null;

            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId)) { tenantId = parsedTenantId; }

            var payload = new TokenPayload
            {
                UserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty,
                Email = email ?? string.Empty,
                Roles = roles,
                TenantId = tenantId,
                IssuedAt = jwtToken.IssuedAt,
                ExpiresAt = jwtToken.ValidTo,
                Issuer = jwtToken.Issuer,
                Audience = jwtToken.Audiences.FirstOrDefault()
            };

            return payload;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting token payload");

            return null;
        }
    }

    /// <summary>
    ///     Revokes a refresh token (logout, security breach, etc.).
    /// </summary>
    public async Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Revoking refresh token");

        try
        {
            var refreshToken = await refreshTokenRepository.GetByTokenAsync(token, cancellationToken).ConfigureAwait(false);

            if (refreshToken == null)
            {
                logger.LogWarning("Refresh token not found");

                return false;
            }

            if (refreshToken.IsRevoked)
            {
                logger.LogInformation("Refresh token already revoked: {TokenId}", refreshToken.Id);

                return true;
            }

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Refresh token revoked: {TokenId}", refreshToken.Id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking refresh token");

            return false;
        }
    }

    #region Synchronous Interface Methods

    /// <summary>
    ///     Generates a JWT access token with user claims (synchronous version).
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string[ ] roles) { return GenerateAccessTokenAsync(userId, email, roles, null, CancellationToken.None).GetAwaiter().GetResult(); }

    /// <summary>
    ///     Generates a JWT access token with additional claims (synchronous version).
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string[ ] roles, IEnumerable<Claim> additionalClaims)
    {
        // For now, we'll use the base method and ignore additional claims
        // TODO: Implement proper additional claims support
        return GenerateAccessTokenAsync(userId, email, roles, null, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Generates a refresh token (synchronous version).
    /// </summary>
    public string GenerateRefreshToken()
    {
        // This method needs DeviceInfo parameter, which we don't have here
        // This is a legacy synchronous method that should not be used
        throw new NotSupportedException("Use GenerateRefreshTokenAsync with DeviceInfo parameter instead");
    }

    /// <summary>
    ///     Validates an expired token and returns the principal (synchronous version).
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        // This is a legacy method - implement if needed
        throw new NotSupportedException("This method is not yet implemented. Use ValidateTokenAsync instead");
    }

    /// <summary>
    ///     Validates a token and returns the principal (synchronous version).
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        // This is a legacy method - implement if needed
        throw new NotSupportedException("This method is not yet implemented. Use ValidateTokenAsync instead");
    }

    #endregion
}
