namespace GameGuild.Common;

/// <summary>
/// Enumeration of supported case types.
/// </summary>
public enum CaseType {
  /// <summary>
  /// kebab-case format (lowercase with hyphens).
  /// </summary>
  Kebab,

  /// <summary>
  /// snake_case format (lowercase with underscores).
  /// </summary>
  Snake,

  /// <summary>
  /// slug format (URL-friendly lowercase with hyphens).
  /// </summary>
  Slug,
}
