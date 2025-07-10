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

    protected override string CacheKeyPrefix {
      get => "slug";
    }

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
