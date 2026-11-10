using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Thread-safe singleton <see cref="IJwtSigningKeyCache"/> implementation. The key
///     rotation store owns refreshes (after initialization, rotation and cleanup);
///     readers always observe a consistent snapshot unioned with the static fallback
///     key, so an empty or momentarily stale cache degrades to the pre-rotation
///     validation behavior instead of rejecting valid tokens.
/// </summary>
public sealed class JwtSigningKeyCache(ILogger<JwtSigningKeyCache>? logger = null) : IJwtSigningKeyCache
{
    private readonly object _sync = new();
    private long _refreshSequence;
    private long _publishedSequence;
    private IReadOnlyList<CachedKey> _rotationKeys = [];

    public void Update(IEnumerable<JwtSigningKey> rotationKeys)
        => Publish(rotationKeys, Interlocked.Increment(ref _refreshSequence));

    private void Publish(IEnumerable<JwtSigningKey> rotationKeys, long sequence)
    {
        ArgumentNullException.ThrowIfNull(rotationKeys);

        var snapshot = rotationKeys.Select(row => (Row: row, Key: JwtSigningKeyMapper.ToSecurityKey(row)))
            .Where(entry => entry.Key is not null)
            .Select(entry => new CachedKey(entry.Key!, entry.Row.ValidFrom, entry.Row.ExpiresAt)).ToArray();
        lock (_sync)
        {
            if (sequence < _publishedSequence) { return; }
            _rotationKeys = snapshot;
            _publishedSequence = sequence;
        }
    }

    public async Task RefreshAsync(IKeyRotationService rotationService, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Order publications by invocation, not completion. Reads may run concurrently,
        // but an older query finishing late must not erase a newly learned signing key.
        var sequence = Interlocked.Increment(ref _refreshSequence);
        try
        {
            var keys = await rotationService.GetValidationKeysAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Publish(keys, sequence);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // Store errors can contain connection strings or key material. Keep only the type.
            logger?.LogWarning("JWT validation-key refresh unavailable: {ErrorType}", exception.GetType().FullName);
        }
    }

    public IReadOnlyList<SecurityKey> GetValidationKeys(SecurityKey fallbackKey)
    {
        ArgumentNullException.ThrowIfNull(fallbackKey);

        IReadOnlyList<CachedKey> rotation;
        lock (_sync)
        {
            rotation = _rotationKeys;
        }

        if (rotation.Count == 0)
        {
            return [fallbackKey];
        }

        // The statically configured key stays in the set forever: tokens signed before
        // a rotation (and by static-key issuers) must survive every key rollover.
        var seenKeyIds = new HashSet<string?>(StringComparer.Ordinal) { fallbackKey.KeyId };
        var keys = new List<SecurityKey>(rotation.Count + 1) { fallbackKey };

        var now = SystemClock.UtcNow;
        foreach (var entry in rotation)
        {
            if (entry.ValidFrom <= now && now < entry.ExpiresAt && seenKeyIds.Add(entry.Key.KeyId))
            {
                keys.Add(entry.Key);
            }
        }

        return keys;
    }

    private sealed record CachedKey(SecurityKey Key, DateTime ValidFrom, DateTime ExpiresAt);
}
