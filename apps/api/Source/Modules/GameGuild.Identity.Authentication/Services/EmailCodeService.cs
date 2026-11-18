using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Issues and verifies one-time email sign-in codes backed by <see cref="IDistributedCache"/>
///     (issue #1272: the Redis path in production, mirroring the token revocation store — the
///     previous in-process memory cache reset the sixty-second resend throttle and the
///     five-attempt ceiling per node and lost every pending code on restart). Codes are six
///     cryptographically random digits, live for ten minutes, accept at most five verification
///     attempts, are single-use, and are stored only as SHA-256 digests. Code comparison is
///     constant-time, including a dummy comparison on the unknown-email path so timings stay
///     uniform.
/// </summary>
public class EmailCodeService(
    ILogger<EmailCodeService> logger,
    IDistributedCache cache) : IEmailCodeService
{
    private const string EntryKeyPrefix = "emailcode:entry:";
    private const string ThrottleKeyPrefix = "emailcode:throttle:";

    /// <summary>How long an issued code can be verified.</summary>
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Minimum interval between two code requests for the same address.</summary>
    public static readonly TimeSpan ResendThrottleWindow = TimeSpan.FromSeconds(60);

    /// <summary>Wrong-code attempts tolerated before the code is invalidated.</summary>
    public const int MaxVerificationAttempts = 5;

    /// <summary>
    ///     Extra retention beyond <see cref="CodeLifetime"/> for the cache-level safety-net
    ///     eviction, so an entry is never physically dropped by the cache before its logical
    ///     lifetime has been judged by the domain clock.
    /// </summary>
    public static readonly TimeSpan EvictionSlack = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly byte[] DummyDigest = SHA256.HashData(Encoding.UTF8.GetBytes("email-code-timing-equalizer"));

    public async Task<string?> GenerateEmailCodeAsync(Guid userId, string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var emailDigest = EmailDigest(normalizedEmail);
        var throttleKey = ThrottleKeyPrefix + emailDigest;

        var lastIssuedRaw = await cache.GetStringAsync(throttleKey).ConfigureAwait(false);
        if (DateTime.TryParse(lastIssuedRaw, out var lastIssued) &&
            SystemClock.UtcNow - lastIssued < ResendThrottleWindow)
        {
            logger.LogWarning(
                "Email-code request throttled for user {UserId}; a code was issued {Seconds:F0}s ago",
                LogRedaction.RedactId(userId, "uid"),
                (SystemClock.UtcNow - lastIssued).TotalSeconds);
            return null;
        }

        // Crypto-random six-digit code (000000–999999, uniform via the RNG rejection-free Int32 overload).
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var state = new EmailCodeState(
            userId,
            normalizedEmail,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))),
            SystemClock.UtcNow.Add(CodeLifetime),
            FailedAttempts: 0);

        // Cache-level eviction is only a safety net for abandoned entries, so it must be
        // relative to the cache's own clock. The logical ten-minute TTL lives in
        // state.ExpiresAt and is enforced against the domain clock (SystemClock) in
        // VerifyEmailCodeAsync — stamping an absolute, domain-clock-derived expiration here
        // would let the cache's real-time scanner evict live entries whenever the domain clock
        // (e.g. a fake provider in tests) disagrees with wall time.
        await cache.SetStringAsync(
            EntryKeyPrefix + emailDigest,
            JsonSerializer.Serialize(state, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CodeLifetime + EvictionSlack })
            .ConfigureAwait(false);

        await cache.SetStringAsync(
            throttleKey,
            SystemClock.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ResendThrottleWindow })
            .ConfigureAwait(false);

        logger.LogInformation("Generated one-time email sign-in code for user {UserId}", LogRedaction.RedactId(userId, "uid"));
        return code;
    }

    public async Task<TokenValidationResult> VerifyEmailCodeAsync(string email, string code)
    {
        var normalizedEmail = NormalizeEmail(email);
        var entryKey = EntryKeyPrefix + EmailDigest(normalizedEmail);
        var suppliedDigest = string.IsNullOrWhiteSpace(code)
            ? SHA256.HashData(Encoding.UTF8.GetBytes(string.Empty))
            : SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));

        // Constant-time comparison on every path, including unknown emails, so the
        // digest match itself never becomes a timing oracle.
        EmailCodeState? state = null;
        var storedPayload = await cache.GetStringAsync(entryKey).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(storedPayload))
        {
            try
            {
                state = JsonSerializer.Deserialize<EmailCodeState>(storedPayload, JsonOptions);
            }
            catch (JsonException)
            {
                // A corrupt entry behaves as an unknown code: fail closed.
                state = null;
            }
        }

        var storedDigest = state is null ? DummyDigest : Convert.FromHexString(state.CodeDigest);
        var matches = CryptographicOperations.FixedTimeEquals(storedDigest, suppliedDigest);

        if (state is null)
        {
            logger.LogWarning("Verification attempted with no pending email sign-in code");
            return TokenValidationResult.Failed("Invalid code");
        }

        if (state.ExpiresAt < SystemClock.UtcNow)
        {
            await cache.RemoveAsync(entryKey).ConfigureAwait(false);
            logger.LogWarning("Expired email sign-in code used for user {UserId}", LogRedaction.RedactId(state.UserId, "uid"));
            return TokenValidationResult.Failed("Expired code");
        }

        if (!matches)
        {
            state = state with { FailedAttempts = state.FailedAttempts + 1 };
            if (state.FailedAttempts >= MaxVerificationAttempts)
            {
                await cache.RemoveAsync(entryKey).ConfigureAwait(false);
                logger.LogWarning(
                    "Email sign-in code exhausted after {Attempts} failed attempts for user {UserId}",
                    state.FailedAttempts,
                    LogRedaction.RedactId(state.UserId, "uid"));
            }
            else
            {
                // Persist the shared failure count so the ceiling applies across nodes.
                await cache.SetStringAsync(
                    entryKey,
                    JsonSerializer.Serialize(state, JsonOptions),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CodeLifetime + EvictionSlack })
                    .ConfigureAwait(false);
                logger.LogWarning(
                    "Wrong email sign-in code submitted for user {UserId}; {Remaining} attempts left",
                    LogRedaction.RedactId(state.UserId, "uid"),
                    MaxVerificationAttempts - state.FailedAttempts);
            }

            return TokenValidationResult.Failed("Invalid code");
        }

        // Single use is enforced by removal: a replay (from any node) reads no pending code.
        await cache.RemoveAsync(entryKey).ConfigureAwait(false);
        logger.LogInformation("Email sign-in code consumed successfully for user {UserId}", LogRedaction.RedactId(state.UserId, "uid"));
        return new TokenValidationResult(true, state.UserId, state.Email);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string EmailDigest(string normalizedEmail) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));

    /// <summary>
    ///     Stored state of an issued one-time email sign-in code. The code itself is never
    ///     kept: only its SHA-256 digest, so a cache or log disclosure cannot leak a usable
    ///     credential.
    /// </summary>
    internal sealed record EmailCodeState(Guid UserId, string Email, string CodeDigest, DateTime ExpiresAt, int FailedAttempts);
}
