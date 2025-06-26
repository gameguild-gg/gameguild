using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Common.Entities;

/// <summary>
/// Entity representing supported languages for localization
/// Mirrors the TypeScript LanguageEntity from the API
/// </summary>
[Table("Languages")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Name))]
public class Language : BaseEntity
{
    private string _code = string.Empty;

    private string _name = string.Empty;

    private bool _isActive = true;

    private ICollection<ResourceLocalization> _resourceLocalizations = new List<ResourceLocalization>();

    /// <summary>
    /// Language code (e.g., 'en-US', 'pt-BR', 'es-ES')
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string Code
    {
        get => _code;
        set => _code = value;
    }

    /// <summary>
    /// Display name of the language
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    /// <summary>
    /// Whether this language is currently active/supported
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    /// <summary>
    /// Collection of resource localizations in this language
    /// </summary>
    public virtual ICollection<ResourceLocalization> ResourceLocalizations
    {
        get => _resourceLocalizations;
        set => _resourceLocalizations = value;
    }
}
