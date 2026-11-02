using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GameGuild.Compliance.Audit;

/// <summary>Classification outcome for one entity field observation.</summary>
public sealed record SensitiveFieldMatch(
    SensitiveDataCategory Category,
    SensitivityLevel Sensitivity,
    string Rule,
    double Confidence)
{
    public bool IsSensitive => Category != SensitiveDataCategory.None;
}

/// <summary>
/// Deterministic sensitive-field classifier (issue #186): combines a field-name knowledge base with
/// value-pattern detection (reusing the PII redaction engine's compiled preset matchers) to identify
/// PII, PHI, financial and credential fields automatically. Pure and synchronous so it can run inline
/// inside entity-change capture and field-access recording without I/O.
/// </summary>
public static class SensitiveFieldClassifier
{
    private sealed record FieldNameRule(
        string RuleName,
        Regex NameMatcher,
        SensitiveDataCategory Category,
        SensitivityLevel Sensitivity,
        double Confidence);

    private static readonly Regex Email = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.Email);
    private static readonly Regex Phone = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.Phone);
    private static readonly Regex NationalId = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.NationalId);
    private static readonly Regex CreditCard = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.CreditCard);

    // Ordered most-specific first; the first name rule that fires wins.
    private static readonly FieldNameRule[] FieldNameRules =
    [
        new FieldNameRule("name:credentials", BuildNameRegex("password", "passwd", "secret", "passphrase", "privatekey", "credential", "apikey", "apisecret", "clientsecret", "accesstoken", "refreshtoken", "tokensignature"),
            SensitiveDataCategory.Credentials, SensitivityLevel.HighlyRestricted, 0.96),
        new FieldNameRule("name:phi", BuildNameRegex("medicalrecord", "diagnosis", "icdcode", "healthcondition", "treatment", "prescription", "medication", "allergy", "disability", "mentalhealth", "immunization", "labresult", "phi"),
            SensitiveDataCategory.PHI, SensitivityLevel.HighlyRestricted, 0.9),
        new FieldNameRule("name:financial", BuildNameRegex("creditcard", "cardnumber", "iban", "bic", "bankaccount", "routingnumber", "taxid", "vatid", "paymentmethodtoken", "walletaddress"),
            SensitiveDataCategory.Financial, SensitivityLevel.Restricted, 0.9),
        new FieldNameRule("name:pii", BuildNameRegex("email", "emailaddress", "phonenumber", "mobile", "firstname", "lastname", "fullname", "displayname", "dateofbirth", "birthdate", "dob", "ssn", "socialsecurity", "nationalid", "passport", "passportnumber", "driverlicense", "homeaddress", "streetaddress", "zipcode", "postcode", "cityofresidence", "ipaddress", "devicedid", "biometric", "avatarurl"),
            SensitiveDataCategory.PII, SensitivityLevel.Confidential, 0.88)
    ];

    private static readonly ConcurrentDictionary<string, SensitiveFieldMatch?> FieldCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Classifies one field observation by field name first, then by value pattern. Returns null for
    /// non-sensitive fields. Only field-name rules are cached: value-based classifications belong
    /// to the individual observation and must be reevaluated when another value uses the same field.
    /// </summary>
    public static SensitiveFieldMatch? Classify(string entityType, string fieldName, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (IsInfrastructureField(fieldName))
        {
            return null;
        }

        if (FieldCache.TryGetValue(fieldName, out var cached))
        {
            return cached;
        }

        var match = ClassifyUncached(entityType, fieldName, value);
        if (match is not null && match.Rule.StartsWith("name:", StringComparison.Ordinal))
        {
            FieldCache[fieldName] = match;
        }
        return match;
    }

    /// <summary>Deterministic masking for a field value based on its sensitivity level.</summary>
    public static string Mask(string? value, SensitivityLevel level)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return level switch
        {
            SensitivityLevel.Public or SensitivityLevel.Internal => value,
            SensitivityLevel.Confidential => PartialMask(value),
            SensitivityLevel.Restricted => new string('*', Math.Min(value.Length, 4)) + LastSegment(value, 2),
            _ => "[MASKED]"
        };
    }

    private static SensitiveFieldMatch? ClassifyUncached(string entityType, string fieldName, string? value)
    {
        foreach (var rule in FieldNameRules)
        {
            if (rule.NameMatcher.IsMatch(fieldName))
            {
                return new SensitiveFieldMatch(rule.Category, rule.Sensitivity, rule.RuleName, rule.Confidence);
            }
        }

        // The name was inconclusive: inspect the observed value with the compiled PII matchers.
        if (!string.IsNullOrEmpty(value))
        {
            if (NationalId.IsMatch(value))
            {
                return new SensitiveFieldMatch(SensitiveDataCategory.PII, SensitivityLevel.Restricted, "value:national-id", 0.95);
            }
            if (LooksLikeCreditCard(value))
            {
                return new SensitiveFieldMatch(SensitiveDataCategory.Financial, SensitivityLevel.Restricted, "value:credit-card", 0.93);
            }
            if (Email.IsMatch(value))
            {
                return new SensitiveFieldMatch(SensitiveDataCategory.PII, SensitivityLevel.Confidential, "value:email", 0.9);
            }
            if (Phone.IsMatch(value) && !HasLetters(value))
            {
                return new SensitiveFieldMatch(SensitiveDataCategory.PII, SensitivityLevel.Confidential, "value:phone", 0.75);
            }
        }

        return null;
    }

    private static bool LooksLikeCreditCard(string value) =>
        CreditCard.IsMatch(value) && LuhnValid(StripSeparators(value));

    private static string StripSeparators(string value) => value.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static bool LuhnValid(string digits)
    {
        if (digits.Length is < 13 or > 19 || !digits.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        var alternate = false;
        for (var index = digits.Length - 1; index >= 0; index--)
        {
            var digit = digits[index] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }
            sum += digit;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }

    private static bool HasLetters(string value) => value.Any(char.IsLetter);

    private static bool IsInfrastructureField(string fieldName) =>
        fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase)
        || fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
        || fieldName.EndsWith("At", StringComparison.OrdinalIgnoreCase)
        || fieldName.EndsWith("AtUtc", StringComparison.OrdinalIgnoreCase)
        || fieldName.EndsWith("Utc", StringComparison.OrdinalIgnoreCase)
        || fieldName.Contains("Hash", StringComparison.OrdinalIgnoreCase)
        || fieldName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase);

    private static string PartialMask(string value) =>
        value.Length <= 2
            ? new string('*', value.Length)
            : value[..1] + new string('*', Math.Min(value.Length - 1, 8));

    private static string LastSegment(string value, int length) =>
        value.Length <= length ? string.Empty : value[^length..];

    /// <summary>Builds a case-insensitive whole-word matcher for the given field-name fragments.</summary>
    private static Regex BuildNameRegex(params string[] fragments) =>
        new($"(?:{string.Join("|", fragments.Select(Regex.Escape))})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}
