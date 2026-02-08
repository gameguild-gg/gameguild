using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Implementation of email verification service that handles token generation, validation, and email sending coordination.
///     Uses IMemoryCache for token/verification storage. In production, replace with IDistributedCache (Redis).
/// </summary>
public class EmailVerificationService(ILogger<EmailVerificationService> logger, IConfiguration configuration, IMemoryCache memoryCache, IUserRepository? userRepository = null) : IEmailVerificationService
{
    private const string TokenKeyPrefix = "emailverify:token:";
    private const string VerifiedKeyPrefix = "emailverify:verified:";
    private const string RateLimitKeyPrefix = "emailverify:ratelimit:";

    public Task<string> GenerateVerificationTokenAsync(Guid userId, string email)
    {
        try
        {
            var token = Guid.NewGuid().ToString("N");
            var tokenInfo = new TokenInfo { UserId = userId, Email = email.ToLowerInvariant(), Type = "email_verification", ExpiresAt = DateTime.UtcNow.AddHours(24) };

            memoryCache.Set(TokenKeyPrefix + token, tokenInfo, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = tokenInfo.ExpiresAt
            });
            logger.LogInformation("Generated email verification token for user {UserId}", userId);

            return Task.FromResult(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating verification token for user {UserId}", userId);

            throw;
        }
    }

    public Task SendVerificationEmailAsync(string email, string token, string? userName = null)
    {
        try
        {
            // PLANNED: Integrate with IEmailService (SendGrid, SMTP, etc.) when Communication module
            // exposes email dispatch infrastructure. Currently logs the verification link.
            var verificationLink = $"{configuration["App:BaseUrl"]}/verify-email?token={token}";
            logger.LogInformation("Email verification link for {Email} (User: {UserName}): {VerificationLink}", email, userName ?? "Unknown", verificationLink);

            // PLANNED: Send actual email via IEmailService.SendAsync when available.
            // Example:
            // await _emailService.SendAsync(new EmailMessage
            // {
            //     To = email,
            //     Subject = "Verify Your Email Address",
            //     Body = $"Hi {userName},\n\nPlease verify your email by clicking: {verificationLink}"
            // });

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification email to {Email}", email);

            throw;
        }
    }

    public Task<bool> VerifyEmailTokenAsync(Guid userId, string token)
    {
        try
        {
            if (!memoryCache.TryGetValue(TokenKeyPrefix + token, out TokenInfo? tokenInfo) || tokenInfo == null)
            {
                logger.LogWarning("Invalid verification token used for user {UserId}", userId);

                return Task.FromResult(false);
            }

            if (tokenInfo.UserId != userId)
            {
                logger.LogWarning("Token user ID mismatch. Expected {ExpectedUserId}, got {ActualUserId}", tokenInfo.UserId, userId);

                return Task.FromResult(false);
            }

            if (tokenInfo.ExpiresAt < DateTime.UtcNow)
            {
                memoryCache.Remove(TokenKeyPrefix + token);
                logger.LogWarning("Expired verification token used for user {UserId}", userId);

                return Task.FromResult(false);
            }

            if (tokenInfo.Type != "email_verification")
            {
                logger.LogWarning("Invalid token type {TokenType} for email verification", tokenInfo.Type);

                return Task.FromResult(false);
            }

            // Mark email as verified (no expiration — stays until app restarts or cache eviction)
            memoryCache.Set(VerifiedKeyPrefix + userId, true);

            // Remove used token
            memoryCache.Remove(TokenKeyPrefix + token);

            logger.LogInformation("Email verified successfully for user {UserId}", userId);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email token for user {UserId}", userId);

            return Task.FromResult(false);
        }
    }

    public async Task<bool> IsEmailVerifiedAsync(Guid userId)
    {
        try
        {
            // Check database for actual email verification status via IUserRepository
            if (userRepository is not null)
            {
                var user = await userRepository.GetByIdAsync(userId).ConfigureAwait(false);
                if (user is not null)
                {
                    return user.IsEmailVerified;
                }
            }

            // Fallback to in-memory cache when IUserRepository is unavailable or user not found
            var isVerified = memoryCache.TryGetValue(VerifiedKeyPrefix + userId, out bool verified) && verified;

            return isVerified;
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

            // Check rate limiting (1 email per 2 minutes)
            if (memoryCache.TryGetValue(rateLimitKey, out DateTime lastSent))
            {
                var timeSinceLastSent = DateTime.UtcNow - lastSent;

                if (timeSinceLastSent < TimeSpan.FromMinutes(2))
                {
                    logger.LogWarning("Rate limit exceeded for resending verification email to user {UserId}. Last sent {Seconds} seconds ago", userId, timeSinceLastSent.TotalSeconds);

                    return false;
                }
            }

            // Generate new token
            var token = await GenerateVerificationTokenAsync(userId, email).ConfigureAwait(false);

            // Send email
            await SendVerificationEmailAsync(email, token).ConfigureAwait(false);

            // Update rate limit with sliding expiration
            memoryCache.Set(rateLimitKey, DateTime.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            });

            logger.LogInformation("Resent verification email to user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending verification email for user {UserId}", userId);

            return false;
        }
    }

    public Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            if (!memoryCache.TryGetValue(TokenKeyPrefix + token, out TokenInfo? tokenInfo) || tokenInfo == null) { return Task.FromResult(false); }

            var isValid = tokenInfo.ExpiresAt >= DateTime.UtcNow && tokenInfo.Type == "email_verification";

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking token validity");

            return Task.FromResult(false);
        }
    }
}
