using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GameGuild;

/// <summary>
///     Utility for redacting sensitive identifiers in log output.
///     Produces a consistent, irreversible short hash so logs can still
///     correlate requests to the same tenant without leaking the raw GUID.
/// </summary>
public static partial class LogRedaction
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

    /// <summary>
    ///     Masks an email address for logging: keeps the first character of the local
    ///     part and the domain, e.g. <c>"alice@example.com"</c> → <c>"a***@example.com"</c>.
    ///     Returns <c>"none"</c> for null/empty input.
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "none";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "invalid";

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var kept = local.Length == 1 ? local[0].ToString() : local[..1];
        return $"{kept}***@{domain}";
    }

    /// <summary>
    ///     Redacts a secret (password, token, key) to a deterministic short hash that
    ///     is safe to log: cannot be reversed, but identical secrets correlate.
    ///     Returns <c>"none"</c> for null/empty input.
    /// </summary>
    public static string RedactSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return "none";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return $"secret:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }

    /// <summary>
    ///     Sanitizes an untrusted string for structured logging by replacing control
    ///     characters (CR, LF, and other C0 controls) with visible escape markers.
    ///     Prevents log forging (CWE-117) without destroying diagnostic value.
    /// </summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return ControlCharactersRegex().Replace(value, "␀");
    }

    [GeneratedRegex(@"[\u0000-\u001F]")]
    private static partial Regex ControlCharactersRegex();
}
