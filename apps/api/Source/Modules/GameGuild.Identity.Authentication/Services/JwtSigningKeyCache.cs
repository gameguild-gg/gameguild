using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Thread-safe singleton <see cref="IJwtSigningKeyCache"/> implementation. The key
///     rotation store owns refreshes (after initialization, rotation and cleanup);
///     readers always observe a consistent snapshot unioned with the static fallback
///     key, so an empty or momentarily stale cache degrades to the pre-rotation
///     validation behavior instead of rejecting valid tokens.
/// </summary>
public sealed class JwtSigningKeyCache : IJwtSigningKeyCache
{
    private readonly object _sync = new();
    private IReadOnlyList<SecurityKey> _rotationKeys = [];

    public void Update(IEnumerable<SecurityKey> rotationKeys)
    {
        ArgumentNullException.ThrowIfNull(rotationKeys);

        lock (_sync)
        {
            _rotationKeys = rotationKeys.ToArray();
        }
    }

    public IReadOnlyList<SecurityKey> GetValidationKeys(SecurityKey fallbackKey)
    {
        ArgumentNullException.ThrowIfNull(fallbackKey);

        IReadOnlyList<SecurityKey> rotation;
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

        foreach (var key in rotation)
        {
            if (seenKeyIds.Add(key.KeyId))
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
