namespace GameGuild.Common;

/// <summary>
/// Factory for creating case transformer instances.
/// </summary>
public static class CaseTransformerFactory {
  private static readonly Lazy<KebabCaseTransformer> LazyKebabTransformer = new(() => new KebabCaseTransformer());
  private static readonly Lazy<SnakeCaseTransformer> LazySnakeTransformer = new(() => new SnakeCaseTransformer());
  private static readonly Lazy<SlugCaseTransformer> LazySlugTransformer = new(() => new SlugCaseTransformer());

  /// <summary>
  /// Gets a kebab-case transformer instance.
  /// </summary>
  public static KebabCaseTransformer Kebab {
    get => LazyKebabTransformer.Value;
  }

  /// <summary>
  /// Gets a snake_case transformer instance.
  /// </summary>
  public static SnakeCaseTransformer Snake {
    get => LazySnakeTransformer.Value;
  }

  /// <summary>
  /// Gets a slug-case transformer instance.
  /// </summary>
  public static SlugCaseTransformer Slug {
    get => LazySlugTransformer.Value;
  }

  /// <summary>
  /// Gets a transformer instance by type.
  /// </summary>
  /// <typeparam name="T">The transformer type to get.</typeparam>
  /// <returns>The transformer instance.</returns>
  /// <exception cref="NotSupportedException">Thrown when the transformer type is not supported.</exception>
  public static T Get<T>() where T : ICaseTransformer {
    return typeof(T) switch {
      Type t when t == typeof(KebabCaseTransformer) => (T)(object)Kebab,
      Type t when t == typeof(SnakeCaseTransformer) => (T)(object)Snake,
      Type t when t == typeof(SlugCaseTransformer) => (T)(object)Slug,
      _ => throw new NotSupportedException($"Transformer type {typeof(T).Name} is not supported."),
    };
  }

  /// <summary>
  /// Gets a transformer instance by case type enum.
  /// </summary>
  /// <param name="caseType">The case type to get a transformer for.</param>
  /// <returns>The transformer instance.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when the case type is not supported.</exception>
  public static ICaseTransformer Get(CaseType caseType) {
    return caseType switch { CaseType.Kebab => Kebab, CaseType.Snake => Snake, CaseType.Slug => Slug, _ => throw new ArgumentOutOfRangeException(nameof(caseType), caseType, "Unsupported case type."), };
  }

  /// <summary>
  /// Transforms a string using the specified case type.
  /// </summary>
  /// <param name="input">The input string to transform.</param>
  /// <param name="caseType">The case type to use for transformation.</param>
  /// <param name="options">Optional transformation options.</param>
  /// <returns>The transformed string.</returns>
  public static string Transform(string input, CaseType caseType, CaseTransformOptions? options = null) {
    var transformer = Get(caseType);

    return transformer.Transform(input, options ?? CaseTransformOptions.Default);
  }

  /// <summary>
  /// Validates if a string is in the correct format for the specified case type.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <param name="caseType">The case type to validate against.</param>
  /// <returns>True if the string is in the correct format, false otherwise.</returns>
  public static bool IsValidFormat(string input, CaseType caseType) {
    var transformer = Get(caseType);

    return transformer.IsValidFormat(input);
  }
}
