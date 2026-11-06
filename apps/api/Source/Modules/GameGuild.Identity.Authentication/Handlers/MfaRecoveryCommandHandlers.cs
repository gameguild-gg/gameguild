using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>Shared token utilities for MFA recovery: opaque generation, SHA-256 hex digests, constant-time comparison.</summary>
internal static class MfaRecoveryTokens
{
    internal const int RequestTokenBytes = 32;
    internal const int ConfirmationTokenBytes = 32;
    internal static readonly TimeSpan Cooldown = TimeSpan.FromDays(7);
    internal static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromHours(1);
    internal static readonly TimeSpan RequestRetention = TimeSpan.FromDays(30);
    internal static readonly TimeSpan EmailRateLimit = TimeSpan.FromMinutes(10);

    internal static string Generate(int byteCount) =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(byteCount)).ToLowerInvariant();

    internal static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Constant-time comparison of two hex digests (see BackupCodeMfaService).</summary>
    internal static bool FixedTimeHexEquals(string left, string right)
    {
        if (left is not { Length: 64 } || right is not { Length: 64 }) { return false; }
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));
    }
}

/// <summary>
///     Opens a lost-MFA-device recovery for an email address. The response never
///     reveals whether the account exists or has MFA enabled.
/// </summary>
public sealed class RequestMfaRecoveryCommandHandler(
    IUserRepository userRepository,
    IMfaAttemptTrackingService mfaTrackingService,
    IMfaRecoveryRequestStore recoveryStore,
    INotificationService notificationService,
    IMemoryCache memoryCache,
    ILogger<RequestMfaRecoveryCommandHandler> logger,
    IAuthenticationAuditEventSink auditSink) : ICommandHandler<RequestMfaRecoveryCommand, MfaRecoveryRequestResult>
{
    private const string RateLimitKeyPrefix = "mfarecovery:ratelimit:";

    public async Task<MfaRecoveryRequestResult> Handle(RequestMfaRecoveryCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        // Cache key carries the full email digest so raw addresses never enter keys and
        // case variants share one bucket (mirrors EmailVerificationService).
        var rateLimitKey = RateLimitKeyPrefix + MfaRecoveryTokens.Hash(normalizedEmail);

        if (memoryCache.TryGetValue(rateLimitKey, out DateTime _))
        {
            logger.LogInformation("MFA recovery request throttled for an email from rate limiting");
            return new MfaRecoveryRequestResult();
        }

        MfaRecoveryRequestResult ApplyRateLimit(MfaRecoveryRequestResult result)
        {
            // The host cache sets SizeLimit; every entry must carry a size (mirrors EmailVerificationService).
            memoryCache.Set(rateLimitKey, SystemClock.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = MfaRecoveryTokens.EmailRateLimit
            }.SetSize(1));
            return result;
        }

        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogInformation("MFA recovery requested for unknown email");
            return ApplyRateLimit(new MfaRecoveryRequestResult());
        }

        var mfaEnabled = await mfaTrackingService.GetMfaStatusAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var liveRequest = await recoveryStore.FindLiveByUserAsync(user.Id.ToString(), cancellationToken).ConfigureAwait(false);

        if (mfaEnabled && liveRequest is null)
        {
            var now = SystemClock.UtcNow;
            var requestToken = MfaRecoveryTokens.Generate(MfaRecoveryTokens.RequestTokenBytes);
            var recovery = new MfaRecoveryRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id.ToString(),
                TenantId = (user.TenantId ?? Guid.Empty).ToString(),
                RequestedAt = now,
                CooldownEndsAt = now.Add(MfaRecoveryTokens.Cooldown),
                Status = MfaRecoveryRequestStatus.Pending,
                RequestTokenHash = MfaRecoveryTokens.Hash(requestToken)
            };

            try 
            {
                await recoveryStore.AddAsync(recovery, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate key") || ex.Message.Contains("unique constraint"))
            {
                logger.LogWarning("MFA recovery request race detected for user {UserId}. Treating as success.", user.Id);
                return ApplyRateLimit(new MfaRecoveryRequestResult());
            }

            await QueueRecoveryStartedEmailAsync(user, requestToken, recovery.CooldownEndsAt, cancellationToken).ConfigureAwait(false);
            await MfaRecoveryAudit.RecordAsync(auditSink, recovery, MfaRecoveryAudit.Requested, "RecoveryStage", cancellationToken).ConfigureAwait(false);
            logger.LogInformation("MFA recovery request opened for user {UserId}", user.Id);
        }
        else
        {
            logger.LogInformation("MFA recovery request skipped for user {UserId}: MFA not enabled or a live request exists", user.Id);
        }

        // Enumeration-safe: identical payload regardless of account existence, MFA state, or a live request.
        return ApplyRateLimit(new MfaRecoveryRequestResult());
    }

    /// <summary>
    ///     Queues the recovery-started email through the durable notification pipeline (outbox row,
    ///     retried by the email dispatcher) instead of a direct IEmailSender call whose transient
    ///     SMTP failure would silently lose the email (issue #1155).
    /// </summary>
    private async Task QueueRecoveryStartedEmailAsync(User user, string requestToken, DateTimeOffset cooldownEndsAt, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            token = requestToken,
            email = user.Email,
            userName = user.Username,
            cooldownEndsAt
        });

        var result = await notificationService.SendAsync(
            user.Id,
            NotificationType.MfaRecovery,
            "Multi-factor authentication recovery requested",
            "A request to reset the multi-factor authentication on your account was received.",
            NotificationChannel.Email,
            user.TenantId,
            metadata: metadata,
            recipientEmail: user.Email,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.IsFailure)
        {
            throw new InvalidOperationException("MFA recovery notification was not durably queued.");
        }
    }
}

