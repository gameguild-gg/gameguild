namespace GameGuild.Modules.Contents.Models;

/// <summary>
/// Represents content in the content management system with full lifecycle support
/// </summary>
[Table("Contents")]
[Index(nameof(TenantId))]
[Index(nameof(AuthorId))]
[Index(nameof(Type))]
[Index(nameof(Status))]
[Index(nameof(Visibility))]
[Index(nameof(PublishedAt))]
[Index(nameof(ScheduledPublishAt))]
[Index(nameof(Slug), IsUnique = true)]
public class Content
{
    /// <summary>
    /// Unique identifier for the content
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant identifier for multi-tenancy
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Author user identifier
    /// </summary>
    [Required]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Content title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for the content
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Content summary or excerpt
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Main content body (HTML or Markdown)
    /// </summary>
    [Column(TypeName = "text")]
    public string? Body { get; set; }

    /// <summary>
    /// Type of content
    /// </summary>
    [Required]
    public ContentType Type { get; set; }

    /// <summary>
    /// Current status of the content
    /// </summary>
    [Required]
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>
    /// Visibility/access level for the content
    /// </summary>
    [Required]
    public AccessLevel Visibility { get; set; } = AccessLevel.Private;

    /// <summary>
    /// Featured image or thumbnail URL
    /// </summary>
    [MaxLength(1000)]
    public string? FeaturedImageUrl { get; set; }

    /// <summary>
    /// License type for the content
    /// </summary>
    [MaxLength(200)]
    public string? License { get; set; }

    /// <summary>
    /// Copyright information
    /// </summary>
    [MaxLength(500)]
    public string? Copyright { get; set; }

    /// <summary>
    /// Tags associated with the content (comma-separated)
    /// </summary>
    [MaxLength(1000)]
    public string? Tags { get; set; }

    /// <summary>
    /// Categories assigned to the content (comma-separated IDs)
    /// </summary>
    [MaxLength(500)]
    public string? CategoryIds { get; set; }

    /// <summary>
    /// When the content was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the content was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the content was published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// When the content is scheduled to be published
    /// </summary>
    public DateTime? ScheduledPublishAt { get; set; }

    /// <summary>
    /// When the content was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// When the content was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Current version number
    /// </summary>
    [Required]
    public int Version { get; set; } = 1;

    /// <summary>
    /// View count for analytics
    /// </summary>
    [Required]
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Like count for engagement
    /// </summary>
    [Required]
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// Comment count for engagement
    /// </summary>
    [Required]
    public int CommentCount { get; set; } = 0;

    /// <summary>
    /// Share count for engagement
    /// </summary>
    [Required]
    public int ShareCount { get; set; } = 0;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// SEO title override
    /// </summary>
    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    /// <summary>
    /// SEO meta description
    /// </summary>
    [MaxLength(500)]
    public string? SeoDescription { get; set; }

    /// <summary>
    /// SEO keywords (comma-separated)
    /// </summary>
    [MaxLength(500)]
    public string? SeoKeywords { get; set; }

    /// <summary>
    /// Canonical URL for SEO
    /// </summary>
    [MaxLength(1000)]
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Language code for localization (e.g., en-US, pt-BR)
    /// </summary>
    [MaxLength(10)]
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Parent content ID for translations (if this is a translation)
    /// </summary>
    public Guid? ParentContentId { get; set; }

    /// <summary>
    /// User who last reviewed the content
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// When the content was last reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Review comments or notes
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// User who approved the content
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// When the content was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Approval comments or notes
    /// </summary>
    [MaxLength(2000)]
    public string? ApprovalNotes { get; set; }

    // Navigation properties
    public virtual ICollection<ContentVersion> Versions { get; set; } = new List<ContentVersion>();
    public virtual ICollection<ContentTranslation> Translations { get; set; } = new List<ContentTranslation>();
}
