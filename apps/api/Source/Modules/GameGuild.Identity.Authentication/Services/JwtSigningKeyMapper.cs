using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Converts <see cref="JwtSigningKey"/> rotation-store rows into signing/validation
///     <see cref="SecurityKey"/> instances (issue #1141). Rows that are not usable HS256
///     keys (wrong algorithm, malformed or undersized material) map to null so a single
///     bad row can never break token issuance or validation.
/// </summary>
public static class JwtSigningKeyMapper
{
    private const int MinimumHmacSha256KeyBytes = 32;

    /// <summary>
    ///     Maps a rotation-store key to a <see cref="SymmetricSecurityKey"/> carrying the
    ///     row's KeyId (surfaced in the JWT 'kid' header), or null when unusable.
    /// </summary>
    public static SymmetricSecurityKey? ToSecurityKey(JwtSigningKey? key)
    {
        if (key is null
            || string.IsNullOrWhiteSpace(key.KeyId)
            || !string.Equals(key.Algorithm, SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        byte[] material;
        try
        {
            material = Convert.FromBase64String(key.KeyMaterial);
        }
        catch (FormatException)
        {
            return null;
        }

        return material.Length >= MinimumHmacSha256KeyBytes
            ? new SymmetricSecurityKey(material) { KeyId = key.KeyId }
            : null;
    }
}
