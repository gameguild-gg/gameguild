using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     <see cref="IDistributedCache"/>-backed OAuth link-state store (issue #1181): Redis in
/// production, the host's distributed-memory cache otherwise, so a link flow started on one
/// instance completes on another. State values are stored as SHA-256 digests — the raw state
/// never enters the cache — under a per-(user, provider) key, mirroring the ten-minute window
/// the web link-state cookie already uses.
/// </summary>
public sealed class DistributedCacheOAuthLinkStateStore(IDistributedCache cache) : IOAuthLinkStateStore
{
    /// <summary>How long an issued link state can complete its flow.</summary>
    public static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private const string KeyPrefix = "auth:oauth:link-state:";

    private static readonly TimeSpan EvictionSlack = TimeSpan.FromMinutes(5);

    public async Task SaveAsync(Guid userId, string provider, string state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(state);

        // One pending state per (user, provider): the newest authorize replaces the previous.
        await cache.SetStringAsync(
            Key(userId, provider),
            Digest(state),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateLifetime + EvictionSlack },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ValidateAndConsumeAsync(Guid userId, string provider, string state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(state))
        {
            return false;
        }

        var key = Key(userId, provider);
        var stored = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(stored) || !FixedTimeEquals(stored, Digest(state)))
        {
            return false;
        }

        // One-shot: remove before reporting success so a replayed state cannot link twice.
        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string Key(Guid userId, string provider) =>
        KeyPrefix + userId.ToString("N") + ":" + provider.ToLowerInvariant();

    private static string Digest(string state) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var mismatch = 0;
        for (var i = 0; i < left.Length; i++)
        {
            mismatch |= left[i] ^ right[i];
        }

        return mismatch == 0;
    }
}
