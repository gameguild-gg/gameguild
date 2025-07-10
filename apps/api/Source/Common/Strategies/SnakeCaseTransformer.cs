using Newtonsoft.Json.Serialization;


namespace GameGuild.Common;

/// <summary>
/// Snake case transformer implementation using Newtonsoft.Json.
/// </summary>
public class SnakeCaseTransformer : CachedCaseTransformer
{
  private static readonly SnakeCaseNamingStrategy NamingStrategy = new();

  protected override string CacheKeyPrefix {
    get => "snake";
  }

  protected override string TransformCore(string input, CaseTransformOptions options)
  {
    if (string.IsNullOrEmpty(input)) return string.Empty;

    var result = input;

    // Apply custom replacements if provided
    if (options.StringReplacements != null)
      foreach (var replacement in options.StringReplacements)
      {
        result = result.Replace(replacement.Key, replacement.Value);
      }

    // Trim whitespace if requested
    if (options.TrimWhitespace) result = result.Trim();

    // Convert to snake case using Newtonsoft.Json naming strategy
    result = NamingStrategy.GetPropertyName(result, false);

    // Apply custom separator if provided
    if (!string.IsNullOrEmpty(options.CustomSeparator) && options.CustomSeparator != "_") result = result.Replace("_", options.CustomSeparator);

    // Truncate if necessary
    if (result.Length > options.MaxLength) result = result[..options.MaxLength].TrimEnd('_');

    return result;
  }

  public override bool IsValidFormat(string input)
  {
    if (string.IsNullOrEmpty(input)) return false;

    // Check if it matches snake_case pattern: lowercase letters, numbers, and underscores
    // No leading/trailing underscores, no consecutive underscores
    return System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-z0-9]+(?:_[a-z0-9]+)*$");
  }
}
