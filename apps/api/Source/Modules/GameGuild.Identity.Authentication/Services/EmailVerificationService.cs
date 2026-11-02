using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles verification/reset token generation, validation, and email dispatch coordination.
///     Verification and password-reset tokens live in <see cref="IDistributedCache"/> (Redis in
///     production, the host's distributed-memory cache in single-node/dev) so a restart or a
///     load-balanced request landing on another node never strands a pending token — the same
///     durability posture as the database-backed magic-link store. The abstraction has no atomic
///     compare-and-delete, so one-time consumption uses a claim protocol: a consumed sentinel is
///     written before removal, making a token read as unavailable to every reader the moment it
///     is claimed; simultaneous cross-instance presentations are bounded by that protocol rather
///     than an interlocked in-process flag.
/// </summary>
public class EmailVerificationService(
    ILogger<EmailVerificationService> logger,
    IDistributedCache cache,
    IPublisher publisher,
    IUserRepository? userRepository = null,
    IMagicLinkTokenStore? magicLinkTokenStore = null) : IEmailVerificationService
{
    private const string TokenKeyPrefix = "emailverify:token:";
    private const string VerifiedKeyPrefix = "emailverify:verified:";
    private const string RateLimitKeyPrefix = "emailverify:ratelimit:";
    private const string EmailVerificationTokenType = "email_verification";
    private const string PasswordResetTokenType = "password_reset";
    private const string MagicLinkTokenType = "magic_link";

    /// <summary>
    ///     Sentinel written under the claim lock before the entry is removed. It is deliberately
    ///     not valid <see cref="TokenInfo"/> JSON, so every reader (validity checks included)
    ///     treats a claimed token as unavailable even while its removal is still in flight.
    /// </summary>
    private const string ConsumedMarker = "emailverify:consumed";

    /// <summary>Claim lock striping: a fixed pool per token-key digest, shared by every service
    ///     instance in the process so concurrent consumers of one token serialize their claim.</summary>
    private const int ClaimLockCount = 256;

    private static readonly TimeSpan ResendRateLimitWindow = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim[] ClaimLocks = Enumerable
        .Range(0, ClaimLockCount)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    public Task<string> GenerateVerificationTokenAsync(Guid userId, string email)
    {
        return GenerateTokenAsync(userId, email, EmailVerificationTokenType, TimeSpan.FromHours(24));
    }

    public Task<string> GeneratePasswordResetTokenAsync(Guid userId, string email)
    {
        return GenerateTokenAsync(userId, email, PasswordResetTokenType, TimeSpan.FromHours(1));
    }

    public async Task<string> GenerateMagicLinkTokenAsync(Guid userId, string email)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var expiresAt = SystemClock.UtcNow.AddMinutes(15);
        await (magicLinkTokenStore ?? throw new InvalidOperationException("Magic-link token storage is not configured."))
            .AddAsync(token, userId, email, expiresAt).ConfigureAwait(false);
        logger.LogInformation("Generated {TokenType} token for user {UserId}", MagicLinkTokenType, LogRedaction.RedactId(userId, "uid"));
        return token;
    }

    private async Task<string> GenerateTokenAsync(Guid userId, string email, string tokenType, TimeSpan lifetime)
    {
        try
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var tokenInfo = new TokenInfo
            {
                UserId = userId,
                Email = email.ToLowerInvariant(),
                Type = tokenType,
                ExpiresAt = SystemClock.UtcNow.Add(lifetime)
            };

            // The cache entry expires with the token, so an unconsumed token never outlives its TTL.
            await cache.SetStringAsync(
                GetTokenCacheKey(token),
                JsonSerializer.Serialize(tokenInfo, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
                CancellationToken.None).ConfigureAwait(false);

            // codeql[cs/cleartext-storage-of-sensitive-information] tokenType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
            // codeql[cs/exposure-of-sensitive-information] tokenType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
            logger.LogInformation("Generated {TokenType} token for user {UserId}", tokenType, LogRedaction.RedactId(userId, "uid"));
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError("Error generating {TokenType} token for user {UserId}: {ErrorType}",
                // codeql[cs/cleartext-storage-of-sensitive-information] tokenType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                // codeql[cs/exposure-of-sensitive-information] tokenType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                tokenType, LogRedaction.RedactId(userId, "uid"), ex.GetType().FullName);
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(string email, string token, string? userName = null)
    {
        try
        {
            await publisher.Publish(
                new EmailVerificationRequestedNotification
                {
                    Email = email,
                    Token = token,
                    UserName = userName
                }).ConfigureAwait(false);

            // codeql[cs/exposure-of-sensitive-information] address is redacted by LogRedaction.MaskEmail before entering the structured log
            logger.LogInformation("Verification email queued for {Email}", LogRedaction.MaskEmail(email));
        }
        catch (Exception ex)
        {
            logger.LogError("Error sending verification email to {Email}: {ErrorType}",
                // codeql[cs/exposure-of-sensitive-information] address is redacted by LogRedaction.MaskEmail before entering the structured log
                LogRedaction.MaskEmail(email), ex.GetType().FullName);
            throw;
        }
    }

    public async Task<bool> VerifyEmailTokenAsync(Guid userId, string token)
    {
        var result = await ValidateAndConsumeTokenAsync(
            token,
            EmailVerificationTokenType,
            userId,
            markEmailVerified: true).ConfigureAwait(false);

        return result.Success;
    }

    public Task<TokenValidationResult> VerifyEmailTokenAsync(string token)
    {
        return ValidateAndConsumeTokenAsync(
            token,
            EmailVerificationTokenType,
            expectedUserId: null,
            markEmailVerified: true);
    }

    public Task<TokenValidationResult> VerifyPasswordResetTokenAsync(string token)
    {
        return ValidateAndConsumeTokenAsync(
            token,
            PasswordResetTokenType,
            expectedUserId: null,
            markEmailVerified: false);
    }

    public async Task<TokenValidationResult> VerifyMagicLinkTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Empty {TokenType} token used", MagicLinkTokenType);
            return TokenValidationResult.Failed("Token is required");
        }

        try
        {
            var result = await (magicLinkTokenStore ?? throw new InvalidOperationException("Magic-link token storage is not configured."))
                .ConsumeAsync(token, SystemClock.UtcNow).ConfigureAwait(false);
            if (result.Success)
            {
                logger.LogInformation("{TokenType} token consumed successfully for user {UserId}", MagicLinkTokenType, LogRedaction.RedactId(result.UserId, "uid"));
            }
            else
            {
                logger.LogWarning("Invalid or expired {TokenType} token used", MagicLinkTokenType);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError("Error verifying {TokenType} token: {ErrorType}", MagicLinkTokenType, ex.GetType().FullName);
            return TokenValidationResult.Failed("Token verification failed");
        }
    }

    private async Task<TokenValidationResult> ValidateAndConsumeTokenAsync(
        string token,
        string expectedType,
        Guid? expectedUserId,
        bool markEmailVerified)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                logger.LogWarning("Empty {TokenType} token used", expectedType);
                return TokenValidationResult.Failed("Token is required");
            }

            var tokenKey = GetTokenCacheKey(token);
            var tokenInfo = await GetTokenInfoAsync(tokenKey).ConfigureAwait(false);
            if (tokenInfo is null)
            {
                // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                logger.LogWarning("Invalid {TokenType} token used", expectedType);
                return TokenValidationResult.Failed("Invalid token");
            }

            if (expectedUserId.HasValue && tokenInfo.UserId != expectedUserId.Value)
            {
                logger.LogWarning(
                    "Token user ID mismatch. Expected {ExpectedUserId}, got {ActualUserId}",
                    LogRedaction.RedactId(expectedUserId, "uid"),
                    LogRedaction.RedactId(tokenInfo.UserId, "uid"));

                return TokenValidationResult.Failed("Token does not belong to the requested user");
            }

            if (tokenInfo.ExpiresAt < SystemClock.UtcNow)
            {
                await cache.RemoveAsync(tokenKey, CancellationToken.None).ConfigureAwait(false);
                // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                logger.LogWarning("Expired {TokenType} token used for user {UserId}", expectedType, LogRedaction.RedactId(tokenInfo.UserId, "uid"));
                return TokenValidationResult.Failed("Expired token");
            }

            if (tokenInfo.Type != expectedType)
            {
                logger.LogWarning(
                    "Invalid token type {ActualTokenType}; expected {ExpectedTokenType}",
                    tokenInfo.Type,
                    // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                    // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                    expectedType);

                return TokenValidationResult.Failed("Invalid token type");
            }

            // One-time claim. The striped lock serializes claimants in this process (all scopes,
            // all service instances); under the lock the entry is re-read, a non-JSON consumed
            // sentinel is written so the token reads as unavailable immediately, and only then is
            // the entry removed. Cross-instance claimants race only on the cache itself —
            // IDistributedCache has no atomic compare-and-delete, which the interface documents
            // as the protocol bound (the same bound the SAML pending-request store accepts).
            var claimLock = ClaimLocks[(int)((uint)tokenKey.GetHashCode(StringComparison.Ordinal) % ClaimLockCount)];
            await claimLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                // Re-read under the lock: a concurrent claimant (any scope in this process)
                // either already wrote the sentinel or already removed the entry.
                var current = await cache.GetStringAsync(tokenKey, CancellationToken.None).ConfigureAwait(false);
                if (string.IsNullOrEmpty(current) || string.Equals(current, ConsumedMarker, StringComparison.Ordinal))
                {
                    // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
                    // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
                    logger.LogWarning("Already consumed {TokenType} token used", expectedType);
                    return TokenValidationResult.Failed("Invalid token");
                }

                await cache.SetStringAsync(tokenKey, ConsumedMarker, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                claimLock.Release();
            }

            await cache.RemoveAsync(tokenKey, CancellationToken.None).ConfigureAwait(false);

            if (markEmailVerified)
            {
                // No expiration: the verified marker outlives the token, mirroring the previous
                // non-expiring in-process entry.
                await cache.SetStringAsync(VerifiedKeyPrefix + tokenInfo.UserId, "true", CancellationToken.None).ConfigureAwait(false);
            }

            // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
            // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
            logger.LogInformation("{TokenType} token consumed successfully for user {UserId}", expectedType, LogRedaction.RedactId(tokenInfo.UserId, "uid"));
            return new TokenValidationResult(true, tokenInfo.UserId, tokenInfo.Email);
        }
        catch (Exception ex)
        {
            // codeql[cs/cleartext-storage-of-sensitive-information] expectedType is the fixed "password_reset"/"email_verification" discriminator constant, not a credential
            // codeql[cs/exposure-of-sensitive-information] expectedType is the fixed "email_verification"/"password_reset" discriminator constant, not personal data
            logger.LogError("Error verifying {TokenType} token: {ErrorType}", expectedType, ex.GetType().FullName);
            return TokenValidationResult.Failed("Token verification failed");
        }
    }

    public async Task<bool> IsEmailVerifiedAsync(Guid userId)
    {
        try
        {
            if (userRepository is not null)
            {
                var user = await userRepository.GetByIdAsync(userId).ConfigureAwait(false);
                if (user is not null)
                {
                    return user.IsEmailVerified;
                }
            }

            return string.Equals(
                await cache.GetStringAsync(VerifiedKeyPrefix + userId, CancellationToken.None).ConfigureAwait(false),
                "true",
                StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking email verification status for user {UserId}: {ErrorType}",
                LogRedaction.RedactId(userId, "uid"), ex.GetType().FullName);
            return false;
        }
    }

    public async Task<bool> ResendVerificationEmailAsync(Guid userId, string email)
    {
        try
        {
            // Cache identity needs a distinct key per address; log redaction deliberately emits a constant marker.
            // Normalize before hashing so case variants of the same address share one rate-limit bucket,
            // and use the complete digest to avoid truncation collisions and keep raw emails out of cache keys.
            var emailDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())));
            var rateLimitKey = RateLimitKeyPrefix + $"{userId}:{emailDigest}";

            var lastSentRaw = await cache.GetStringAsync(rateLimitKey, CancellationToken.None).ConfigureAwait(false);
            if (DateTime.TryParse(lastSentRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastSent))
            {
                var timeSinceLastSent = SystemClock.UtcNow - lastSent;

                if (timeSinceLastSent < ResendRateLimitWindow)
                {
                    logger.LogWarning(
                        "Rate limit exceeded for resending verification email to user {UserId}. Last sent {Seconds} seconds ago",
                        LogRedaction.RedactId(userId, "uid"),
                        timeSinceLastSent.TotalSeconds);

                    return false;
                }
            }

            var token = await GenerateVerificationTokenAsync(userId, email).ConfigureAwait(false);
            await SendVerificationEmailAsync(email, token).ConfigureAwait(false);

            await cache.SetStringAsync(
                rateLimitKey,
                SystemClock.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ResendRateLimitWindow },
                CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation("Resent verification email to user {UserId}", LogRedaction.RedactId(userId, "uid"));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Error resending verification email for user {UserId}: {ErrorType}",
                LogRedaction.RedactId(userId, "uid"), ex.GetType().FullName);
            return false;
        }
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (magicLinkTokenStore is not null &&
                await magicLinkTokenStore.IsValidAsync(token, SystemClock.UtcNow).ConfigureAwait(false))
            {
                return true;
            }

            var tokenInfo = await GetTokenInfoAsync(GetTokenCacheKey(token)).ConfigureAwait(false);
            if (tokenInfo is null)
            {
                return false;
            }

            return tokenInfo.ExpiresAt >= SystemClock.UtcNow &&
                (tokenInfo.Type == EmailVerificationTokenType || tokenInfo.Type == PasswordResetTokenType);
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking token validity: {ErrorType}", ex.GetType().FullName);
            return false;
        }
    }

    /// <summary>
    ///     Reads a token entry. A missing entry, the consumed sentinel, or an unparsable payload
    ///     (which the claim protocol can write transiently) all read as "no token available".
    /// </summary>
    private async Task<TokenInfo?> GetTokenInfoAsync(string tokenKey)
    {
        var payload = await cache.GetStringAsync(tokenKey, CancellationToken.None).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TokenInfo>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            // The consumed sentinel (or a torn write) is not TokenInfo JSON by design.
            return null;
        }
    }

    private static string GetTokenCacheKey(string token) =>
        TokenKeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
