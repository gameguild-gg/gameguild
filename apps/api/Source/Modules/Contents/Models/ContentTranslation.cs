using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Source.Modules.Contents.Models;

/// <summary>
/// Represents a translation of content for localization
/// </summary>
[Table("ContentTranslations")]
[Index(nameof(ContentId))]
[Index(nameof(LanguageCode))]
[Index(nameof(ContentId), nameof(LanguageCode), IsUnique = true)]
public class ContentTranslation
{
    /// <summary>
    /// Unique identifier for the content translation
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent content
    /// </summary>
    [Required]
    public Guid ContentId { get; set; }

    /// <summary>
    /// Language code (e.g., en-US, pt-BR, es-ES)
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Translated title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Translated slug
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Translated summary
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Translated body
    /// </summary>
    [Column(TypeName = "text")]
    public string? Body { get; set; }

    /// <summary>
    /// Translated SEO title
    /// </summary>
    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    /// <summary>
    /// Translated SEO description
    /// </summary>
    [MaxLength(500)]
    public string? SeoDescription { get; set; }

    /// <summary>
    /// Translated SEO keywords
    /// </summary>
    [MaxLength(500)]
    public string? SeoKeywords { get; set; }

    /// <summary>
    /// User who created the translation
    /// </summary>
    [Required]
    public Guid TranslatedBy { get; set; }

    /// <summary>
    /// When the translation was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the translation was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the translation is approved
    /// </summary>
    [Required]
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// User who approved the translation
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// When the translation was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    // Navigation property
    public virtual Content Content { get; set; } = null!;
}
