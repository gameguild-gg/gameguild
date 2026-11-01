using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Redaction pass applied to every raw webhook delivery before it is persisted by the
///     opt-in retention policy (issue #408: capture raw payload and all headers).
///     Ordering contract: intake ALWAYS verifies the provider signature first and the
///     redacted form is only stored afterwards — the redactor is applied at persistence
///     time, never before verification, and it is one-way: secrets can never be recovered
///     from what it emits.
///     Rules:
///     <list type="bullet">
///         <item>Secret-bearing header VALUES (authorization, signatures, cookies, API keys) are replaced by <c>sha256:&lt;hex&gt;</c> — the hash remains verifiable-by-hash: recompute SHA-256 over the candidate raw value and compare.</item>
///         <item>Token-shaped strings inside the payload (Bearer credentials, provider secret keys such as <c>sk_live_…</c>/<c>whsec_…</c>) are likewise replaced by hashes. This is unconditional: secrets are never stored literally.</item>
///         <item>Email addresses are replaced by a deterministic <c>email:&lt;short-hash&gt;</c> token when <c>RedactPii</c> is on (default), destroying the address while keeping occurrences correlatable.</item>
///         <item>Non-secret headers (user agent, request ids, timestamps) are kept verbatim — that is their forensic value.</item>
///     </list>
/// </summary>
public static partial class WebhookRawDeliveryRedactor
{
    /// <summary>Prefix marking a SHA-256 hash replacement of a redacted secret value.</summary>
    public const string HashPrefix = "sha256:";

    /// <summary>Prefix marking a redacted (pseudonymized) email address token.</summary>
    public const string EmailPrefix = "email:";

    /// <summary>Maximum length of a raw header VALUE considered for verbatim retention; longer values are hashed.</summary>
    public const int MaxVerbatimHeaderValueLength = 2048;

