namespace GameGuild.Common;

/// <summary>
/// Extension methods for convenient case transformations.
/// </summary>
public static class CaseTransformExtensions {
  /// <summary>
  /// Converts a string to kebab-case.
  /// </summary>
  /// <param name="input">The string to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The string in kebab-case format.</returns>
  public static string ToKebabCase(this string input, CaseTransformOptions? options = null) { return CaseTransformerFactory.Kebab.Transform(input, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Converts a string to snake_case.
  /// </summary>
  /// <param name="input">The string to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The string in snake_case format.</returns>
  public static string ToSnakeCase(this string input, CaseTransformOptions? options = null) { return CaseTransformerFactory.Snake.Transform(input, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Converts a string to slug format.
  /// </summary>
  /// <param name="input">The string to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The string in slug format.</returns>
  public static string ToSlugCase(this string input, CaseTransformOptions? options = null) { return CaseTransformerFactory.Slug.Transform(input, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Converts a string using the specified case type.
  /// </summary>
  /// <param name="input">The string to convert.</param>
  /// <param name="caseType">The case type to convert to.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The transformed string.</returns>
  public static string ToCase(this string input, CaseType caseType, CaseTransformOptions? options = null) { return CaseTransformerFactory.Transform(input, caseType, options); }

  /// <summary>
  /// Validates if a string is in the specified case format.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <param name="caseType">The case type to validate against.</param>
  /// <returns>True if the string is in the correct format, false otherwise.</returns>
  public static bool IsValidCase(this string input, CaseType caseType) { return CaseTransformerFactory.IsValidFormat(input, caseType); }

  /// <summary>
  /// Checks if a string is in kebab-case format.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <returns>True if the string is in kebab-case format, false otherwise.</returns>
  public static bool IsKebabCase(this string input) { return CaseTransformerFactory.Kebab.IsValidFormat(input); }

  /// <summary>
  /// Checks if a string is in snake_case format.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <returns>True if the string is in snake_case format, false otherwise.</returns>
  public static bool IsSnakeCase(this string input) { return CaseTransformerFactory.Snake.IsValidFormat(input); }

  /// <summary>
  /// Checks if a string is in slug format.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <returns>True if the string is in slug format, false otherwise.</returns>
  public static bool IsSlugCase(this string input) { return CaseTransformerFactory.Slug.IsValidFormat(input); }

  /// <summary>
  /// Converts a type name to the specified case format.
  /// </summary>
  /// <param name="type">The type whose name to convert.</param>
  /// <param name="caseType">The case type to convert to.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The transformed type name.</returns>
  public static string ToCase(this Type type, CaseType caseType, CaseTransformOptions? options = null) {
    if (type == null) throw new ArgumentNullException(nameof(type));

    var transformer = CaseTransformerFactory.Get(caseType);

    if (transformer is CachedCaseTransformer cachedTransformer) return cachedTransformer.TransformType(type, options);

    return transformer.Transform(type.Name, options ?? CaseTransformOptions.Default);
  }

  /// <summary>
  /// Converts a type name to kebab-case.
  /// </summary>
  /// <param name="type">The type whose name to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The type name in kebab-case format.</returns>
  public static string ToKebabCase(this Type type, CaseTransformOptions? options = null) { return CaseTransformerFactory.Kebab.TransformType(type, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Converts a type name to snake_case.
  /// </summary>
  /// <param name="type">The type whose name to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The type name in snake_case format.</returns>
  public static string ToSnakeCase(this Type type, CaseTransformOptions? options = null) { return CaseTransformerFactory.Snake.TransformType(type, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Converts a type name to slug format.
  /// </summary>
  /// <param name="type">The type whose name to convert.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The type name in slug format.</returns>
  public static string ToSlugCase(this Type type, CaseTransformOptions? options = null) { return CaseTransformerFactory.Slug.TransformType(type, options ?? CaseTransformOptions.Default); }
}
