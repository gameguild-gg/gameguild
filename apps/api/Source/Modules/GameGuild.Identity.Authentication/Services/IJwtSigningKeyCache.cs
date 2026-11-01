using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Process-wide snapshot of the JWT validation keys held in the key-rotation store
///     (issue #1141). Rotated keys must remain verifiable for the lifetime of the tokens
///     they signed, so validation surfaces (bearer middleware, JwtTokenService) read the
///     candidate key set from this cache instead of holding a single static key.
/// </summary>
public interface IJwtSigningKeyCache
{
    /// <summary>
    ///     Returns the keys usable for signature validation: every key currently valid in
    ///     the rotation store plus <paramref name="fallbackKey"/> (the statically
    ///     configured secret), so tokens issued before a rotation — or by issuers that
    ///     still use the static key — keep validating across key rollover.
    /// </summary>
    IReadOnlyList<SecurityKey> GetValidationKeys(SecurityKey fallbackKey);

    /// <summary>
    ///     Replaces the cached rotation-key set with the keys currently valid for
    ///     validation (as returned by IKeyRotationService.GetValidationKeysAsync).
    /// </summary>
    void Update(IEnumerable<SecurityKey> rotationKeys);
}
