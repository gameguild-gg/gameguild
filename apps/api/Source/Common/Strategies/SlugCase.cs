namespace GameGuild.Common;

/// <summary>
/// Provides utilities for converting strings to URL-friendly slug format.
/// Backward compatibility wrapper around SlugCaseTransformer.
/// </summary>
public static class SlugCase {
  private static readonly SlugCaseTransformer Transformer = new();

  /// <summary>
  /// Converts a string to a URL-friendly slug format using Slugify.Core.
  /// </summary>
  /// <param name="text">The text to convert to a slug.</param>
  /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
  /// <returns>A URL-friendly slug string.</returns>
  /// <exception cref="ArgumentException">Thrown when text is null or empty.</exception>
  public static string Convert(string text, int maxLength = 100) {
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
  public static string Convert(string text, string separator, int maxLength = 100) { return Transformer.Transform(text, new CaseTransformOptions { MaxLength = maxLength, CustomSeparator = separator, }); }

  /// <summary>
  /// Converts multiple strings to slugs.
  /// </summary>
  /// <param name="texts">The strings to convert.</param>
  /// <returns>An array of slug strings.</returns>
  public static string[] ConvertMany(params string[] texts) { return Transformer.TransformMany(texts); }

  /// <summary>
  /// Generates a unique slug by appending a number if the base slug already exists.
  /// </summary>
  /// <param name="text">The text to convert to a slug.</param>
  /// <param name="existingSlugs">Collection of existing slugs to check against.</param>
  /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
  /// <returns>A unique slug string.</returns>
  public static string GenerateUnique(string text, IEnumerable<string> existingSlugs, int maxLength = 100) { return Transformer.GenerateUnique(text, existingSlugs, new CaseTransformOptions { MaxLength = maxLength }); }

  /// <summary>
  /// Validates if a string is already a valid slug.
  /// </summary>
  /// <param name="text">The text to validate.</param>
  /// <returns>True if the text is a valid slug, false otherwise.</returns>
  public static bool IsValidSlug(string text) { return Transformer.IsValidFormat(text); }

  /// <summary>
  /// Creates a slug from a type name.
  /// </summary>
  /// <typeparam name="T">The type whose name to convert.</typeparam>
  /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
  /// <returns>A slug based on the type name.</returns>
  public static string FromType<T>(int maxLength = 100) { return Transformer.TransformType<T>(new CaseTransformOptions { MaxLength = maxLength }); }

  /// <summary>
  /// Creates a slug from a type name.
  /// </summary>
  /// <param name="type">The type whose name to convert.</param>
  /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
  /// <returns>A slug based on the type name.</returns>
  /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
  public static string FromType(Type type, int maxLength = 100) {
    if (type == null) throw new ArgumentNullException(nameof(type));

    return Transformer.TransformType(type, new CaseTransformOptions { MaxLength = maxLength });
  }

  /// <summary>
  /// Converts a string to a slug without caching (useful for one-time conversions).
  /// </summary>
  /// <param name="text">The text to convert to a slug.</param>
  /// <param name="maxLength">Maximum length of the resulting slug (default: 100).</param>
  /// <returns>A URL-friendly slug string.</returns>
  public static string ConvertUncached(string text, int maxLength = 100) {
    if (string.IsNullOrEmpty(text)) return string.Empty;

    if (maxLength <= 0) throw new ArgumentException("Max length must be greater than zero.", nameof(maxLength));

    return Transformer.Transform(text, new CaseTransformOptions { MaxLength = maxLength, UseCache = false });
  }

  /// <summary>
  /// Clears the internal cache by disposing and recreating it. Useful for memory management in long-running applications.
  /// </summary>
  public static void ClearCache() { CachedCaseTransformer.ClearCache(); }

  /// <summary>
  /// Disposes the memory cache. Call this method when the application is shutting down.
  /// </summary>
  public static void Dispose() { CachedCaseTransformer.Dispose(); }
}
