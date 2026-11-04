using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Opens a delegated account recovery for an email address (issue #209). Only an
///     account with recovery factors (confirmed trusted contacts and/or an enrolled
///     passkey) gets a request; the response never reveals account existence or state.
///     Recovery codes are single-use and stored as digests. Approval-code and owner-notice
///     emails are queued through the durable notification outbox (issue #1145) so a
///     transient SMTP failure can no longer lose them.
/// </summary>
public sealed class RequestAccountRecoveryCommandHandler(
    IUserRepository userRepository,
    ITrustedContactStore contactStore,
    IAccountRecoveryStore recoveryStore,
    IWebAuthnCredentialRepository webAuthnCredentials,
    INotificationService notificationService,
    IMemoryCache memoryCache,
    IOptions<AccountRecoveryOptions> options,
    IAuthenticationAuditEventSink auditSink,
    ILogger<RequestAccountRecoveryCommandHandler> logger) : ICommandHandler<RequestAccountRecoveryCommand, AccountRecoveryRequestResult>
{
    private const string RateLimitKeyPrefix = "accountrecovery:ratelimit:";
    private readonly AccountRecoveryOptions options = options.Value;

    public async Task<AccountRecoveryRequestResult> Handle(RequestAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        // Cache key carries the full email digest so raw addresses never enter keys and
        // case variants share one bucket (mirrors EmailVerificationService).
        var rateLimitKey = RateLimitKeyPrefix + AccountRecoveryTokens.Hash(normalizedEmail);

        if (memoryCache.TryGetValue(rateLimitKey, out DateTime _))
        {
            logger.LogInformation("Account recovery request throttled for an email from rate limiting");
            return new AccountRecoveryRequestResult();
        }

        AccountRecoveryRequestResult ApplyRateLimit(AccountRecoveryRequestResult result)
        {
            // The host cache sets SizeLimit; every entry must carry a size (mirrors EmailVerificationService).
            memoryCache.Set(rateLimitKey, SystemClock.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.EmailRateLimit
            }.SetSize(1));
            return result;
        }

        await recoveryStore.ExpireElapsedAsync(SystemClock.UtcNow, cancellationToken).ConfigureAwait(false);

        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogInformation("Account recovery requested for unknown email");
            return ApplyRateLimit(new AccountRecoveryRequestResult());
        }

        var confirmedContacts = (await contactStore.GetActiveByOwnerAsync(user.Id, cancellationToken).ConfigureAwait(false))
            .Where(contact => contact.Status == TrustedContactStatus.Confirmed)
            .ToList();
        var hasPasskey = await webAuthnCredentials.HasActiveCredentialsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var availableFactors = confirmedContacts.Count + (hasPasskey ? 1 : 0);
        if (availableFactors == 0)
        {
            logger.LogInformation("Account recovery skipped for user {UserId}: no confirmed contacts and no enrolled passkey", user.Id);
            await AuditAsync("AccountRecovery.RequestRejected", user.Id, user.TenantId, success: false, request, cancellationToken).ConfigureAwait(false);
            return ApplyRateLimit(new AccountRecoveryRequestResult());
        }

        var liveRequest = await recoveryStore.FindLiveByOwnerAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (liveRequest is not null)
        {
            logger.LogInformation("Account recovery skipped for user {UserId}: a live request exists", user.Id);
            return ApplyRateLimit(new AccountRecoveryRequestResult());
        }

        var now = SystemClock.UtcNow;
        var recovery = new AccountRecoveryRequest
        {
            Id = Guid.NewGuid(),
            OwnerUserId = user.Id,
            TenantId = user.TenantId ?? Guid.Empty,
            Status = AccountRecoveryStatus.PendingApprovals,
            RequestedAt = now,
            CooldownEndsAt = now.Add(options.Cooldown),
            ApprovalWindowEndsAt = now.Add(options.ApprovalWindow),
            RequiredApprovals = options.EffectiveRequiredApprovals(availableFactors)
        };

        var approvals = new List<RecoveryApproval>(confirmedContacts.Count);
        var contactCodes = new List<(TrustedContact Contact, string Code)>(confirmedContacts.Count);
        foreach (var contact in confirmedContacts)
        {
            var code = AccountRecoveryTokens.Generate(AccountRecoveryTokens.ApprovalCodeBytes);
            contactCodes.Add((contact, code));
            approvals.Add(new RecoveryApproval
            {
                Id = Guid.NewGuid(),
                RecoveryRequestId = recovery.Id,
                OwnerUserId = user.Id,
                TenantId = recovery.TenantId,
                Factor = RecoveryApprovalFactor.TrustedContact,
                TrustedContactId = contact.Id,
                ApprovalCodeHash = AccountRecoveryTokens.Hash(code),
                IssuedAt = now
            });
        }

        try
        {
            await recoveryStore.AddWithApprovalsAsync(recovery, approvals, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex.Message.Contains("duplicate key") || ex.Message.Contains("unique constraint"))
        {
            logger.LogWarning("Account recovery race detected for user {UserId}. Treating as generic success.", user.Id);
            return ApplyRateLimit(new AccountRecoveryRequestResult());
        }

        // Emails are queued AFTER the request row exists (codes without a persisted request are
        // unusable) and through the durable outbox, so transient SMTP failures retry instead of
        // losing the codes (issue #1145). A failed durable enqueue aborts the command rather than
        // acknowledging a lost notification.
        foreach (var (contact, code) in contactCodes)
        {
            await QueueContactCodeEmailAsync(contact.Email, user, code, recovery, cancellationToken).ConfigureAwait(false);
        }

        await QueueOwnerNoticeEmailAsync(user, recovery, cancellationToken).ConfigureAwait(false);
        await AuditAsync("AccountRecovery.RequestOpened", user.Id, user.TenantId, success: true, request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Account recovery opened for user {UserId}: {Contacts} contacts, passkey {HasPasskey}, threshold {Threshold}",
            user.Id,
            confirmedContacts.Count,
            hasPasskey,
            recovery.RequiredApprovals);

        // Enumeration-safe: identical payload regardless of account existence, factor state, or a live request.
        return ApplyRateLimit(new AccountRecoveryRequestResult());
    }

    /// <summary>
    ///     Queues a single-use approval code for one trusted contact through the durable
    ///     notification pipeline (issue #1145).
    /// </summary>
    private async Task QueueContactCodeEmailAsync(string contactEmail, User owner, string code, AccountRecoveryRequest recovery, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            code,
            contactEmail,
            ownerEmail = owner.Email,
            windowStart = recovery.CooldownEndsAt.ToString("u"),
            windowEnd = recovery.ApprovalWindowEndsAt.ToString("u")
        });

        var result = await notificationService.SendAsync(
            null,
            NotificationType.TrustedContactRecoveryCode,
            "Account recovery approval code",
            "A recovery was requested for a GameGuild account that lists you as a trusted recovery contact.",
            NotificationChannel.Email,
            owner.TenantId,
            metadata: metadata,
            recipientEmail: contactEmail,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.IsFailure)
        {
            throw new InvalidOperationException("Trusted-contact recovery code was not durably queued.");
        }
    }

    /// <summary>
    ///     Queues the owner's recovery-requested notice through the durable notification
    ///     pipeline (issue #1145). Carries no bearer material, so its metadata is not
    ///     credential-protected.
    /// </summary>
    private async Task QueueOwnerNoticeEmailAsync(User owner, AccountRecoveryRequest recovery, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            ownerEmail = owner.Email,
            windowStart = recovery.CooldownEndsAt.ToString("u"),
            windowEnd = recovery.ApprovalWindowEndsAt.ToString("u")
        });

        var result = await notificationService.SendAsync(
            owner.Id,
            NotificationType.AccountRecoveryOwnerNotice,
            "Account recovery requested",
            "A delegated account recovery was requested for your account.",
            NotificationChannel.Email,
            owner.TenantId,
            metadata: metadata,
            recipientEmail: owner.Email,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.IsFailure)
        {
            throw new InvalidOperationException("Account recovery owner notice was not durably queued.");
        }
    }

    private Task AuditAsync(string actionType, Guid userId, Guid? tenantId, bool success, RequestAccountRecoveryCommand request, CancellationToken cancellationToken) =>
        auditSink.RecordAsync(new AuthenticationAuditEvent(
            actionType,
            userId,
            success,
            "AccountRecovery",
            request.IpAddress,
            request.UserAgent,
            TenantId: tenantId,
            AssessedRiskLevel: RiskLevel.High), cancellationToken);
}

