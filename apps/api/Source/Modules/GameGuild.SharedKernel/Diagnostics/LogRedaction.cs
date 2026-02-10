using System.Security.Cryptography;
using System.Text;

namespace GameGuild;

/// <summary>
///     Utility for redacting sensitive identifiers in log output.
///     Produces a consistent, irreversible short hash so logs can still
///     correlate requests to the same tenant without leaking the raw GUID.
/// </summary>
public static class LogRedaction
{
    /// <summary>
    ///     Redacts a <see cref="Guid"/> to a short hash prefix (first 8 hex chars of SHA-256).
    ///     Returns <c>"none"</c> for <see cref="Guid.Empty"/> or null.
    ///     Deterministic — same input always produces the same output.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var redacted = LogRedaction.RedactId(tenantId);
    ///     // e.g. "tid:a1b2c3d4"
    ///     </code>
    /// </example>
    public static string RedactId(Guid? id, string prefix = "tid")
    {
        if (!id.HasValue || id.Value == Guid.Empty)
            return "none";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id.Value.ToString("N")));
        return $"{prefix}:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }

    /// <summary>
    ///     Redacts a string identifier (e.g., user ID, subject ID) to a short hash.
    /// </summary>
    public static string RedactId(string? id, string prefix = "uid")
    {
        if (string.IsNullOrEmpty(id))
            return "none";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id));
        return $"{prefix}:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }
}
