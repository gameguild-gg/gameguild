using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles verification/reset token generation, validation, and email dispatch coordination.
/// </summary>
public class EmailVerificationService(
    ILogger<EmailVerificationService> logger,
    IMemoryCache memoryCache,
    IPublisher publisher,
    IUserRepository? userRepository = null,
    IDistributedCache? distributedCache = null) : IEmailVerificationService
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    private const string TokenKeyPrefix = "emailverify:token:";
    private const string VerifiedKeyPrefix = "emailverify:verified:";
    private const string RateLimitKeyPrefix = "emailverify:ratelimit:";
    private const string EmailVerificationTokenType = "email_verification";
    private const string PasswordResetTokenType = "password_reset";
    private const string MagicLinkTokenType = "magic_link";

    public Task<string> GenerateVerificationTokenAsync(Guid userId, string email)
    {
        return GenerateTokenAsync(userId, email, EmailVerificationTokenType, TimeSpan.FromHours(24));
    }

    public Task<string> GeneratePasswordResetTokenAsync(Guid userId, string email)
    {
        return GenerateTokenAsync(userId, email, PasswordResetTokenType, TimeSpan.FromHours(1));
    }

    public Task<string> GenerateMagicLinkTokenAsync(Guid userId, string email)
    {
        return GenerateTokenAsync(userId, email, MagicLinkTokenType, TimeSpan.FromMinutes(15));
    }

    private async Task<string> GenerateTokenAsync(Guid userId, string email, string tokenType, TimeSpan lifetime)
    {
        try
        {
            var token = Guid.NewGuid().ToString("N");
            var tokenInfo = new TokenInfo
            {
                UserId = userId,
                Email = email.ToLowerInvariant(),
                Type = tokenType,
                ExpiresAt = SystemClock.UtcNow.Add(lifetime)
            };

            await StoreTokenInfoAsync(TokenKeyPrefix + token, tokenInfo).ConfigureAwait(false);

            logger.LogInformation("Generated {TokenType} token for user {UserId}", tokenType, userId);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating {TokenType} token for user {UserId}", tokenType, userId);
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

            logger.LogInformation("Verification email queued for {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification email to {Email}", email);
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

    public Task<TokenValidationResult> VerifyMagicLinkTokenAsync(string token)
    {
        return ValidateAndConsumeTokenAsync(
            token,
            MagicLinkTokenType,
            expectedUserId: null,
            markEmailVerified: false);
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
                logger.LogWarning("Empty {TokenType} token used", expectedType);
                return TokenValidationResult.Failed("Token is required");
            }

            var tokenKey = TokenKeyPrefix + token;
            var tokenInfo = await GetTokenInfoAsync(tokenKey).ConfigureAwait(false);
            if (tokenInfo == null)
            {
                logger.LogWarning("Invalid {TokenType} token used", expectedType);
                return TokenValidationResult.Failed("Invalid token");
            }

            if (expectedUserId.HasValue && tokenInfo.UserId != expectedUserId.Value)
            {
                logger.LogWarning(
                    "Token user ID mismatch. Expected {ExpectedUserId}, got {ActualUserId}",
                    tokenInfo.UserId,
                    expectedUserId);

                return TokenValidationResult.Failed("Token does not belong to the requested user");
            }

            if (tokenInfo.ExpiresAt < SystemClock.UtcNow)
            {
                await RemoveTokenInfoAsync(tokenKey).ConfigureAwait(false);
                logger.LogWarning("Expired {TokenType} token used for user {UserId}", expectedType, tokenInfo.UserId);
                return TokenValidationResult.Failed("Expired token");
            }

            if (tokenInfo.Type != expectedType)
            {
                logger.LogWarning(
                    "Invalid token type {ActualTokenType}; expected {ExpectedTokenType}",
                    tokenInfo.Type,
                    expectedType);

                return TokenValidationResult.Failed("Invalid token type");
            }

            if (markEmailVerified)
            {
                await StoreVerifiedMarkerAsync(tokenInfo.UserId).ConfigureAwait(false);
            }

            await RemoveTokenInfoAsync(tokenKey).ConfigureAwait(false);

            logger.LogInformation("{TokenType} token consumed successfully for user {UserId}", expectedType, tokenInfo.UserId);
            return new TokenValidationResult(true, tokenInfo.UserId, tokenInfo.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying {TokenType} token", expectedType);
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

            if (memoryCache.TryGetValue(VerifiedKeyPrefix + userId, out bool verified) && verified)
            {
                return true;
            }

            if (distributedCache is null)
            {
                return false;
            }

            var distributedValue = await distributedCache.GetAsync(VerifiedKeyPrefix + userId).ConfigureAwait(false);
            return distributedValue is [1];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking email verification status for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResendVerificationEmailAsync(Guid userId, string email)
    {
        try
        {
            var rateLimitKey = RateLimitKeyPrefix + $"{userId}:{email.ToLowerInvariant()}";

            if (memoryCache.TryGetValue(rateLimitKey, out DateTime lastSent))
            {
                var timeSinceLastSent = SystemClock.UtcNow - lastSent;

                if (timeSinceLastSent < TimeSpan.FromMinutes(2))
                {
                    logger.LogWarning(
                        "Rate limit exceeded for resending verification email to user {UserId}. Last sent {Seconds} seconds ago",
                        userId,
                        timeSinceLastSent.TotalSeconds);

                    return false;
                }
            }

            var token = await GenerateVerificationTokenAsync(userId, email).ConfigureAwait(false);
            await SendVerificationEmailAsync(email, token).ConfigureAwait(false);

            memoryCache.Set(rateLimitKey, SystemClock.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            }.SetSize(1));

            logger.LogInformation("Resent verification email to user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending verification email for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            var tokenInfo = await GetTokenInfoAsync(TokenKeyPrefix + token).ConfigureAwait(false);
            if (tokenInfo == null)
            {
                return false;
            }

            var isValid = tokenInfo.ExpiresAt >= SystemClock.UtcNow &&
                (tokenInfo.Type == EmailVerificationTokenType ||
                 tokenInfo.Type == PasswordResetTokenType ||
                 tokenInfo.Type == MagicLinkTokenType);

            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking token validity");
            return false;
        }
    }

    private async Task StoreTokenInfoAsync(string cacheKey, TokenInfo tokenInfo)
    {
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenInfo.ExpiresAt
        }.SetSize(1);

        memoryCache.Set(cacheKey, tokenInfo, memoryOptions);

        if (distributedCache is null)
        {
            return;
        }

        await distributedCache.SetAsync(
            cacheKey,
            JsonSerializer.SerializeToUtf8Bytes(tokenInfo, CacheJsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = tokenInfo.ExpiresAt
            }).ConfigureAwait(false);
    }

    private async Task<TokenInfo?> GetTokenInfoAsync(string cacheKey)
    {
        if (memoryCache.TryGetValue(cacheKey, out TokenInfo? tokenInfo) && tokenInfo is not null)
        {
            return tokenInfo;
        }

        if (distributedCache is null)
        {
            return null;
        }

        var bytes = await distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        tokenInfo = JsonSerializer.Deserialize<TokenInfo>(bytes, CacheJsonOptions);
        if (tokenInfo is null)
        {
            return null;
        }

        memoryCache.Set(cacheKey, tokenInfo, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenInfo.ExpiresAt
        }.SetSize(1));

        return tokenInfo;
    }

    private async Task RemoveTokenInfoAsync(string cacheKey)
    {
        memoryCache.Remove(cacheKey);

        if (distributedCache is not null)
        {
            await distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
        }
    }

    private async Task StoreVerifiedMarkerAsync(Guid userId)
    {
        var cacheKey = VerifiedKeyPrefix + userId;
        memoryCache.Set(cacheKey, true, new MemoryCacheEntryOptions().SetSize(1));

        if (distributedCache is not null)
        {
            await distributedCache.SetAsync(cacheKey, [1]).ConfigureAwait(false);
        }
    }
}
