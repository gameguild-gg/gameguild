using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     <see cref="IIpBlocklistService"/> backed by <see cref="IDistributedCache"/> (Redis in
///     production, so the list spans every API instance). A per-IP key answers the hot
///     lookup path and carries the entry's own TTL; a registry document backs the admin
///     listing. Writes update both. Read paths fail open: an unavailable cache reports
///     "not blocklisted" and logs, because the blocklist is defense-in-depth and must never
///     take sign-in availability down with it.
/// </summary>
public sealed class DistributedCacheIpBlocklistService(
    IDistributedCache cache,
    ILogger<DistributedCacheIpBlocklistService> logger) : IIpBlocklistService
{
    private const string RegistryKey = "auth:ipblocklist:registry";
    private const string EntryKeyPrefix = "auth:ipblocklist:ip:";

    /// <summary>Cache retention for permanent entries; effectively "does not expire".</summary>
    private static readonly TimeSpan PermanentRetention = TimeSpan.FromDays(3650);

    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);

    public async Task<IpBlocklistEntry?> FindEntryAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        try
        {
            var raw = await cache.GetStringAsync(EntryKey(ipAddress), cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            var entry = JsonSerializer.Deserialize<IpBlocklistEntry>(raw, SerializationOptions);
            if (entry is null)
            {
                return null;
            }

            if (entry.ExpiresAt is { } expiresAt && expiresAt <= SystemClock.UtcNow)
            {
                return null;
            }

            return entry;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "IP blocklist lookup skipped after a cache error: {ExceptionType}",
                exception.GetType().Name);
            return null;
        }
    }

    public async Task<IReadOnlyList<IpBlocklistEntry>> ListEntriesAsync(CancellationToken cancellationToken = default)
    {
        var registry = await ReadRegistryAsync(cancellationToken).ConfigureAwait(false);
        var now = SystemClock.UtcNow;
        return registry.Values
            .Where(entry => entry.ExpiresAt is not { } expiresAt || expiresAt > now)
            .OrderBy(entry => entry.IpAddress, StringComparer.Ordinal)
            .ToList();
    }

    public async Task AddOrUpdateEntryAsync(IpBlocklistEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var normalized = NormalizeIp(entry.IpAddress);

        var registry = await ReadRegistryAsync(cancellationToken).ConfigureAwait(false);
        registry[normalized] = entry with { IpAddress = normalized };

        await WriteRegistryAsync(registry, cancellationToken).ConfigureAwait(false);
        await WriteEntryAsync(entry with { IpAddress = normalized }, cancellationToken).ConfigureAwait(false);
        logger.LogWarning(
            "IP blocklist entry stored for {IpAddress} by {AddedBy}: {Reason}",
            LogRedaction.MaskIpAddress(normalized),
            string.IsNullOrWhiteSpace(entry.AddedBy) ? "unknown" : entry.AddedBy,
            string.IsNullOrWhiteSpace(entry.Reason) ? "no reason recorded" : entry.Reason);
    }

    public async Task<bool> RemoveEntryAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeIp(ipAddress);
        var registry = await ReadRegistryAsync(cancellationToken).ConfigureAwait(false);
        if (!registry.TryRemove(normalized, out _))
        {
            return false;
        }

        await WriteRegistryAsync(registry, cancellationToken).ConfigureAwait(false);
        await cache.RemoveAsync(EntryKey(normalized), cancellationToken).ConfigureAwait(false);
        logger.LogWarning(
            "IP blocklist entry removed for {IpAddress}",
            LogRedaction.MaskIpAddress(normalized));
        return true;
    }

    private async Task<ConcurrentDictionary<string, IpBlocklistEntry>> ReadRegistryAsync(CancellationToken cancellationToken)
    {
        var raw = await cache.GetStringAsync(RegistryKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(raw))
        {
            return new ConcurrentDictionary<string, IpBlocklistEntry>(StringComparer.Ordinal);
        }

        var entries = JsonSerializer.Deserialize<List<IpBlocklistEntry>>(raw, SerializationOptions) ?? [];
        return new ConcurrentDictionary<string, IpBlocklistEntry>(
            entries.Select(entry => new KeyValuePair<string, IpBlocklistEntry>(NormalizeIp(entry.IpAddress), entry with { IpAddress = NormalizeIp(entry.IpAddress) })),
            StringComparer.Ordinal);
    }

    private async Task WriteRegistryAsync(ConcurrentDictionary<string, IpBlocklistEntry> registry, CancellationToken cancellationToken)
    {
        // The registry document never expires on its own; entry-level TTLs govern matching.
        await cache.SetStringAsync(
            RegistryKey,
            JsonSerializer.Serialize(registry.Values.ToList(), SerializationOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PermanentRetention },
            cancellationToken).ConfigureAwait(false);
    }

    private Task WriteEntryAsync(IpBlocklistEntry entry, CancellationToken cancellationToken)
    {
        var retention = entry.ExpiresAt is { } expiresAt
            ? expiresAt - SystemClock.UtcNow
            : PermanentRetention;
        if (retention <= TimeSpan.Zero)
        {
            // Already expired: store nothing; the admin flow rejects such input earlier.
            return cache.RemoveAsync(EntryKey(entry.IpAddress), cancellationToken);
        }

        // Keep the entry alive slightly beyond its logical expiry so the domain-clock check
        // in FindEntryAsync is what decides, not a cache eviction race.
        return cache.SetStringAsync(
            EntryKey(entry.IpAddress),
            JsonSerializer.Serialize(entry, SerializationOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = retention + TimeSpan.FromMinutes(5) },
            cancellationToken);
    }

    private static string NormalizeIp(string ipAddress) => ipAddress.Trim();

    private static string EntryKey(string ipAddress) =>
        EntryKeyPrefix + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(ipAddress.Trim().ToLowerInvariant())));
}
