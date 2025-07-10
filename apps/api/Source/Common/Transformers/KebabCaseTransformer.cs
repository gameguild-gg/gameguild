using System.Text.RegularExpressions;


namespace GameGuild.Common;

/// <summary>
/// Kebab case transformer implementation.
/// </summary>
public partial class KebabCaseTransformer : CachedCaseTransformer {
  [GeneratedRegex("([a-z])([A-Z])")] private static partial Regex KebabCaseGeneratedRegex();

  [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
  private static partial Regex KebabCaseValidationRegex();

  protected override string CacheKeyPrefix {
    get => "kebab";
  }

  protected override string TransformCore(string input, CaseTransformOptions options) {
    if (string.IsNullOrEmpty(input)) return string.Empty;

    var result = input;

    // Apply custom replacements if provided
    if (options.StringReplacements != null)
      foreach (var replacement in options.StringReplacements) { result = result.Replace(replacement.Key, replacement.Value); }

    // Trim whitespace if requested
    if (options.TrimWhitespace) result = result.Trim();

    // Convert to kebab case
    result = KebabCaseGeneratedRegex().Replace(result, "$1-$2");

    // Force lowercase if requested
    if (options.ForceLowerCase) result = result.ToLower();

    // Apply custom separator if provided
    if (!string.IsNullOrEmpty(options.CustomSeparator) && options.CustomSeparator != "-") result = result.Replace("-", options.CustomSeparator);

    // Truncate if necessary
    if (result.Length > options.MaxLength) result = result[..options.MaxLength].TrimEnd('-');

    return result;
  }

  public override bool IsValidFormat(string input) {
    if (string.IsNullOrEmpty(input)) return false;

    return KebabCaseValidationRegex().IsMatch(input);
  }
}
