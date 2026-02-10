using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Content.Pages;

/// <summary>
///     The type of page — determines its purpose and rendering strategy.
/// </summary>
public enum PageType
{
    /// <summary>Landing / marketing page (home, about, pricing)</summary>
    Landing,

    /// <summary>Legal / policy page (terms, privacy, cookies)</summary>
    Legal,

    /// <summary>Resource listing page (blog index, tutorials index, docs index)</summary>
    ResourceIndex,

    /// <summary>Individual resource page (blog post, tutorial, doc)</summary>
    Resource,

    /// <summary>Custom / generic page</summary>
    Custom,
}

/// <summary>
///     Publication status of a page.
/// </summary>
public enum PageStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>
///     Represents a page on the SaaS website.
///     Holds SEO metadata, OpenGraph data, and optional structured sections.
/// </summary>
[Table("pages")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(PageType))]
[Index(nameof(Status))]
[Index(nameof(Locale))]
[Index(nameof(ParentPageId))]
public class Page : EntityBase
{
    /// <summary>URL slug (unique, used for routing: /about, /pricing, /blog/my-post)</summary>
    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Human-readable page title (also used as &lt;title&gt; fallback)</summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Short description / subtitle</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Page type</summary>
    public PageType PageType { get; set; } = PageType.Landing;

    /// <summary>Publication status</summary>
    public PageStatus Status { get; set; } = PageStatus.Draft;

    /// <summary>BCP-47 locale (e.g. "en-US", "pt-BR"). Null = all locales.</summary>
    [MaxLength(10)]
    public string? Locale { get; set; }

    // ── SEO metadata ──

    /// <summary>SEO &lt;title&gt; override (falls back to Title)</summary>
    [MaxLength(300)]
    public string? MetaTitle { get; set; }

    /// <summary>SEO &lt;meta name="description"&gt;</summary>
    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    /// <summary>SEO &lt;meta name="keywords"&gt; (comma-separated)</summary>
    [MaxLength(500)]
    public string? MetaKeywords { get; set; }

    /// <summary>Canonical URL override</summary>
    [MaxLength(2000)]
    public string? CanonicalUrl { get; set; }

    /// <summary>Robots directive (e.g. "index,follow", "noindex,nofollow")</summary>
    [MaxLength(100)]
    public string? RobotsDirective { get; set; }

    // ── OpenGraph ──

    /// <summary>og:title (falls back to MetaTitle then Title)</summary>
    [MaxLength(300)]
    public string? OgTitle { get; set; }

    /// <summary>og:description</summary>
    [MaxLength(500)]
    public string? OgDescription { get; set; }

    /// <summary>og:image URL</summary>
    [MaxLength(2000)]
    public string? OgImageUrl { get; set; }

    /// <summary>og:type (website, article, product…)</summary>
    [MaxLength(50)]
    public string? OgType { get; set; }

    // ── Twitter Card ──

    /// <summary>twitter:card type (summary, summary_large_image, player…)</summary>
    [MaxLength(50)]
    public string? TwitterCard { get; set; }

    /// <summary>twitter:site handle</summary>
    [MaxLength(100)]
    public string? TwitterSite { get; set; }

    // ── Structured data ──

    /// <summary>JSON-LD structured data (schema.org) stored as JSONB</summary>
    [Column(TypeName = "jsonb")]
    public string? StructuredData { get; set; }

    // ── Content body ──

    /// <summary>Main content body (Markdown / HTML / rich text)</summary>
    [Column(TypeName = "text")]
    public string? Body { get; set; }

    /// <summary>Page-level custom metadata stored as JSONB (hero config, CTA config, etc.)</summary>
    [Column(TypeName = "jsonb")]
    public string? CustomData { get; set; }

    // ── Hierarchy ──

    /// <summary>Parent page ID for nested pages (e.g. /resources/tutorials)</summary>
    public Guid? ParentPageId { get; set; }

    /// <summary>Parent page navigation property</summary>
    [ForeignKey(nameof(ParentPageId))]
    public Page? ParentPage { get; set; }

    /// <summary>Child pages</summary>
    public ICollection<Page> ChildPages { get; set; } = new List<Page>();

    /// <summary>Display order among siblings</summary>
    public int SortOrder { get; set; }

    // ── Sections ──

    /// <summary>Ordered sections within this page</summary>
    public ICollection<PageSection> Sections { get; set; } = new List<PageSection>();

    // ── Publishing ──

    /// <summary>When the page was first published</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Scheduled publish date</summary>
    public DateTime? ScheduledPublishAt { get; set; }

    /// <summary>Who published the page</summary>
    public Guid? PublishedBy { get; set; }
}
