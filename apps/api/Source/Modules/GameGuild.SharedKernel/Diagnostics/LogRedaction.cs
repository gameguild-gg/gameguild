using System.Globalization;
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
        {
            return "none";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id.Value.ToString("N")));
        return $"{prefix}:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }

    /// <summary>
    ///     Redacts a string identifier (e.g., user ID, subject ID) to a short hash.
    /// </summary>
    public static string RedactId(string? id, string prefix = "uid")
    {
        if (string.IsNullOrEmpty(id))
        {
            return "none";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id));
        return $"{prefix}:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }

    /// <summary>
    ///     Masks an email address for logging while keeping a deterministic,
    ///     correlation-friendly fingerprint: the first letter-or-digit of the
    ///     local part, the first letter-or-digit of the domain, and the public
    ///     suffix. Example: <c>alice@example.com</c> → <c>a***@e***.com</c>.
    ///     Only letters and digits from the input ever reach the output, so no
    ///     other part of the address leaks and control characters cannot be
    ///     smuggled through. Returns <c>"none"</c> for null/empty input and
    ///     <c>"invalid"</c> when the value has no usable local part or no
    ///     <c>@</c> separator.
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "none";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "invalid";
        }

        var localPrefix = LeadingSafeChars(email[..atIndex], 1);
        var domain = email[(atIndex + 1)..];
        var domainPrefix = LeadingSafeChars(domain, 1);
        if (domainPrefix.Length == 0)
        {
            return "invalid";
        }

        var lastDot = domain.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < domain.Length - 1)
        {
            var suffix = LeadingSafeChars(domain[(lastDot + 1)..], 8);
            if (suffix.Length > 0)
            {
                return $"{localPrefix}***@{domainPrefix}***.{suffix}";
            }
        }

        return $"{localPrefix}***@{domainPrefix}***";
    }

    /// <summary>
    ///     Masks a username or handle to a deterministic first-character plus
    ///     length marker, e.g. <c>alice</c> → <c>a***(5)</c>. Returns
    ///     <c>"none"</c> for null/empty input and <c>"invalid"</c> when the
    ///     value has no leading letter or digit.
    /// </summary>
    public static string MaskUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return "none";
        }

        var prefix = LeadingSafeChars(username, 1);
        return prefix.Length == 0 ? "invalid" : $"{prefix}***({username.Length})";
    }

    /// <summary>
    ///     Masks an IPv4 address for logging while preserving network
    ///     correlation: octets 1 and 3 are kept, e.g. <c>10.20.0.30</c> →
    ///     <c>10.x.0.x</c>. Non-IPv4 values (IPv6, hostnames, free text) fall
    ///     back to a short deterministic hash. Returns <c>"none"</c> for
    ///     null/empty input.
    /// </summary>
    public static string MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return "none";
        }

        if (TrySplitIpv4(ipAddress, out var octet1, out _, out var octet3))
        {
            return $"{octet1}.x.{octet3}.x";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress));
        return $"ip:{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
    }

    /// <summary>
    ///     Masks an identifier that may be an email address, an IPv4 address,
    ///     or a username, dispatching to the most specific mask. Use for
    ///     security logs (sign-in identifiers, threat-detection alerts) where
    ///     the shape of the identifier is not known up front. Returns
    ///     <c>"none"</c> for null/empty input.
    /// </summary>
    public static string MaskIdentifier(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "none";
        }

        if (identifier.Contains('@'))
        {
            return MaskEmail(identifier);
        }

        if (TrySplitIpv4(identifier, out _, out _, out _))
        {
            return MaskIpAddress(identifier);
        }

        return MaskUsername(identifier);
    }

    private static bool TrySplitIpv4(string value, out string octet1, out string octet2, out string octet3)
    {
        octet1 = octet2 = octet3 = string.Empty;
        var parts = value.Split('.');
        if (parts.Length != 4)
        {
            return false;
        }

        foreach (var part in parts)
        {
            if (!byte.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }
        }

        octet1 = parts[0];
        octet2 = parts[1];
        octet3 = parts[2];
        return true;
    }

    private static string LeadingSafeChars(string segment, int maxChars)
    {
        var count = 0;
        while (count < maxChars && count < segment.Length && char.IsLetterOrDigit(segment[count]))
        {
            count++;
        }

        return count == 0 ? string.Empty : segment[..count].ToLowerInvariant();
    }

    /// <summary>
    ///     Redacts a secret (password, token, key) without retaining a fingerprint.
    ///     Hashing low-entropy secrets would let a log reader test password guesses.
    ///     Returns <c>"none"</c> for null/empty input.
    /// </summary>
    public static string RedactSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return "none";
        }

        return "secret:redacted";
    }

    /// <summary>
    ///     Sanitizes an untrusted string for structured logging by replacing control
    ///     characters and Unicode line separators with visible escape markers.
    ///     Prevents log forging (CWE-117) without destroying diagnostic value.
    /// </summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return ControlCharactersRegex().Replace(value, "␀");
    }

    [GeneratedRegex(@"[\u0000-\u001F\u007F-\u009F\u2028\u2029]")]
    private static partial Regex ControlCharactersRegex();
}
