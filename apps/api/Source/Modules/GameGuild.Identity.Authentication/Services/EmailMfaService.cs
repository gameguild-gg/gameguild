using System.Security.Cryptography;
using System.Text;
using GameGuild.CQRS;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Email OTP factor lifecycle (issue #1152). Mirrors <see cref="SmsMfaService"/> security
///     semantics: crypto-random six-digit codes, SHA-256 digest-only storage, constant-time
///     comparison (including a dummy comparison on the miss path so timings stay uniform),
///     single-use consumption, a five-attempt verify ceiling and a sixty-second resend throttle.
///     Codes are queued for delivery through the Notifications email channel to the account's
///     own address; the verified address is only persisted once the code is confirmed.
/// </summary>
public sealed class EmailMfaService(
    ILogger<EmailMfaService> logger,
    IMemoryCache memoryCache,
    IUserMfaConfigurationRepository mfaConfigRepository,
    IUserRepository userRepository,
    IPublisher publisher) : IEmailMfaService
{
    /// <summary>How long an issued code can be verified.</summary>
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Minimum interval between two code requests for the same user.</summary>
    public static readonly TimeSpan ResendThrottleWindow = TimeSpan.FromSeconds(60);

    /// <summary>Wrong-code attempts tolerated before the code is invalidated.</summary>
    public const int MaxVerificationAttempts = 5;

    private const string EntryKeyPrefix = "emailmfa:entry:";
    private const string ThrottleKeyPrefix = "emailmfa:throttle:";

    private static readonly byte[] DummyDigest = SHA256.HashData(Encoding.UTF8.GetBytes("email-mfa-code-timing-equalizer"));

    /// <inheritdoc />
    public async Task<EmailMfaChallengeResult> StartEnrollmentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return Failure("No email address is associated with this account.");
        }

        var normalized = NormalizeEmailAddress(user.Email);
        if (normalized is null)
        {
            return Failure("The account's email address is not a valid delivery destination.");
        }

        var code = await IssueCodeAsync(userId, normalized, isEnrollment: true, cancellationToken).ConfigureAwait(false);
        return code is null
            ? Failure("A verification code was sent recently. Wait before requesting another.")
            : new EmailMfaChallengeResult
            {
                Success = true,
                EmailMasked = MaskEmailAddress(normalized),
                ExpiresInSeconds = (int)CodeLifetime.TotalSeconds
            };
    }

    /// <inheritdoc />
    public async Task<MfaVerificationResult> CompleteEnrollmentAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var entry = ConsumeCode(userId, code);
        if (entry is null)
        {
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Email, "verify-failed");
            return new MfaVerificationResult { Success = false, Message = "Invalid or expired email code" };
        }

        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Email, "verify-failed");
            return new MfaVerificationResult { Success = false, Message = "No MFA configuration found for user" };
        }

        configuration.EmailMfaAddress = entry.Email;
        configuration.IsEnabled = true;
        configuration.IsSetupComplete = true;
        configuration.UpdatedAt = SystemClock.UtcNow;
        await mfaConfigRepository.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false);

        MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Email, "enrolled");
        logger.LogInformation("Email MFA enrolled for user {UserId}", LogRedaction.RedactId(userId, "uid"));
        return new MfaVerificationResult { Success = true, Message = "Email MFA enrolled successfully" };
    }

    /// <inheritdoc />
    public async Task<EmailMfaChallengeResult> SendChallengeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (configuration is not { IsEnabled: true } || string.IsNullOrWhiteSpace(configuration.EmailMfaAddress))
        {
            return Failure("Email MFA is not enrolled for this account.");
        }

        var emailAddress = configuration.EmailMfaAddress;
        var code = await IssueCodeAsync(userId, emailAddress, isEnrollment: false, cancellationToken).ConfigureAwait(false);
        return code is null
            ? Failure("A verification code was sent recently. Wait before requesting another.")
            : new EmailMfaChallengeResult
            {
                Success = true,
                EmailMasked = MaskEmailAddress(emailAddress),
                ExpiresInSeconds = (int)CodeLifetime.TotalSeconds
            };
    }

    /// <inheritdoc />
    public Task<bool> VerifyCodeAsync(Guid userId, string code) =>
        VerifyCodeAsync(userId, code, CancellationToken.None);

    /// <inheritdoc />
    public async Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (configuration is not { IsEnabled: true } || string.IsNullOrWhiteSpace(configuration.EmailMfaAddress))
        {
            return false;
        }

        return ConsumeCode(userId, code) is not null;
    }

    private async Task<string?> IssueCodeAsync(Guid userId, string email, bool isEnrollment, CancellationToken cancellationToken)
    {
        var throttleKey = ThrottleKeyPrefix + userId.ToString("N");
        if (memoryCache.TryGetValue(throttleKey, out DateTime lastIssued) &&
            SystemClock.UtcNow - lastIssued < ResendThrottleWindow)
        {
            logger.LogWarning(
                "Email MFA code request throttled for user {UserId}; a code was issued {Seconds:F0}s ago",
                LogRedaction.RedactId(userId, "uid"),
                (SystemClock.UtcNow - lastIssued).TotalSeconds);
            return null;
        }

        // Crypto-random six-digit code (uniform via the rejection-free Int32 overload).
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var entry = new EmailCodeEntry
        {
            Email = email,
            CodeDigest = SHA256.HashData(Encoding.UTF8.GetBytes(code)),
            ExpiresAt = SystemClock.UtcNow.Add(CodeLifetime)
        };

        memoryCache.Set(EntryKey(userId), entry, new MemoryCacheEntryOptions
        {
            // Cache eviction is only a safety net; the logical TTL lives in entry.ExpiresAt and is
            // enforced against the domain clock in ConsumeCode (see EmailCodeService for the
            // rationale against stamping domain-clock-derived absolute expirations here).
            AbsoluteExpirationRelativeToNow = CodeLifetime + EmailCodeService.EvictionSlack
        }.SetSize(1));

        memoryCache.Set(throttleKey, SystemClock.UtcNow, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ResendThrottleWindow
        }.SetSize(1));

        try
        {
            await publisher.Publish(new EmailMfaCodeRequestedNotification
            {
                UserId = userId,
                Email = email,
                Code = code,
                IsEnrollment = isEnrollment
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Delivery failure must not leave a live code the user never receives; drop it.
            memoryCache.Remove(EntryKey(userId));
            logger.LogError(exception, "Failed to dispatch email MFA code notification for user {UserId}", LogRedaction.RedactId(userId, "uid"));
            throw;
        }

        logger.LogInformation("Email MFA code generated for user {UserId} ({State})",
            LogRedaction.RedactId(userId, "uid"), isEnrollment ? "enrollment" : "challenge");
        return code;
    }

    /// <summary>Consumes a submitted code; returns the consumed entry (with destination email) on success, null otherwise.</summary>
    private EmailCodeEntry? ConsumeCode(Guid userId, string code)
    {
        var entryKey = EntryKey(userId);
        var suppliedDigest = string.IsNullOrWhiteSpace(code)
            ? SHA256.HashData(Encoding.UTF8.GetBytes(string.Empty))
            : SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));

        // Constant-time comparison on every path, including missing entries, so the digest match
        // itself never becomes a timing oracle.
        var entry = memoryCache.TryGetValue(entryKey, out EmailCodeEntry? stored) ? stored : null;
        var storedDigest = entry?.CodeDigest ?? DummyDigest;
        var matches = CryptographicOperations.FixedTimeEquals(storedDigest, suppliedDigest);

        if (entry is null)
        {
            logger.LogWarning("Email MFA verification attempted with no pending code for user {UserId}", LogRedaction.RedactId(userId, "uid"));
            return null;
        }

        if (entry.ExpiresAt < SystemClock.UtcNow)
        {
            memoryCache.Remove(entryKey);
            logger.LogWarning("Expired email MFA code used for user {UserId}", LogRedaction.RedactId(userId, "uid"));
            return null;
        }

        if (!matches)
        {
            entry.FailedAttempts++;
            if (entry.FailedAttempts >= MaxVerificationAttempts)
            {
                memoryCache.Remove(entryKey);
                logger.LogWarning(
                    "Email MFA code exhausted after {Attempts} failed attempts for user {UserId}",
                    entry.FailedAttempts,
                    LogRedaction.RedactId(userId, "uid"));
            }

            return null;
        }

        memoryCache.Remove(entryKey);
        logger.LogInformation("Email MFA code consumed successfully for user {UserId}", LogRedaction.RedactId(userId, "uid"));
        return entry;
    }

    private static string EntryKey(Guid userId) => EntryKeyPrefix + userId.ToString("N");

    /// <summary>Normalizes an email address: trimmed, exactly one '@', non-empty local part and domain.</summary>
    internal static string? NormalizeEmailAddress(string email)
    {
        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1 || trimmed.IndexOf('@', atIndex + 1) >= 0)
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    /// <summary>Masks the local part to its first character; the domain stays readable for confirmation.</summary>
    internal static string MaskEmailAddress(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex <= 0
            ? "***"
            : $"{email[..1]}***{email[atIndex..]}";
    }

    private static EmailMfaChallengeResult Failure(string message) =>
        new() { Success = false, EmailMasked = string.Empty, ExpiresInSeconds = 0, Message = message };

    private sealed class EmailCodeEntry
    {
        public string? Email { get; init; }

        public byte[] CodeDigest { get; init; } = [];

        public DateTime ExpiresAt { get; init; }

        public int FailedAttempts { get; set; }
    }
}
