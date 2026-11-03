using System.Collections.Frozen;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     JWT token service for generating and validating access tokens and refresh tokens.
///     Issues and accepts HS256 tokens signed with the ACTIVE key from the key-rotation
///     store (issue #1141); the JWT 'kid' header identifies which key signed each token.
///     When the rotation store is absent or unusable it falls back to the statically
///     configured symmetric key, and that static key always remains accepted so tokens
///     issued before a rotation keep validating.
/// </summary>
public sealed class JwtTokenService(
    ILogger<JwtTokenService> logger,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions,
    IAuthorizationGroupMembershipProvider? groupMembershipProvider = null,
    IOptions<AuthorizationTokenOptions>? authorizationTokenOptions = null,
    IRefreshTokenLifecycleRecorder? lifecycleRecorder = null,
    IPublisher? eventPublisher = null,
    IKeyRotationService? keyRotationService = null,
    IJwtSigningKeyCache? signingKeyCache = null) : IJwtTokenService
{
    private const string StaticSigningKeyId = "GameGuild-jwt-key";

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly AuthorizationTokenOptions _authorizationTokenOptions = authorizationTokenOptions?.Value ?? new AuthorizationTokenOptions();

    private static readonly FrozenSet<string> ReservedAdditionalClaimTypes = new[]
    {
        JwtRegisteredClaimNames.Sub, JwtRegisteredClaimNames.Iss, JwtRegisteredClaimNames.Aud,
        JwtRegisteredClaimNames.Exp, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Iat,
        JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Email,
        ClaimTypes.NameIdentifier, ClaimTypes.Role, ClaimTypes.Email,
        "role", "roles", "token_version", "auth_time", "tid", "sid",
        "perm", "role_id", "group_id", "udt", "amr", "acr",
        "actor_kind", "actor_type", "client_id", "grant_type", "scope",
        ClaimNames.UserId, ClaimNames.TenantId, ClaimNames.Group,
        ClaimNames.MfaVerified, ClaimNames.MfaTime, ClaimNames.MfaTimestamp, ClaimNames.EmailVerified,
        JwtClaimTypes.TenantId, JwtClaimTypes.SessionId, JwtClaimTypes.MfaEnabled,
        JwtClaimTypes.TenantPermissionFlags1, JwtClaimTypes.TenantPermissionFlags2
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Generates a JWT access token with user claims.
    /// </summary>
    public Task<string> GenerateAccessTokenAsync(Guid userId, string email, string[ ] roles, Guid? tenantId, int tokenVersion = 1, CancellationToken cancellationToken = default)
    {
        return GenerateAccessTokenCoreAsync(
            userId,
            email,
            roles,
            tenantId,
            tokenVersion,
            new DateTimeOffset(DateTime.SpecifyKind(SystemClock.UtcNow, DateTimeKind.Utc)),
            null,
            cancellationToken,
            null);
    }

    public Task<string> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        string[ ] roles,
        Guid? tenantId,
        int tokenVersion,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAccessTokenCoreAsync(
            userId,
            email,
            roles,
            tenantId,
            tokenVersion,
            new DateTimeOffset(DateTime.SpecifyKind(SystemClock.UtcNow, DateTimeKind.Utc)),
            sessionId,
            cancellationToken,
            null);
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
        return GenerateAccessTokenCoreAsync(userId, email, roles, tenantId, tokenVersion, authenticatedAt, null, cancellationToken, null);
    }

    public Task<string> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        string[ ] roles,
        Guid? tenantId,
        int tokenVersion,
        DateTimeOffset authenticatedAt,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAccessTokenCoreAsync(userId, email, roles, tenantId, tokenVersion, authenticatedAt, sessionId, cancellationToken, null);
    }

    public Task<string> GenerateMfaAccessTokenAsync(Guid userId, string email, string[] roles, Guid? tenantId,
        int tokenVersion, DateTimeOffset authenticatedAt, Guid sessionId, SignInMfaProof proof,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proof);
        cancellationToken.ThrowIfCancellationRequested();
        if (sessionId == Guid.Empty) { throw new AuthenticationRequiredException("An authenticated session is required."); }
        proof.RequireBinding(userId, tenantId, tokenVersion, authenticatedAt, DateTimeOffset.UtcNow);
        return GenerateAccessTokenCoreAsync(userId, email, roles, tenantId, tokenVersion, authenticatedAt,
            sessionId, cancellationToken, null, proof);
    }

    private async Task<string> GenerateAccessTokenCoreAsync(
        Guid userId,
        string email,
        string[ ] roles,
        Guid? tenantId,
        int tokenVersion,
        DateTimeOffset authenticatedAt,
        Guid? sessionId,
        CancellationToken cancellationToken,
        IEnumerable<Claim>? additionalClaims,
        SignInMfaProof? mfaProof = null)
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
            // The API disables inbound claim mapping and configures "role" as
            // RoleClaimType, so access tokens must emit the canonical short claim.
            foreach (var role in roles) { claims.Add(new Claim(ClaimNames.Role, role)); }

            // Add tenant claim if multi-tenant
            if (tenantId.HasValue) { claims.Add(new Claim("tenant_id", tenantId.Value.ToString())); }

            if (additionalClaims is not null)
            {
                AppendAdditionalClaims(claims, additionalClaims);
            }

            if (mfaProof is not null)
            {
                mfaProof.RequireBinding(userId, tenantId, tokenVersion, authenticatedAt, DateTimeOffset.UtcNow);
                claims.Add(new Claim("amr", "mfa"));
                if (mfaProof.FirstFactor == SignInFirstFactor.Password) { claims.Add(new Claim("amr", "pwd")); }
                if (mfaProof.Method == MfaMethod.Totp) { claims.Add(new Claim("amr", "otp")); }
                claims.Add(new Claim(ClaimNames.MfaVerified, "true"));
                claims.Add(new Claim(ClaimNames.MfaTime, mfaProof.VerifiedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
            }

            if (groupMembershipProvider is not null)
            {
                var groupIds = await groupMembershipProvider
                    .GetActiveGroupIdsAsync(userId, tenantId, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var groupId in groupIds.Where(id => id != Guid.Empty).Distinct())
                {
                    claims.Add(new Claim(_authorizationTokenOptions.GroupIdClaimType, groupId.ToString("D")));
                }
            }

            if (sessionId.HasValue) { claims.Add(new Claim(JwtClaimTypes.SessionId, sessionId.Value.ToString())); }

            var credentials = await ResolveSigningCredentialsAsync(cancellationToken).ConfigureAwait(false);

            var token = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, SystemClock.UtcNow, SystemClock.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes), credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogInformation("Access token generated for user: {UserId}, Expires: {Expires}", userId, token.ValidTo);

            return tokenString;
        }
        catch (Exception ex)
        {
            logger.LogError("Error generating access token for user: {UserId}: {ErrorType}", userId, ex.GetType().FullName);

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

    public Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        DeviceInfo deviceInfo,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken = default)
    {
        return GenerateRefreshTokenAsync(userId, deviceInfo, authenticatedAt, expiresAt: null, cancellationToken);
    }

    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        DeviceInfo deviceInfo,
        DateTimeOffset authenticatedAt,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));
        cancellationToken.ThrowIfCancellationRequested();
        RefreshTokenLifecycleMetrics.RecordAttempt(RefreshTokenLifecycleOperation.Issued);

        logger.LogInformation("Generating refresh token for user: {UserId}", LogRedaction.RedactId(userId, "uid"));

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
                    // The caller-resolved deadline is authoritative so the persisted token row matches
                    // the session policy (persistent "remember me" vs standard lifetime).
                    ExpiresAt = expiresAt ?? SystemClock.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                    IsRevoked = false
                };

                try
                {
                    await refreshTokenRepository.CreateAsync(refreshToken, cancellationToken).ConfigureAwait(false);
                    if (lifecycleRecorder is not null)
                    {
                        await lifecycleRecorder.RecordMutationAsync(new RefreshTokenLifecycleEvent(RefreshTokenLifecycleOperation.Issued,
                            userId, refreshToken.Id), cancellationToken).ConfigureAwait(false);
                    }

                    // In-process refresh-token lifecycle telemetry (issue #1199): this is the
                    // module's single issue point, so every family's token generation is covered.
                    await AuthenticationEventDispatch.PublishAsync(
                        eventPublisher,
                        new RefreshTokenGeneratedEvent(refreshToken.Id.ToString(), userId, refreshToken.ExpiresAt),
                        logger,
                        cancellationToken).ConfigureAwait(false);

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
            logger.LogError("Error generating refresh token for user: {UserId}: {ErrorType}", userId, ex.GetType().FullName);

            throw;
        }
    }

    /// <summary>
    ///     Validates a JWT access token and returns validation result.
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await RefreshValidationKeysAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, CreateValidationParameters(validateLifetime: _jwtOptions.ValidateLifetime), out _);

            logger.LogInformation("Token validated successfully");

            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            logger.LogWarning("Token validation failed: Token expired: {ErrorType}", ex.GetType().FullName);

            return false;
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning("Token validation failed: {ErrorType}", ex.GetType().FullName);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError("Error validating token: {ErrorType}", ex.GetType().FullName);

            return false;
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
            // Read both the canonical API claim and the legacy URI claim so
            // tokens issued immediately before this rollout remain readable.
            var roles = jwtToken.Claims
                .Where(c => c.Type == ClaimNames.Role || c.Type == ClaimTypes.Role || c.Type == "roles")
                .Select(c => c.Value)
                .ToArray();
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
            logger.LogError("Error extracting token payload: {ErrorType}", ex.GetType().FullName);

            return Task.FromResult<TokenPayload?>(null);
        }
    }

    /// <summary>
    ///     Revokes a refresh token (logout, security breach, etc.).
    /// </summary>
    public async Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RefreshTokenLifecycleMetrics.RecordAttempt(RefreshTokenLifecycleOperation.Revoked);
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
            if (lifecycleRecorder is not null)
            {
                await lifecycleRecorder.RecordMutationAsync(new RefreshTokenLifecycleEvent(RefreshTokenLifecycleOperation.Revoked,
                    refreshToken.UserId, refreshToken.Id, refreshToken.SessionId), cancellationToken).ConfigureAwait(false);
            }

            // In-process refresh-token lifecycle telemetry (issue #1199).
            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new RefreshTokenRevokedEvent(refreshToken.Id.ToString(), refreshToken.UserId, "Revoked", SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Refresh token revoked: {TokenId}", refreshToken.Id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Error revoking refresh token: {ErrorType}", ex.GetType().FullName);

            return false;
        }
    }

    /// <summary>
    ///     Generates a JWT access token for a service account (machine-to-machine).
    /// </summary>
    public async Task<(string Token, DateTime ExpiresAt)> GenerateServiceAccountTokenAsync(
        string serviceAccountId,
        string clientId,
        string serviceName,
        IReadOnlySet<string> scopes,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Generating service account token for: {ServiceAccountId}, ClientId: {ClientId}",
            LogRedaction.RedactId(serviceAccountId, "said"), LogRedaction.RedactId(clientId, "cid"));

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

            var credentials = await ResolveSigningCredentialsAsync(cancellationToken).ConfigureAwait(false);

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
                LogRedaction.RedactId(serviceAccountId, "said"), expiresAt);

            return (tokenString, expiresAt);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Error generating service account token for: {ServiceAccountId}: {ErrorType}",
                LogRedaction.RedactId(serviceAccountId, "said"), ex.GetType().FullName);

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
        ArgumentNullException.ThrowIfNull(additionalClaims);
        return GenerateAccessTokenCoreAsync(userId, email, roles, null, 1,
            new DateTimeOffset(DateTime.SpecifyKind(SystemClock.UtcNow, DateTimeKind.Utc)),
            null, CancellationToken.None, additionalClaims).GetAwaiter().GetResult();
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
        RefreshValidationKeysAsync(CancellationToken.None).GetAwaiter().GetResult();
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
        RefreshValidationKeysAsync(CancellationToken.None).GetAwaiter().GetResult();
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, CreateValidationParameters(validateLifetime: _jwtOptions.ValidateLifetime), out var securityToken);
        EnsureExpectedSigningAlgorithm(securityToken);
        return principal;
    }

    #endregion

    private Task RefreshValidationKeysAsync(CancellationToken cancellationToken)
        => keyRotationService is not null && signingKeyCache is not null
            ? signingKeyCache.RefreshAsync(keyRotationService, cancellationToken)
            : Task.CompletedTask;

    private TokenValidationParameters CreateValidationParameters(bool validateLifetime)
    {
        var staticKey = CreateStaticSigningKey();

        return new TokenValidationParameters
        {
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = staticKey,
            // Issue #1141: accept every key currently valid in the rotation store in
            // addition to the static key, so tokens signed by rotated keys (identified
            // by their 'kid' header) validate during and after key rollover.
            IssuerSigningKeys = signingKeyCache?.GetValidationKeys(staticKey),
            TryAllIssuerSigningKeys = true,
            ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
        };
    }

    /// <summary>
    ///     Builds the fallback key from the statically configured secret. Issued with the
    ///     well-known key id and always accepted by validation, so deployments without a
    ///     rotation store (and tokens issued before rotation was wired) keep working.
    /// </summary>
    private SymmetricSecurityKey CreateStaticSigningKey()
        => new(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)) { KeyId = StaticSigningKeyId };

    /// <summary>
    ///     Resolves the credentials used to sign new tokens (issue #1141): the ACTIVE key
    ///     from the rotation store, surfaced through the JWT 'kid' header so validators
    ///     can pick the matching key. Falls back to the static secret when the store is
    ///     unavailable or holds no usable key — issuance never hard-fails on a
    ///     rotation-store outage.
    /// </summary>
    private async Task<SigningCredentials> ResolveSigningCredentialsAsync(CancellationToken cancellationToken)
    {
        if (keyRotationService is not null)
        {
            try
            {
                var activeKey = await keyRotationService.GetActiveSigningKeyAsync(cancellationToken).ConfigureAwait(false);
                var securityKey = JwtSigningKeyMapper.ToSecurityKey(activeKey);
                if (securityKey is not null)
                {
                    return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                }

                logger.LogWarning("No usable active signing key in the rotation store; signing with the static Jwt secret");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError("Signing-key rotation store unavailable; signing with the static Jwt secret: {ErrorType}", ex.GetType().FullName);
            }
        }

        return new SigningCredentials(CreateStaticSigningKey(), SecurityAlgorithms.HmacSha256);
    }

    private void AppendAdditionalClaims(List<Claim> destination, IEnumerable<Claim> additionalClaims)
    {
        foreach (var claim in additionalClaims)
        {
            if (claim is null || string.IsNullOrWhiteSpace(claim.Type) || IsReservedAdditionalClaim(claim.Type))
            {
                throw new ArgumentException(
                    "Additional claims must have a non-reserved claim type.", nameof(additionalClaims));
            }

            destination.Add(claim.Clone());
        }
    }

    private bool IsReservedAdditionalClaim(string claimType) =>
        ReservedAdditionalClaimTypes.Contains(claimType)
        || string.Equals(claimType, _authorizationTokenOptions.TenantClaimType, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimType, _authorizationTokenOptions.UserDefaultTenantClaimType, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimType, _authorizationTokenOptions.PermissionClaimType, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimType, _authorizationTokenOptions.RoleIdClaimType, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimType, _authorizationTokenOptions.GroupIdClaimType, StringComparison.OrdinalIgnoreCase);

    private static void EnsureExpectedSigningAlgorithm(SecurityToken securityToken)
    {
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
        {
            throw new SecurityTokenException("Invalid token signing algorithm.");
        }
    }
}
