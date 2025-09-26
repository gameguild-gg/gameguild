using GameGuild.Modules.Resources;

namespace GameGuild.Modules.Localization;

/// <summary>
/// EntityBase representing supported languages for localization
/// </summary>
[Table("languages")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Name))]
public class Language : EntityBase
{
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
    /// Indicates if this language is the default language for the platform
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Collection of resource localizations in this language
    /// </summary>
    public virtual ICollection<ResourceLocalization> ResourceLocalizations { get; init; } = [];
}
