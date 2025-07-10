using Newtonsoft.Json.Serialization;


namespace GameGuild.Common;

/// <summary>
/// Snake case transformer implementation using Newtonsoft.Json.
/// </summary>
public class SnakeCaseTransformer : CachedCaseTransformer
{
    private static readonly SnakeCaseNamingStrategy NamingStrategy = new();

    protected override string CacheKeyPrefix => "snake";

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

/// <summary>
/// Provides utilities for converting strings and type names to snake_case format.
/// Backward compatibility wrapper around SnakeCaseTransformer.
/// </summary>
public static class SnakeCase
{
    private static readonly SnakeCaseTransformer Transformer = new();

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="name">The string to convert.</param>
    /// <returns>The converted string in snake_case.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public static string Convert(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        return Transformer.Transform(name);
    }

    /// <summary>
    /// Converts a type name to snake_case.
    /// </summary>
    /// <typeparam name="T">The type whose name to convert.</typeparam>
    /// <returns>The converted type name in snake_case.</returns>
    public static string Convert<T>() => Transformer.TransformType<T>();

    /// <summary>
    /// Converts a type name to snake_case.
    /// </summary>
    /// <param name="type">The type whose name to convert.</param>
    /// <returns>The converted type name in snake_case.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static string Convert(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return Transformer.TransformType(type);
    }

    /// <summary>
    /// Converts a string to snake_case without caching (useful for one-time conversions).
    /// </summary>
    /// <param name="name">The string to convert.</param>
    /// <returns>The converted string in snake_case.</returns>
    public static string ConvertUncached(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return Transformer.Transform(name, CaseTransformOptions.Uncached);
    }

    /// <summary>
    /// Converts multiple strings to snake_case.
    /// </summary>
    /// <param name="names">The strings to convert.</param>
    /// <returns>An array of converted strings in snake_case.</returns>
    public static string[] ConvertMany(params string[] names) => Transformer.TransformMany(names);

    /// <summary>
    /// Converts multiple type names to snake_case.
    /// </summary>
    /// <param name="types">The types whose names to convert.</param>
    /// <returns>An array of converted type names in snake_case.</returns>
    public static string[] ConvertMany(params Type[] types)
    {
        if (types.Length == 0) return [];

        var result = new string[types.Length];
        for (var i = 0; i < types.Length; i++) {
          result[i] = Convert(types[i]);
        }

        return result;
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
