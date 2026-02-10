using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Content.Pages;

/// <summary>
///     The type of section — drives frontend rendering.
/// </summary>
public enum SectionType
{
    /// <summary>Hero / banner section</summary>
    Hero,

    /// <summary>Feature grid / feature cards</summary>
    Features,

    /// <summary>Testimonials / social proof</summary>
    Testimonials,

    /// <summary>Pricing table</summary>
    Pricing,

    /// <summary>Call-to-action block</summary>
    CallToAction,

    /// <summary>FAQ / accordion</summary>
    Faq,

    /// <summary>Rich text / markdown content block</summary>
    RichText,

    /// <summary>Image / video gallery</summary>
    Gallery,

    /// <summary>Statistics / metrics showcase</summary>
    Stats,

    /// <summary>Team members grid</summary>
    Team,

    /// <summary>Partner / integration logos</summary>
    LogoCloud,

    /// <summary>Newsletter signup</summary>
    Newsletter,

    /// <summary>Contact form</summary>
    Contact,

    /// <summary>Resource cards (courses, tutorials, etc.)</summary>
    ResourceCards,

    /// <summary>Custom / freeform section</summary>
    Custom,
}

/// <summary>
///     Represents an ordered section within a <see cref="Page"/>.
///     Each section has a type that the frontend uses to pick the right rendering component,
///     and a JSONB payload for the section-specific data (headline, items, images, etc.).
/// </summary>
[Table("page_sections")]
[Index(nameof(PageId))]
[Index(nameof(PageId), nameof(SortOrder))]
[Index(nameof(SectionType))]
public class PageSection : EntityBase
{
    /// <summary>Parent page ID</summary>
    public Guid PageId { get; set; }

    /// <summary>Parent page navigation property</summary>
    [ForeignKey(nameof(PageId))]
    public Page Page { get; set; } = null!;

    /// <summary>Section type — drives frontend component selection</summary>
    public SectionType SectionType { get; set; }

    /// <summary>Optional heading for this section</summary>
    [MaxLength(300)]
    public string? Heading { get; set; }

    /// <summary>Optional subheading</summary>
    [MaxLength(500)]
    public string? Subheading { get; set; }

    /// <summary>
    ///     Section payload as JSONB — content varies by SectionType.
    ///     E.g. for Hero: { "backgroundImage": "...", "ctaText": "Get Started", "ctaUrl": "/sign-up" }
    ///     E.g. for Features: { "items": [{ "icon": "...", "title": "...", "description": "..." }, ...] }
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Data { get; set; }

    /// <summary>Display order within the parent page</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this section is visible (allows hiding without deleting)</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Optional CSS class names or Tailwind utility classes for styling overrides</summary>
    [MaxLength(500)]
    public string? CssClasses { get; set; }
}
