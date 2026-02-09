using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Localization;

/// <summary>
/// Represents a localized version of a resource field
/// </summary>
[Table("resource_localizations")]
[Index(nameof(LanguageId))]
[Index(nameof(ResourceId))]
[Index(nameof(FieldName))]
[Index(nameof(ResourceId), nameof(FieldName), nameof(LanguageId), IsUnique = true)]
public class ResourceLocalization : EntityBase
{
    /// <summary>
    /// The ID of the resource being localized
    /// </summary>
    [Required]
    public Guid ResourceId { get; set; }

    /// <summary>
    /// The type of resource being localized (e.g., "Course", "Project")
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// The field name being localized (e.g., "Title", "Description")
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The localized content
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The language ID for this localization
    /// </summary>
    [Required]
    public Guid LanguageId { get; set; }

    /// <summary>
    /// The status of this localization
    /// </summary>
    public LocalizationStatus Status { get; set; } = LocalizationStatus.Draft;

    /// <summary>
    /// Navigation property to Language
    /// </summary>
    [ForeignKey(nameof(LanguageId))]
    public virtual Language Language { get; set; } = null!;
}
