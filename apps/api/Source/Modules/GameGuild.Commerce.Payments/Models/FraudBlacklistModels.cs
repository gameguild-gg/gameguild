namespace GameGuild.Commerce.Payments;

/// <summary>
///     Kind of value stored in a <see cref="FraudBlacklistEntry"/> (issue #409). The store is
///     intentionally generic: every entry type supports the same wildcard patterns, expiry and
///     whitelist override semantics, so new attribute kinds need no schema change.
/// </summary>
public enum FraudBlacklistEntryType
{
    /// <summary>Client IP address or CIDR-style wildcard, e.g. <c>203.0.113.*</c>.</summary>
    IpAddress = 0,

    /// <summary>Email domain, e.g. <c>mailinator.com</c> or <c>*.malicious.com</c>.</summary>
    EmailDomain = 1,

    /// <summary>Exact email address.</summary>
    Email = 2,

    /// <summary>Card BIN (first 6-8 digits of the card number).</summary>
    CardBin = 3,

    /// <summary>Device fingerprint identifier.</summary>
    DeviceFingerprint = 4,

    /// <summary>Gateway payment-method token.</summary>
    PaymentMethodToken = 5,

    /// <summary>Gateway card fingerprint (stable hash across customers for the same card).</summary>
    CardFingerprint = 6,

    /// <summary>Provider customer identifier.</summary>
    ExternalCustomerId = 7
}

/// <summary>How a blacklist entry came to exist.</summary>
public enum FraudBlacklistSource
{
    /// <summary>Added by an administrator through the management API.</summary>
    Manual = 0,

    /// <summary>Added automatically when a fraud review was declined as confirmed fraud.</summary>
    AutoFraudDecline = 1
}

/// <summary>Outcome of evaluating a value against the tenant blacklist (issue #409).</summary>
public enum FraudBlacklistEvaluation
{
    /// <summary>No active entry matched the value.</summary>
    NoMatch = 0,

    /// <summary>An active blacklist entry matched the value.</summary>
    Blacklisted = 1,

    /// <summary>An active manual whitelist override matched the value; blacklist entries are suppressed.</summary>
    Whitelisted = 2
}

/// <summary>
///     Result of a blacklist evaluation: the decision, and when matched, the bounded evidence
///     of which entry fired (never raw provider tokens).
/// </summary>
/// <param name="Evaluation">Match decision.</param>
/// <param name="MatchedPattern">The stored pattern that matched, when <see cref="FraudBlacklistEvaluation.Blacklisted"/>.</param>
/// <param name="EntryType">Entry type that matched.</param>
/// <param name="Reason">Bounded reason recorded with the matching entry.</param>
/// <param name="Source">How the matching entry was added.</param>
/// <param name="ExpiresAt">When the matching entry expires (null = no expiry).</param>
public sealed record FraudBlacklistMatch(
    FraudBlacklistEvaluation Evaluation,
    FraudBlacklistEntryType? EntryType = null,
    string? MatchedPattern = null,
    string? Reason = null,
    FraudBlacklistSource? Source = null,
    DateTime? ExpiresAt = null)
{
    /// <summary>Convenience: the evaluation was a blacklist hit (not suppressed by a whitelist override).</summary>
    public bool IsBlacklisted => Evaluation == FraudBlacklistEvaluation.Blacklisted;
}

/// <summary>
///     Wildcard matcher for fraud blacklist patterns (issue #409). Patterns are compared
///     case-insensitively; <c>*</c> matches any run of characters, so <c>*.malicious.com</c>,
///     <c>192.168.*</c> and <c>user-*@example.com</c> all behave as expected.
/// </summary>
public static class FraudBlacklistPatterns
{
    /// <summary>Normalizes a value or pattern for storage and comparison.</summary>
    /// <param name="value">Raw value as supplied.</param>
    /// <returns>Trimmed, lowercased value; null when blank.</returns>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    ///     Matches a stored pattern against a concrete value. A pattern without wildcards is an
    ///     exact (normalized) comparison.
    /// </summary>
    /// <param name="pattern">Stored pattern (already normalized).</param>
    /// <param name="value">Value being evaluated (already normalized).</param>
    /// <returns>True when the pattern matches the value.</returns>
    public static bool Matches(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
        }

        return GlobMatch(pattern, value);
    }

    /// <summary>Classic two-pointer glob match supporting only <c>*</c> wildcards.</summary>
    private static bool GlobMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> value)
    {
        var patternIndex = 0;
        var valueIndex = 0;
        var starIndex = -1;
        var starValueIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == value[valueIndex] || pattern[patternIndex] == '?'))
            {
                patternIndex++;
                valueIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                starValueIndex = valueIndex;
                patternIndex++;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                starValueIndex++;
                valueIndex = starValueIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