/// <summary>
///     Second stage: after the cooling-off window, exchanges the request token for a
///     confirmation email. Denials never distinguish unknown, expired, or completed tokens.
/// </summary>
public sealed class ContinueMfaRecoveryCommandHandler(
    IMfaRecoveryRequestStore recoveryStore,
    IUserRepository userRepository,
    INotificationService notificationService,
    ILogger<ContinueMfaRecoveryCommandHandler> logger,
    IAuthenticationAuditEventSink auditSink) : ICommandHandler<ContinueMfaRecoveryCommand, MfaRecoveryContinueResult>
{
    private static readonly MfaRecoveryContinueResult Denial = new()
    {
        Success = false,
        Message = "Invalid or expired recovery request."
    };

    public async Task<MfaRecoveryContinueResult> Handle(ContinueMfaRecoveryCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await recoveryStore.ExpireOlderThanAsync(SystemClock.UtcNow.Subtract(MfaRecoveryTokens.RequestRetention), cancellationToken).ConfigureAwait(false);

        var tokenHash = MfaRecoveryTokens.Hash(request.RequestToken);
        var recovery = await recoveryStore.FindLiveByRequestTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (recovery is null)
        {
            logger.LogInformation("MFA recovery continue denied: no live request for the presented token");
            return Denial;
        }

        if (!MfaRecoveryTokens.FixedTimeHexEquals(recovery.RequestTokenHash!, tokenHash))
        {
            return Denial;
        }

        var now = SystemClock.UtcNow;
        if (now < recovery.CooldownEndsAt)
        {
            // The caller holds a valid request token; revealing the remaining wait is safe and required.
            return new MfaRecoveryContinueResult
            {
                Success = false,
                Message = "The recovery cooling-off period has not elapsed yet.",
                RetryAfterSeconds = (int)Math.Ceiling((recovery.CooldownEndsAt - now).TotalSeconds)
            };
        }

        var user = await userRepository.GetByIdAsync(Guid.Parse(recovery.UserId), cancellationToken).ConfigureAwait(false);
        if (user is null) { return Denial; }

        // Compare the live state and original bearer again in the database before issuing another bearer.
        var confirmationToken = MfaRecoveryTokens.Generate(MfaRecoveryTokens.ConfirmationTokenBytes);
        if (!await recoveryStore.TryIssueConfirmationAsync(recovery.Id, tokenHash, MfaRecoveryTokens.Hash(confirmationToken),
                now, now.Add(MfaRecoveryTokens.ConfirmationLifetime), cancellationToken).ConfigureAwait(false))
        {
            return Denial;
        }

        await QueueConfirmationEmailAsync(user, confirmationToken, cancellationToken).ConfigureAwait(false);
        await MfaRecoveryAudit.RecordAsync(auditSink, recovery, MfaRecoveryAudit.Continued, "RecoveryStage", cancellationToken).ConfigureAwait(false);
        logger.LogInformation("MFA recovery confirmation issued for request {RequestId}", recovery.Id);

        return new MfaRecoveryContinueResult
        {
            Success = true,
            Message = "A confirmation link has been sent to the account's email address."
        };
    }

    /// <summary>
    ///     Queues the confirmation email through the durable notification pipeline (outbox row,
    ///     retried by the email dispatcher) instead of a direct IEmailSender call (issue #1155).
    /// </summary>
    private async Task QueueConfirmationEmailAsync(User user, string confirmationToken, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            token = confirmationToken,
            email = user.Email,
            userName = user.Username
        });

        var result = await notificationService.SendAsync(
            user.Id,
            NotificationType.MfaRecoveryConfirmation,
            "Confirm multi-factor authentication recovery",
            "Confirm and disable multi-factor authentication for your account.",
            NotificationChannel.Email,
            user.TenantId,
            metadata: metadata,
            recipientEmail: user.Email,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.IsFailure)
        {
            throw new InvalidOperationException("MFA recovery notification was not durably queued.");
        }
    }
}

