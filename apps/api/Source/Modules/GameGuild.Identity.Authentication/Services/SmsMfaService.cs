using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text;
using GameGuild.CQRS;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     SMS OTP factor lifecycle (issue #244). Mirrors <see cref="EmailCodeService"/> security
///     semantics: crypto-random six-digit codes, SHA-256 digest-only storage, constant-time
///     comparison (including a dummy comparison on the miss path so timings stay uniform),
///     single-use consumption, a five-attempt verify ceiling and a sixty-second resend throttle.
///     Codes are queued for delivery through the Notifications SMS channel.
/// </summary>
public sealed class SmsMfaService(
    ILogger<SmsMfaService> logger,
    IMemoryCache memoryCache,
    IUserMfaConfigurationRepository mfaConfigRepository,
    IPublisher publisher,
    IMfaAttemptTrackingService attemptTrackingService) : ISmsMfaService
{
    /// <summary>How long an issued code can be verified.</summary>
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Minimum interval between two code requests for the same user.</summary>
    public static readonly TimeSpan ResendThrottleWindow = TimeSpan.FromSeconds(60);

    /// <summary>Wrong-code attempts tolerated before the code is invalidated.</summary>
    public const int MaxVerificationAttempts = 5;

    private static readonly ConditionalWeakTable<IMemoryCache, object> CacheGates = new();
    private readonly object _cacheGate = CacheGates.GetValue(memoryCache, static _ => new object());

    private const string EntryKeyPrefix = "smsmfa:entry:";
    private const string ThrottleKeyPrefix = "smsmfa:throttle:";

    private static readonly byte[] DummyDigest = SHA256.HashData(Encoding.UTF8.GetBytes("sms-mfa-code-timing-equalizer"));

    /// <inheritdoc />
    public async Task<SmsMfaChallengeResult> StartEnrollmentAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        var normalized = NormalizePhoneNumber(phoneNumber);
        if (normalized is null)
        {
            return Failure("Phone number must be in international E.164 format (for example +15551234567).");
        }

        var code = await IssueCodeAsync(userId, normalized, true, null, cancellationToken).ConfigureAwait(false);
        return code is null
            ? Failure("A verification code was sent recently. Wait before requesting another.")
            : new SmsMfaChallengeResult
            {
                Success = true,
                PhoneNumberMasked = MaskPhoneNumber(normalized),
                ExpiresInSeconds = (int)CodeLifetime.TotalSeconds
            };
    }

    /// <inheritdoc />
    public async Task<MfaVerificationResult> CompleteEnrollmentAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = ConsumeCode(userId, code, true, null);
        if (entry is null)
        {
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Sms, "verify-failed");
            return new MfaVerificationResult { Success = false, Message = "Invalid or expired SMS code" };
        }

        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var isNewConfiguration = configuration is null;
        configuration ??= new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = userId, CreatedAt = SystemClock.UtcNow };

        var wasEnrolled = configuration.IsEnabled && configuration.IsSetupComplete;
        if (!wasEnrolled)
        {
            // A pending authenticator secret is not an already verified additional factor.
            configuration.TotpSecretKey = null;
            configuration.QrCodeSetupData = null;
            configuration.PreferredMethod = MfaMethod.Sms;
        }
        configuration.SmsPhoneNumber = entry.PhoneNumber;
        configuration.IsEnabled = true;
        configuration.IsSetupComplete = true;
        configuration.SetupExpiresAt = null;
        configuration.EnabledAt ??= SystemClock.UtcNow;
        configuration.UpdatedAt = SystemClock.UtcNow;
        if (isNewConfiguration) { await mfaConfigRepository.CreateAsync(configuration, cancellationToken).ConfigureAwait(false); }
        else { await mfaConfigRepository.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false); }

        MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Sms, "enrolled");
        logger.LogInformation("SMS MFA enrolled for user {UserId}", LogRedaction.RedactId(userId, "uid"));
        return new MfaVerificationResult { Success = true, Message = "SMS MFA enrolled successfully" };
    }

    /// <inheritdoc />
    public async Task<SmsMfaChallengeResult> SendChallengeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (configuration?.SmsPhoneNumber is not { } phoneNumber)
        {
            return Failure("SMS MFA is not enrolled for this account.");
        }

        var code = await IssueCodeAsync(userId, phoneNumber, false, null, cancellationToken).ConfigureAwait(false);
        return code is null
            ? Failure("A verification code was sent recently. Wait before requesting another.")
            : new SmsMfaChallengeResult
            {
                Success = true,
                PhoneNumberMasked = MaskPhoneNumber(phoneNumber),
                ExpiresInSeconds = (int)CodeLifetime.TotalSeconds
            };
    }

    /// <inheritdoc />
    public Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ConsumeCode(userId, code, false, null) is not null);
    }

    /// <inheritdoc />
    public async Task<SmsMfaChallengeResult> SendSignInChallengeAsync(Guid userId, Guid signInChallengeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (signInChallengeId == Guid.Empty || configuration is not { IsEnabled: true, IsSetupComplete: true } ||
            string.IsNullOrWhiteSpace(configuration.SmsPhoneNumber) || configuration.LockedOutUntil > SystemClock.UtcNow ||
            attemptTrackingService.IsLockedOut(configuration))
        {
            return Failure("Invalid MFA challenge");
        }
        var code = await IssueCodeAsync(userId, configuration.SmsPhoneNumber, false, signInChallengeId, cancellationToken).ConfigureAwait(false);
        return code is null ? Failure("A verification code was sent recently. Wait before requesting another.")
            : new SmsMfaChallengeResult { Success = true, PhoneNumberMasked = MaskPhoneNumber(configuration.SmsPhoneNumber), ExpiresInSeconds = (int)CodeLifetime.TotalSeconds };
    }

    /// <inheritdoc />
    public async Task<bool> VerifySignInCodeAsync(Guid userId, Guid signInChallengeId, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (signInChallengeId == Guid.Empty || configuration is not { IsEnabled: true, IsSetupComplete: true } ||
            string.IsNullOrWhiteSpace(configuration.SmsPhoneNumber) || configuration.LockedOutUntil > SystemClock.UtcNow ||
            attemptTrackingService.IsLockedOut(configuration)) { return false; }
        if (ConsumeCode(userId, code, false, signInChallengeId) is null)
        {
            await attemptTrackingService.RecordFailedMfaAttemptAsync(configuration, MfaMethod.Sms,
                "Invalid or expired challenge proof", null, cancellationToken).ConfigureAwait(false);
            return false;
        }
        configuration.FailedAttempts = 0;
        configuration.LockedOutUntil = null;
        configuration.LastUsedAt = SystemClock.UtcNow;
        configuration.UpdatedAt = SystemClock.UtcNow;
        await mfaConfigRepository.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false);
        await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.Sms, true, null, null, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<string?> IssueCodeAsync(Guid userId, string phoneNumber, bool isEnrollment, Guid? signInChallengeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var entry = new SmsCodeEntry { PhoneNumber = phoneNumber, IsEnrollment = isEnrollment,
            SignInChallengeId = signInChallengeId, CodeDigest = SHA256.HashData(Encoding.UTF8.GetBytes(code)),
            ExpiresAt = SystemClock.UtcNow.Add(CodeLifetime) };
        lock (_cacheGate)
        {
            var throttleKey = ThrottleKeyPrefix + userId.ToString("N");
            if (memoryCache.TryGetValue(throttleKey, out DateTime lastIssued) && SystemClock.UtcNow - lastIssued < ResendThrottleWindow)
            {
                return null;
            }
            memoryCache.Set(EntryKey(userId), entry, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CodeLifetime + EmailCodeService.EvictionSlack
            }.SetSize(1));
            memoryCache.Set(throttleKey, SystemClock.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResendThrottleWindow
            }.SetSize(1));
        }
        try
        {
            await publisher.Publish(new SmsMfaCodeRequestedNotification
            {
                UserId = userId, PhoneNumber = phoneNumber, Code = code, IsEnrollment = isEnrollment
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RemoveIssuedCode(userId, entry);
            throw;
        }
        catch (Exception exception)
        {
            RemoveIssuedCode(userId, entry);
            logger.LogError(exception, "Failed to dispatch SMS MFA code notification for user {UserId}", LogRedaction.RedactId(userId, "uid"));
            throw;
        }
        return code;
    }

    private void RemoveIssuedCode(Guid userId, SmsCodeEntry issued)
    {
        lock (_cacheGate)
        {
            if (memoryCache.TryGetValue(EntryKey(userId), out SmsCodeEntry? current) && ReferenceEquals(current, issued))
            {
                memoryCache.Remove(EntryKey(userId));
            }
        }
    }

    private SmsCodeEntry? ConsumeCode(Guid userId, string code, bool isEnrollment, Guid? signInChallengeId)
    {
        var suppliedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim()));
        lock (_cacheGate)
        {
            var entryKey = EntryKey(userId);
            var entry = memoryCache.TryGetValue(entryKey, out SmsCodeEntry? stored) ? stored : null;
            var matches = CryptographicOperations.FixedTimeEquals(entry?.CodeDigest ?? DummyDigest, suppliedDigest);
            if (entry is null || entry.IsEnrollment != isEnrollment || entry.SignInChallengeId != signInChallengeId) { return null; }
            if (entry.ExpiresAt <= SystemClock.UtcNow)
            {
                memoryCache.Remove(entryKey);
                return null;
            }
            if (!matches)
            {
                entry.FailedAttempts++;
                if (entry.FailedAttempts >= MaxVerificationAttempts) { memoryCache.Remove(entryKey); }
                return null;
            }
            memoryCache.Remove(entryKey);
            return entry;
        }
    }

    private static string EntryKey(Guid userId) => EntryKeyPrefix + userId.ToString("N");

    /// <summary>Normalizes to E.164: optional leading +, then 8–15 digits.</summary>
    internal static string? NormalizePhoneNumber(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();
        if (trimmed.Length < 9 || trimmed.Length > 16)
        {
            return null;
        }

        var span = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;
        if (span.Length < 8 || span.Length > 15 || !span.All(char.IsDigit))
        {
            return null;
        }

        return "+" + span;
    }

    internal static string MaskPhoneNumber(string phoneNumber) =>
        phoneNumber.Length < 5 ? "****" : $"***-***-{phoneNumber[^4..]}";

    private static SmsMfaChallengeResult Failure(string message) =>
        new() { Success = false, PhoneNumberMasked = string.Empty, ExpiresInSeconds = 0, Message = message };

    private sealed class SmsCodeEntry
    {
        public string? PhoneNumber { get; init; }
        public bool IsEnrollment { get; init; }
        public Guid? SignInChallengeId { get; init; }
        public byte[] CodeDigest { get; init; } = [];
        public DateTime ExpiresAt { get; init; }
        public int FailedAttempts { get; set; }
    }
}