    private const int EmailHashLength = 16;

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?<scheme>Bearer|Basic)\s+(?<credential>[A-Za-z0-9\-._~+/]+=*)", RegexOptions.IgnoreCase)]
    private static partial Regex CredentialSchemeRegex();

    [GeneratedRegex(@"\b(?:sk|rk)_(?:live|test)_[A-Za-z0-9]{16,}\b|\bwhsec_[A-Za-z0-9]{16,}\b|\bwhsec_[A-Za-z0-9]{8,}\b")]
    private static partial Regex ProviderSecretKeyRegex();

    private static readonly HashSet<string> SecretHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        "Set-Cookie",
        "Stripe-Signature",
        "PayPal-Transmission-Sig",
        "Apple-Pay-Signature",
        "X-Api-Key",
        "X-Amz-Security-Token",
        "X-Goog-Api-Key"
    };

    /// <summary>
    ///     Redacts a raw webhook payload. JSON payloads are walked node by node so only
    ///     matching string leaves are rewritten; non-JSON bodies get a regex fallback over
    ///     the raw text. Secrets are always hashed; emails are pseudonymized when
    ///     <paramref name="redactPii"/> is set.
    /// </summary>
    /// <param name="rawPayload">Raw HTTP body as received from the provider.</param>
    /// <param name="redactPii">Whether email addresses are pseudonymized.</param>
    /// <returns>The redacted payload, safe to persist.</returns>
    public static string RedactPayload(string rawPayload, bool redactPii)
    {
        if (string.IsNullOrEmpty(rawPayload))
        {
            return string.Empty;
        }

        try
        {
            var node = JsonSerializer.Deserialize<JsonElement>(rawPayload);
            var redacted = RedactJsonElement(node, redactPii);
            return JsonSerializer.Serialize(redacted);
        }
        catch (JsonException)
        {
            // Not JSON (Braintree form bodies, IPN query strings): fall back to a
            // pattern pass over the raw text.
            return RedactPlainText(rawPayload, redactPii);
        }
    }

    /// <summary>
    ///     Redacts the full header set of a delivery into a persisted JSON document.
    ///     Secret-bearing headers are hashed (verifiable-by-hash); every other header is
    ///     retained verbatim after an email/credential value scrub so the document stays
    ///     forensically useful without ever carrying a literal secret.
    /// </summary>
    /// <param name="headers">Header name/value pairs of the callback request.</param>
    /// <param name="redactPii">Whether email addresses are pseudonymized.</param>
    /// <returns>Serialized redacted headers, safe to persist.</returns>
    public static string RedactHeaders(IEnumerable<KeyValuePair<string, string>> headers, bool redactPii)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var document = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
            {
                continue;
            }

            document[header.Key] = RedactHeaderValue(header.Key, header.Value, redactPii);
        }

        return JsonSerializer.Serialize(document);
    }

    /// <summary>
    ///     Redacts a single header value: secret-bearing names (and any value carrying a
    ///     credential scheme or provider secret) hash to <c>sha256:&lt;hex&gt;</c>; other
    ///     values are scrubbed of emails when PII redaction is on and truncated to the
    ///     verbatim ceiling.
    /// </summary>
    public static string RedactHeaderValue(string name, string value, bool redactPii)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (IsSecretHeader(name) || LooksLikeCredential(value))
        {
            return HashToken(value);
        }

        var scrubbed = RedactPlainText(value, redactPii);
        return scrubbed.Length <= MaxVerbatimHeaderValueLength ? scrubbed : HashToken(value);
    }

    /// <summary>
    ///     One-way SHA-256 replacement for a secret value. The returned token remains
    ///     verifiable-by-hash: <see cref="VerifyHash"/> recomputes the digest of a candidate
    ///     raw value and compares in constant time.
    /// </summary>
    public static string HashToken(string value)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return HashPrefix + Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>
    ///     Verifies that a candidate raw value matches a stored redacted hash
    ///     (<c>sha256:&lt;hex&gt;</c>). Values stored under any other form never match.
    /// </summary>
    public static bool VerifyHash(string candidateValue, string? storedRedacted)
    {
        if (string.IsNullOrEmpty(storedRedacted) || !storedRedacted.StartsWith(HashPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var storedHex = storedRedacted[HashPrefix.Length..];
        if (storedHex.Length != 64)
        {
            return false;
        }

        Span<byte> stored = stackalloc byte[32];
        if (!TryDecodeHexString(storedHex, stored))
        {
            return false;
        }

        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(candidateValue));
        return CryptographicOperations.FixedTimeEquals(candidate, stored);
    }

    /// <summary>
    ///     Pseudonymizes an email address into a deterministic short token so repeated
    ///     occurrences stay correlatable while the address itself is destroyed.
    /// </summary>
    public static string RedactEmail(string email)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return EmailPrefix + Convert.ToHexString(digest)[..EmailHashLength].ToLowerInvariant();
    }

    private static bool IsSecretHeader(string name) => SecretHeaderNames.Contains(name);

    private static bool LooksLikeCredential(string value) =>
        CredentialSchemeRegex().IsMatch(value) || ProviderSecretKeyRegex().IsMatch(value);

    private static string RedactPlainText(string text, bool redactPii)
    {
        var result = ProviderSecretKeyRegex().Replace(
            text,
            match => HashToken(match.Value));
        result = CredentialSchemeRegex().Replace(
            result,
            match => $"{match.Groups["scheme"].Value} {HashToken(match.Groups["credential"].Value)}");

        if (redactPii)
        {
            result = EmailRegex().Replace(result, match => RedactEmail(match.Value));
        }

        return result;
    }

    private static JsonElement RedactJsonElement(JsonElement element, bool redactPii)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var redacted = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    redacted[property.Name] = RedactJsonElement(property.Value, redactPii);
                }

                return JsonSerializer.SerializeToElement(redacted);
            }

            case JsonValueKind.Array:
            {
                var redacted = new List<JsonElement>();
                foreach (var item in element.EnumerateArray())
                {
                    redacted.Add(RedactJsonElement(item, redactPii));
                }

                return JsonSerializer.SerializeToElement(redacted);
            }

            case JsonValueKind.String:
            {
                var value = element.GetString();
                if (string.IsNullOrEmpty(value))
                {
                    return element;
                }

                if (LooksLikeCredential(value))
                {
                    return JsonSerializer.SerializeToElement(HashToken(value));
                }

                if (redactPii && EmailRegex().IsMatch(value))
                {
                    return JsonSerializer.SerializeToElement(RedactPlainText(value, redactPii));
                }

                return element;
            }

            default:
                return element;
        }
    }

    private static bool TryDecodeHexString(string hex, Span<byte> destination)
    {
        if (hex.Length != destination.Length * 2)
        {
            return false;
        }

        for (var index = 0; index < destination.Length; index++)
        {
            if (!byte.TryParse(hex.AsSpan(index * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var decoded))
            {
                return false;
            }

            destination[index] = decoded;
        }

        return true;
    }
}
