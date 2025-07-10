using System.Text.RegularExpressions;
using Slugify;


namespace GameGuild.Common;

/// <summary>
/// Slug case transformer implementation using Slugify.Core.
/// </summary>
public class SlugCaseTransformer : CachedCaseTransformer
{
    private static readonly SlugHelper SlugHelper;

    static SlugCaseTransformer()
    {
        var config = new SlugHelperConfiguration
        {
            ForceLowerCase = true,
            CollapseDashes = true,
            TrimWhitespace = true,
            StringReplacements = new Dictionary<string, string> { { "&", "and" }, { "+", "plus" } },
        };

        SlugHelper = new SlugHelper(config);
    }

    protected override string CacheKeyPrefix => "slug";

    protected override string TransformCore(string input, CaseTransformOptions options)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var result = input;

        // Apply custom replacements if provided (merge with default ones)
        var replacements = new Dictionary<string, string> { { "&", "and" }, { "+", "plus" } };
        if (options.StringReplacements != null)
          foreach (var replacement in options.StringReplacements)
          {
            replacements[replacement.Key] = replacement.Value;
          }

        // Apply replacements
        foreach (var replacement in replacements)
        {
            result = result.Replace(replacement.Key, replacement.Value);
        }

        // Generate slug using Slugify.Core
        result = SlugHelper.GenerateSlug(result);

        // Apply custom separator if provided
        if (!string.IsNullOrEmpty(options.CustomSeparator) && options.CustomSeparator != "-") result = result.Replace("-", options.CustomSeparator);

        // Truncate if necessary
        if (result.Length > options.MaxLength) result = result[..options.MaxLength].TrimEnd('-');

        return result;
    }

    public override bool IsValidFormat(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        // Check if it matches slug pattern: lowercase letters, numbers, and dashes
        // No leading/trailing dashes, no consecutive dashes
        return Regex.IsMatch(input, @"^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }

    /// <summary>
    /// Generates a unique slug by appending a number if the base slug already exists.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="existingSlugs">Collection of existing slugs to check against.</param>
    /// <param name="options">Transformation options.</param>
    /// <returns>A unique slug string.</returns>
    public string GenerateUnique(string text, IEnumerable<string> existingSlugs, CaseTransformOptions? options = null)
    {
        options ??= CaseTransformOptions.Default;
        var baseSlug = Transform(text, options);

        var enumerable = existingSlugs as string[] ?? existingSlugs.ToArray();

        if (!enumerable.Contains(baseSlug)) return baseSlug;

        var existingSet = new HashSet<string>(enumerable, StringComparer.OrdinalIgnoreCase);
        var counter = 1;
        string uniqueSlug;

        do
        {
            var suffix = $"-{counter}";
            var availableLength = options.MaxLength - suffix.Length;

            if (availableLength <= 0)
            {
                uniqueSlug = counter.ToString();
            }
            else
            {
                var truncatedBase = baseSlug.Length > availableLength
                                  ? baseSlug.Substring(0, availableLength).TrimEnd('-')
                                  : baseSlug;
                uniqueSlug = truncatedBase + suffix;
            }

            counter++;
        } while (existingSet.Contains(uniqueSlug));

        return uniqueSlug;
    }
}

/// <summary>
/// Provides utilities for converting strings to URL-friendly slug format.
/// Backward compatibility wrapper around SlugCaseTransformer.
/// </summary>
public static class SlugCase
{
    private static readonly SlugCaseTransformer Transformer = new();

    /// <summary>
    /// Converts a string to a URL-friendly slug format using Slugify.Core.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A URL-friendly slug string.</returns>
    /// <exception cref="ArgumentException">Thrown when text is null or empty.</exception>
    public static string Convert(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        if (maxLength <= 0) throw new ArgumentException("Max length must be greater than zero.", nameof(maxLength));

        return Transformer.Transform(text, new CaseTransformOptions { MaxLength = maxLength });
    }

    /// <summary>
    /// Converts a string to a slug with a specific separator.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="separator">The separator to use (default: "-").</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A URL-friendly slug string with custom separator.</returns>
    public static string Convert(string text, string separator, int maxLength = 100)
    {
        return Transformer.Transform(text, new CaseTransformOptions 
        { 
            MaxLength = maxLength, 
            CustomSeparator = separator 
        });
    }

    /// <summary>
    /// Converts multiple strings to slugs.
    /// </summary>
    /// <param name="texts">The strings to convert.</param>
    /// <returns>An array of slug strings.</returns>
    public static string[] ConvertMany(params string[] texts) => Transformer.TransformMany(texts);

    /// <summary>
    /// Generates a unique slug by appending a number if the base slug already exists.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="existingSlugs">Collection of existing slugs to check against.</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A unique slug string.</returns>
    public static string GenerateUnique(string text, IEnumerable<string> existingSlugs, int maxLength = 100)
    {
        return Transformer.GenerateUnique(text, existingSlugs, new CaseTransformOptions { MaxLength = maxLength });
    }

    /// <summary>
    /// Validates if a string is already a valid slug.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>True if the text is a valid slug, false otherwise.</returns>
    public static bool IsValidSlug(string text) => Transformer.IsValidFormat(text);

    /// <summary>
    /// Creates a slug from a type name.
    /// </summary>
    /// <typeparam name="T">The type whose name to convert.</typeparam>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A slug based on the type name.</returns>
    public static string FromType<T>(int maxLength = 100) => 
        Transformer.TransformType<T>(new CaseTransformOptions { MaxLength = maxLength });

    /// <summary>
    /// Creates a slug from a type name.
    /// </summary>
    /// <param name="type">The type whose name to convert.</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A slug based on the type name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static string FromType(Type type, int maxLength = 100)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return Transformer.TransformType(type, new CaseTransformOptions { MaxLength = maxLength });
    }

    /// <summary>
    /// Converts a string to a slug without caching (useful for one-time conversions).
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
    /// <returns>A URL-friendly slug string.</returns>
    public static string ConvertUncached(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (maxLength <= 0) throw new ArgumentException("Max length must be greater than zero.", nameof(maxLength));

        return Transformer.Transform(text, new CaseTransformOptions { MaxLength = maxLength, UseCache = false });
    }

    /// <summary>
    /// Clears the internal cache by disposing and recreating it. Useful for memory management in long-running applications.
    /// </summary>
    public static void ClearCache() => CachedCaseTransformer.ClearCache();

    /// <summary>
    /// Disposes the memory cache. Call this method when the application is shutting down.
    /// </summary>
    public static void Dispose() => CachedCaseTransformer.Dispose();
}