/// <summary>
///     Cancels the acting owner's live recovery request. Cancelling a ResetUnlocked
///     request also kills the minted reset token (lookups require ResetUnlocked).
/// </summary>
public sealed class CancelAccountRecoveryCommandHandler(
    IAccountRecoveryStore recoveryStore,
    IAuthenticationAuditEventSink auditSink,
    ILogger<CancelAccountRecoveryCommandHandler> logger) : ICommandHandler<CancelAccountRecoveryCommand, AccountRecoveryCancelResult>
{
    public async Task<AccountRecoveryCancelResult> Handle(CancelAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var recovery = await recoveryStore.FindLiveByOwnerAsync(request.OwnerUserId, cancellationToken).ConfigureAwait(false);
        if (recovery is null)
        {
            return new AccountRecoveryCancelResult { Success = false, Message = "No active recovery request." };
        }

        var now = SystemClock.UtcNow;
        recovery.Status = AccountRecoveryStatus.Cancelled;
        recovery.CancelledAt = now;
        // Fail closed for the in-flight reset token: dropping the digest makes it unusable.
        if (recovery.ResetTokenHash is not null)
        {
            recovery.ResetTokenHash = null;
            recovery.ResetTokenExpiresAt = null;
        }

        await recoveryStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await auditSink.RecordAsync(new AuthenticationAuditEvent(
            "AccountRecovery.RequestCancelled",
            request.OwnerUserId,
            true,
            "AccountRecovery",
            request.IpAddress,
            request.UserAgent,
            TenantId: recovery.TenantId,
            AssessedRiskLevel: RiskLevel.Medium), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Account recovery {RecoveryId} cancelled by owner {OwnerUserId}", recovery.Id, request.OwnerUserId);

        return new AccountRecoveryCancelResult { Success = true, Message = "Recovery request cancelled." };
    }
}
