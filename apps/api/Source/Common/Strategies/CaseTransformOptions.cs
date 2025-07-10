namespace GameGuild.Common;

/// <summary>
/// Options for case transformation operations.
/// </summary>
public class CaseTransformOptions
{
    /// <summary>
    /// Maximum length of the resulting string. Default is 100.
    /// </summary>
    public int MaxLength { get; set; } = 100;

    /// <summary>
    /// Custom separator to use instead of the default for the case type.
    /// </summary>
    public string? CustomSeparator { get; set; }

    /// <summary>
    /// Whether to use caching for the transformation. Default is true.
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// Custom string replacements to apply during transformation.
    /// </summary>
    public Dictionary<string, string>? StringReplacements { get; set; }

    /// <summary>
    /// Whether to force lowercase. Default is true for most transformers.
    /// </summary>
    public bool ForceLowerCase { get; set; } = true;

    /// <summary>
    /// Whether to trim whitespace. Default is true.
    /// </summary>
    public bool TrimWhitespace { get; set; } = true;

    /// <summary>
    /// Creates default options for case transformation.
    /// </summary>
    public static CaseTransformOptions Default {
      get => new();
    }

    /// <summary>
    /// Creates options without caching (for one-time transformations).
    /// </summary>
    public static CaseTransformOptions Uncached {
      get => new() { UseCache = false };
    }
}
