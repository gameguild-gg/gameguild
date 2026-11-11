using System.Text.RegularExpressions;

namespace GameGuild.API.Eventing;

/// <summary>
///     Redacts durable consumer-failure text before it is persisted to
///     <c>inbox_receipts.LastError</c> (issue #1313 S2-b). Handler exceptions routinely embed
///     request values, credentials and connection strings in their messages and stack traces;
///     <see cref="InboxStore.RecordFailureAsync"/> is the one kernel store that keeps that
///     text durably, and the admin dead-letter surface serves it back — so the redaction pass
///     happens here, at the write, before truncation.
/// </summary>
public static partial class InboxFailureTextRedactor
{
    public const string RedactedPlaceholder = "[redacted]";

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?<scheme>Bearer|Basic|Api-?Key|Token)\s+(?<credential>[A-Za-z0-9\-._~+/]+=*)", RegexOptions.IgnoreCase)]
    private static partial Regex CredentialSchemeRegex();

    [GeneratedRegex(
        @"\b(?:sk|rk|pk)_(?:live|test)_[A-Za-z0-9]{16,}\b" +
        @"|\bwhsec_[A-Za-z0-9]{8,}\b" +
        @"|\b(?<prefix>api[-_]?key|secret|password|passwd|pwd|token)(?<separator>\s*[:=]\s*)(?<value>[^\s,;""'&]{6,})",
        RegexOptions.IgnoreCase)]
    private static partial Regex SecretPatternRegex();

    [GeneratedRegex(@"(?<key>AccountKey|SharedAccessKey|Password|User ID|Uid|Pwd)\s*=\s*(?<value>[^;@\s]{4,})", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringRegex();

    /// <summary>
    ///     Redacts emails, credential-scheme pairs, provider secret keys, secret-bearing
    ///     key/value pairs and connection-string passwords from failure text. Never throws:
    ///     a redaction failure fails closed to the placeholder so raw text cannot leak.
    /// </summary>
    public static string Redact(string failureText)
    {
        if (string.IsNullOrEmpty(failureText))
        {
            return failureText;
        }

        try
        {
            var redacted = EmailRegex().Replace(failureText, RedactedPlaceholder);
            redacted = CredentialSchemeRegex().Replace(redacted, match => $"{match.Groups["scheme"].Value} {RedactedPlaceholder}");
            redacted = SecretPatternRegex().Replace(redacted, match =>
                match.Groups["prefix"].Success
                    ? $"{match.Groups["prefix"].Value}{match.Groups["separator"].Value}{RedactedPlaceholder}"
                    : RedactedPlaceholder);
            redacted = ConnectionStringRegex().Replace(redacted, match => $"{match.Groups["key"].Value}={RedactedPlaceholder}");
            return redacted;
        }
        catch (RegexMatchTimeoutException)
        {
            return RedactedPlaceholder;
        }
    }
}
