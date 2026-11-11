using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild;

/// <summary>
///     Multi-instance implementation of <see cref="IIdempotencyStore"/> backed by
///     <see cref="IDistributedCache"/> (Redis in production via
///     <c>AddStackExchangeRedisCache</c>, the distributed memory cache in single-node/dev).
///     Cached responses are shared across every application instance, so an idempotency key
///     replayed against a different pod still returns the original response.
/// </summary>
/// <remarks>
///     <para>
///     <b>Concurrency caveat:</b> <see cref="IDistributedCache"/> has no atomic
///     set-if-absent operation, so <see cref="TryMarkInFlightAsync"/> uses a
///     get-then-set check. Two instances receiving the same new key inside that window can
///     both proceed (full mutual exclusion needs a distributed lock such as RedLock —
///     the same limitation the interface documents). Replay correctness — the property
///     clients depend on — is preserved because the response is written to shared storage.
///     </para>
/// </remarks>
public sealed class DistributedCacheIdempotencyStore(IDistributedCache cache) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<IdempotentResponse?> TryGetResponseAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var payload = await _cache.GetStringAsync(ResponseKey(key)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IdempotentResponse>(payload, SerializerOptions);
        }
        catch (JsonException)
        {
            // Corrupted payloads (e.g. a store flushed mid-write) are treated as a miss so the
            // request is processed again rather than dead-ending.
            return null;
        }
    }

    public Task SetResponseAsync(string key, IdempotentResponse response, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(response);
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return _cache.SetStringAsync(
            ResponseKey(key),
            JsonSerializer.Serialize(response, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration });
    }

    public async Task<bool> TryMarkInFlightAsync(string key, TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (timeout < TimeSpan.Zero)
        {
            timeout = TimeSpan.Zero;
        }

        var inFlightKey = InFlightKey(key);
        var existing = await _cache.GetStringAsync(inFlightKey).ConfigureAwait(false);
        if (existing is not null)
        {
            return false;
        }

        await _cache.SetStringAsync(
            inFlightKey,
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeout }).ConfigureAwait(false);
        return true;
    }

    public Task RemoveInFlightAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _cache.RemoveAsync(InFlightKey(key));
    }

    private static string ResponseKey(string key) => $"idempotency:response:{key}";

    private static string InFlightKey(string key) => $"idempotency:in-flight:{key}";
}

/// <summary>
///     Registers the distributed idempotency store as the default
///     <see cref="IIdempotencyStore"/>. A local <see cref="MemoryDistributedCache"/> is
///     installed only when no other <see cref="IDistributedCache"/> provider was
///     registered, mirroring how the host composes Redis in production.
/// </summary>
public static class DistributedIdempotencyStoreExtensions
{
    /// <summary>
    ///     Ensures an <see cref="IDistributedCache"/> provider exists and registers
    ///     <see cref="DistributedCacheIdempotencyStore"/> as the default store.
    /// </summary>
    public static IServiceCollection AddDistributedIdempotencyStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
        services.TryAddSingleton<IIdempotencyStore, DistributedCacheIdempotencyStore>();
        return services;
    }
}
