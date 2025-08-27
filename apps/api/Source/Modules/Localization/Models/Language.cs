using GameGuild.Common;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.Localization;

/// <summary>
/// Entity representing supported languages for localization
/// </summary>
[Table("Languages")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Name))]
public class Language : Entity {
  /// <summary>
  /// Language code (e.g., 'en-US', 'pt-BR', 'es-ES')
  /// </summary>
  [Required]
  [MaxLength(64)]
  public string Code { get; init; } = string.Empty;

  /// <summary>
  /// Display name of the language
  /// </summary>
  [Required]
  [MaxLength(64)]
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// Whether this language is currently active/supported
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Collection of resource localizations in this language
  /// </summary>
  public virtual ICollection<ResourceLocalization> ResourceLocalizations { get; init; } = new List<ResourceLocalization>();
}
