using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Content.Pages;

/// <summary>
///     The type of content resource.
/// </summary>
public enum ContentResourceType
{
    /// <summary>Blog / article</summary>
    Article,

    /// <summary>Tutorial (step-by-step guide)</summary>
    Tutorial,

    /// <summary>Documentation page</summary>
    Documentation,

    /// <summary>Video content</summary>
    Video,

    /// <summary>Downloadable asset (PDF, template, etc.)</summary>
    Download,

    /// <summary>External link / curated resource</summary>
    ExternalLink,

    /// <summary>Course (links to a Learning.Courses program)</summary>
    Course,

    /// <summary>Custom / other</summary>
    Custom,
}

/// <summary>
///     Publication status for a content resource.
/// </summary>
public enum ContentResourceStatus
{
    Draft,
    InReview,
    Published,
    Archived,
}

/// <summary>
///     Represents a standalone content resource (blog post, tutorial, doc, video, etc.)
///     that can appear in resource listing pages or be linked from page sections.
///     This is the "Resources > Contents > Courses" hierarchy entity.
/// </summary>
[Table("content_resources")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(ResourceType))]
[Index(nameof(Status))]
[Index(nameof(Locale))]
[Index(nameof(CategorySlug))]
[Index(nameof(AuthorId))]
[Index(nameof(PublishedAt))]
[Index(nameof(IsFeatured))]
public class ContentResource : EntityBase
{
    /// <summary>URL-safe slug (unique)</summary>
    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Resource title</summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Short summary / excerpt (shown in cards, search results, RSS)</summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    /// <summary>Main body content (Markdown / HTML)</summary>
    [Column(TypeName = "text")]
    public string? Body { get; set; }

    /// <summary>Resource type</summary>
    public ContentResourceType ResourceType { get; set; } = ContentResourceType.Article;

    /// <summary>Publication status</summary>
    public ContentResourceStatus Status { get; set; } = ContentResourceStatus.Draft;

    /// <summary>BCP-47 locale</summary>
    [MaxLength(10)]
    public string? Locale { get; set; }

    // ── Categorisation ──

    /// <summary>Category slug (e.g. "game-design", "programming", "art")</summary>
    [MaxLength(200)]
    public string? CategorySlug { get; set; }

    /// <summary>Comma-separated tags</summary>
    [MaxLength(1000)]
    public string? Tags { get; set; }

    // ── Authorship ──

    /// <summary>Author user ID</summary>
    public Guid? AuthorId { get; set; }

    /// <summary>Author display name (denormalized for performance)</summary>
    [MaxLength(200)]
    public string? AuthorName { get; set; }

    // ── Media ──

    /// <summary>Cover / thumbnail image URL</summary>
    [MaxLength(2000)]
    public string? CoverImageUrl { get; set; }

    /// <summary>Video embed URL (for Video type)</summary>
    [MaxLength(2000)]
    public string? VideoUrl { get; set; }

    /// <summary>Download URL / asset URL (for Download type)</summary>
    [MaxLength(2000)]
    public string? DownloadUrl { get; set; }

    /// <summary>External URL (for ExternalLink type)</summary>
    [MaxLength(2000)]
    public string? ExternalUrl { get; set; }

    // ── Cross-reference ──

    /// <summary>Linked entity ID (e.g., Course program ID for Course type)</summary>
    public Guid? LinkedEntityId { get; set; }

    /// <summary>Linked entity type name (e.g., "Program")</summary>
    [MaxLength(100)]
    public string? LinkedEntityType { get; set; }

    // ── SEO / OpenGraph (shared with Page but duplicated for standalone resource pages) ──

    /// <summary>SEO &lt;title&gt; override</summary>
    [MaxLength(300)]
    public string? MetaTitle { get; set; }

    /// <summary>SEO meta description override</summary>
    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    /// <summary>og:image URL override</summary>
    [MaxLength(2000)]
    public string? OgImageUrl { get; set; }

    /// <summary>JSON-LD structured data</summary>
    [Column(TypeName = "jsonb")]
    public string? StructuredData { get; set; }

    // ── Engagement ──

    /// <summary>Estimated reading time in minutes</summary>
    public int? ReadingTimeMinutes { get; set; }

    /// <summary>View count (denormalized for performance)</summary>
    public long ViewCount { get; set; }

    /// <summary>Is this resource featured / pinned?</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Display order for featured resources</summary>
    public int SortOrder { get; set; }

    // ── Publishing ──

    /// <summary>When first published</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Scheduled publish date</summary>
    public DateTime? ScheduledPublishAt { get; set; }

    /// <summary>Who published</summary>
    public Guid? PublishedBy { get; set; }

    // ── Custom data ──

    /// <summary>Arbitrary metadata as JSONB</summary>
    [Column(TypeName = "jsonb")]
    public string? CustomData { get; set; }
}
