namespace GameGuild.Modules.Contents.Models;

/// <summary>
/// Represents a version of content for revision history
/// </summary>
[Table("ContentVersions")]
[Index(nameof(ContentId))]
[Index(nameof(Version))]
[Index(nameof(CreatedAt))]
public class ContentVersion
{
    /// <summary>
    /// Unique identifier for the content version
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent content
    /// </summary>
    [Required]
    public Guid ContentId { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    [Required]
    public int Version { get; set; }

    /// <summary>
    /// Content title at this version
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content summary at this version
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Content body at this version
    /// </summary>
    [Column(TypeName = "text")]
    public string? Body { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Change notes or description
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Metadata snapshot at this version
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    // Navigation property
    public virtual Content Content { get; set; } = null!;
}
