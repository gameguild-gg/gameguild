using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     <see cref="IPasswordlessRequestIpRateLimiter"/> backed by <see cref="IDistributedCache"/>
/// (Redis in production, in-memory fallback in single-node hosts). Magic-link and email-code
/// requests share one fixed-window budget per client IP. The counter store is
/// defense-in-depth: every store error fails open so the passwordless request path keeps its
/// availability, and the response shape stays enumeration-safe either way (callers log and
/// skip generation instead of returning a distinguishable error).
/// </summary>
public sealed class DistributedCachePasswordlessRequestIpRateLimiter(
    IDistributedCache cache,
    IOptions<PasswordlessRequestIpRateLimitOptions> options,
    ILogger<DistributedCachePasswordlessRequestIpRateLimiter> logger) : IPasswordlessRequestIpRateLimiter
{
    private const string KeyPrefix = "auth:passwordless:iprl:";

    /// <summary>
    ///     Extra cache retention beyond the window so a bucket is never physically evicted
    ///     before its logical lifetime ends when the domain and cache clocks drift apart.
    /// </summary>
    private static readonly TimeSpan EvictionSlack = TimeSpan.FromMinutes(5);

    public async Task<bool> TryAdmitRequestAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || !settings.IsValid())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            // No observed client IP: nothing to partition on, so the request is admitted.
            return true;
        }

        try
        {
            var now = SystemClock.UtcNow;
            var window = settings.Window;
            var windowBucket = now.Ticks / window.Ticks;
            var key = KeyPrefix + windowBucket.ToString(System.Globalization.CultureInfo.InvariantCulture)
                      + ":" + IpDigest(ipAddress);

            var current = await ReadCountAsync(key, cancellationToken).ConfigureAwait(false);
            if (current >= settings.MaxRequestsPerWindow)
            {
                logger.LogWarning(
                    "Passwordless sign-in request throttled for IP {IpAddress}: {Count} requests within the last {Minutes:F0} minutes",
                    LogRedaction.MaskIpAddress(ipAddress),
                    current,
                    window.TotalMinutes);
                return false;
            }

            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(current + 1),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window + EvictionSlack
                },
                cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail open: the per-IP budget is abuse protection, not an authorization gate.
            logger.LogWarning(
                "Passwordless per-IP rate limit skipped after a cache error: {ExceptionType}",
                exception.GetType().Name);
            return true;
        }
    }

    private async Task<int> ReadCountAsync(string key, CancellationToken cancellationToken)
    {
        var raw = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(raw)
            ? 0
            : JsonSerializer.Deserialize<int>(raw);
    }

    /// <summary>
    ///     Raw addresses never become cache keys; the full digest avoids truncation
    ///     collisions, mirroring the email-digest keys of the per-email throttles.
    /// </summary>
    private static string IpDigest(string ipAddress) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress.Trim().ToLowerInvariant())));
}