/// <summary>
///     Final stage: consumes the confirmation token, marks the request Completed, and
///     disables MFA for the recovered account (revoking tokens and sessions per the
///     shared disable path). Denials never distinguish token states.
/// </summary>
public sealed class ConfirmMfaRecoveryCommandHandler(
    IMfaRecoveryRequestStore recoveryStore,
    IMfaAttemptTrackingService mfaTrackingService,
    ILogger<ConfirmMfaRecoveryCommandHandler> logger,
    IAuthenticationAuditEventSink auditSink) : ICommandHandler<ConfirmMfaRecoveryCommand, MfaRecoveryConfirmResult>
{
    private static readonly MfaRecoveryConfirmResult Denial = new()
    {
        Success = false,
        Message = "Invalid or expired recovery confirmation."
    };

    public async Task<MfaRecoveryConfirmResult> Handle(ConfirmMfaRecoveryCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenHash = MfaRecoveryTokens.Hash(request.ConfirmationToken);
        var recovery = await recoveryStore.FindAwaitingByConfirmationTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (recovery is null)
        {
            logger.LogInformation("MFA recovery confirm denied: no awaiting request for the presented token");
            return Denial;
        }

        if (!MfaRecoveryTokens.FixedTimeHexEquals(recovery.ConfirmationTokenHash!, tokenHash))
        {
            return Denial;
        }

        var now = SystemClock.UtcNow;
        if (recovery.ConfirmationTokenExpiresAt is not { } expiresAt || expiresAt < now)
        {
            return Denial;
        }

        // The conditional update owns the one-time transition until this command commits.
        // A cancellation that committed first leaves this update at zero rows.
        if (!await recoveryStore.TryConfirmAsync(recovery.Id, tokenHash, now, cancellationToken).ConfigureAwait(false))
        {
            return Denial;
        }

        var userId = Guid.Parse(recovery.UserId);
        // Required audit persists in this transaction before credentials are reset.
        await MfaRecoveryAudit.RecordAsync(auditSink, recovery, MfaRecoveryAudit.Confirmed, "RecoveryStage", cancellationToken).ConfigureAwait(false);
        var disabled = await mfaTrackingService.DisableMfaAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!disabled) { throw new InvalidOperationException("MFA recovery could not complete the required factor reset."); }
        logger.LogInformation("MFA recovery completed for request {RequestId}; MFA disabled: {Disabled}", recovery.Id, disabled);

        return new MfaRecoveryConfirmResult
        {
            Success = true,
            Message = "Multi-factor authentication recovery completed. You can sign in and set up MFA again."
        };
    }
}
