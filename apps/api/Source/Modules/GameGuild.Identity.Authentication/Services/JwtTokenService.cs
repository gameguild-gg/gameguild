using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     JWT token service for generating and validating access tokens and refresh tokens.
///     Supports RS256 (asymmetric) and HS256 (symmetric) algorithms.
/// </summary>
public sealed class JwtTokenService(
    ILogger<JwtTokenService> logger,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    /// <summary>
    ///     Generates a JWT access token with user claims.
    /// </summary>
    public Task<string> GenerateAccessTokenAsync(Guid userId, string email, string[ ] roles, Guid? tenantId, int tokenVersion = 1, CancellationToken cancellationToken = default)
    {
        return GenerateAccessTokenAsync(
            userId,
            email,
            roles,
            tenantId,
            tokenVersion,
            new DateTimeOffset(DateTime.SpecifyKind(SystemClock.UtcNow, DateTimeKind.Utc)),
            cancellationToken);
    }

    public Task<string> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        string[ ] roles,
        Guid? tenantId,
        int tokenVersion,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken = default)
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
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new Claim("auth_time", authenticatedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                // Token version for immediate revocation support
                // When user changes password/signs out all sessions, increment their token version
                // Validation middleware checks current version vs token version and rejects stale tokens
                new Claim("token_version", tokenVersion.ToString(CultureInfo.InvariantCulture))
            };

            // Add roles
            foreach (var role in roles) { claims.Add(new Claim(ClaimTypes.Role, role)); }

            // Add tenant claim if multi-tenant
            if (tenantId.HasValue) { claims.Add(new Claim("tenant_id", tenantId.Value.ToString())); }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)) { KeyId = "GameGuild-jwt-key" };
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, SystemClock.UtcNow, SystemClock.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes), credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogInformation("Access token generated for user: {UserId}, Expires: {Expires}", userId, token.ValidTo);

            return Task.FromResult(tokenString);
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
    public Task<string> GenerateRefreshTokenAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        return GenerateRefreshTokenAsync(
            userId,
            deviceInfo,
            new DateTimeOffset(DateTime.SpecifyKind(SystemClock.UtcNow, DateTimeKind.Utc)),
            cancellationToken);
    }

    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        DeviceInfo deviceInfo,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken = default)
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
                var entropy = $"{SystemClock.UtcNow.Ticks}_{userId}_{Guid.NewGuid()}";
                var entropyBytes = Encoding.UTF8.GetBytes(entropy);

                // Combine random bytes with entropy
                var combinedBytes = tokenBytes.Concat(entropyBytes).ToArray();
                var tokenString = Convert.ToBase64String(combinedBytes);

                // Hash the token for secure storage (never store plaintext)
                var hashedToken = refreshTokenHasher.HashToken(tokenString);

                // Create refresh token entity with HASHED token
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = hashedToken, // Store hash, not plaintext
                    CreatedByIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                    CreatedAt = authenticatedAt.UtcDateTime,
                    ExpiresAt = SystemClock.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
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
    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, CreateValidationParameters(validateLifetime: _jwtOptions.ValidateLifetime), out _);

            logger.LogInformation("Token validated successfully");

            return Task.FromResult(true);
        }
        catch (SecurityTokenExpiredException ex)
        {
            logger.LogWarning(ex, "Token validation failed: Token expired");

            return Task.FromResult(false);
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating token");

            return Task.FromResult(false);
        }
    }

    /// <summary>
    ///     Extracts payload (claims) from a JWT token without validation.
    /// </summary>
    public Task<TokenPayload?> GetTokenPayloadAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                logger.LogWarning("Invalid JWT token format");

                return Task.FromResult<TokenPayload?>(null);
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

            return Task.FromResult<TokenPayload?>(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting token payload");

            return Task.FromResult<TokenPayload?>(null);
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
            // Hash the incoming token to match against stored hash
            var hashedToken = refreshTokenHasher.HashToken(token);
            var refreshToken = await refreshTokenRepository.GetByTokenAsync(hashedToken, cancellationToken).ConfigureAwait(false);

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
            refreshToken.RevokedAt = SystemClock.UtcNow;

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

    /// <summary>
    ///     Generates a JWT access token for a service account (machine-to-machine).
    /// </summary>
    public Task<(string Token, DateTime ExpiresAt)> GenerateServiceAccountTokenAsync(
        string serviceAccountId,
        string clientId,
        string serviceName,
        IReadOnlySet<string> scopes,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Generating service account token for: {ServiceAccountId}, ClientId: {ClientId}",
            serviceAccountId, clientId);

        try
        {
            var expiresAt = SystemClock.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                // Subject is the service account ID
                new Claim(JwtRegisteredClaimNames.Sub, serviceAccountId),
                // Client ID for OAuth2 compatibility
                new Claim("client_id", clientId),
                // Service name for logging/auditing
                new Claim("service_name", serviceName),
                // Unique token identifier
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Issued at timestamp
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer64),
                // Actor kind to distinguish from user tokens
                new Claim("actor_kind", "Service"),
                // Grant type for OAuth2 compatibility
                new Claim("grant_type", "client_credentials")
            };

            // Add scopes as individual claims
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            // Add tenant claim if multi-tenant
            if (tenantId.HasValue)
            {
                claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey))
            {
                KeyId = "GameGuild-jwt-key"
            };
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: SystemClock.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogInformation(
                "Service account token generated for: {ServiceAccountId}, Expires: {ExpiresAt}",
                serviceAccountId, expiresAt);

            return Task.FromResult((tokenString, expiresAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error generating service account token for: {ServiceAccountId}",
                serviceAccountId);

            throw;
        }
    }

    #region Synchronous Interface Methods

    /// <summary>
    ///     Generates a JWT access token with user claims (synchronous version).
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string[ ] roles) { return GenerateAccessTokenAsync(userId, email, roles, null, tokenVersion: 1, CancellationToken.None).GetAwaiter().GetResult(); }

    /// <summary>
    ///     Generates a JWT access token with additional claims (synchronous version).
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string[ ] roles, IEnumerable<Claim> additionalClaims)
    {
        // NOTE: Additional claims are not yet forwarded to GenerateAccessTokenAsync because
        // the async overload does not accept an additionalClaims parameter. When the async
        // method is extended, wire the claims through here.
        return GenerateAccessTokenAsync(userId, email, roles, null, tokenVersion: 1, CancellationToken.None).GetAwaiter().GetResult();
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
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, CreateValidationParameters(validateLifetime: false), out var securityToken);
        EnsureExpectedSigningAlgorithm(securityToken);
        return principal;
    }

    /// <summary>
    ///     Validates a token and returns the principal (synchronous version).
    /// </summary>
    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, CreateValidationParameters(validateLifetime: _jwtOptions.ValidateLifetime), out var securityToken);
        EnsureExpectedSigningAlgorithm(securityToken);
        return principal;
    }

    #endregion

    private TokenValidationParameters CreateValidationParameters(bool validateLifetime)
        => new()
        {
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
        };

    private static void EnsureExpectedSigningAlgorithm(SecurityToken securityToken)
    {
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
        {
            throw new SecurityTokenException("Invalid token signing algorithm.");
        }
    }
}
