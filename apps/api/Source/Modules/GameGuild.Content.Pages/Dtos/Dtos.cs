namespace GameGuild.Content.Pages;

// ──────────────────────────── Page DTOs ────────────────────────────

public record PageDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string PageType { get; init; } = nameof(Pages.PageType.Landing);
    public string Status { get; init; } = nameof(PageStatus.Draft);
    public string? Locale { get; init; }

    // SEO
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? RobotsDirective { get; init; }

    // OpenGraph
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? OgType { get; init; }

    // Twitter
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }

    // Content
    public string? StructuredData { get; init; }
    public string? Body { get; init; }
    public string? CustomData { get; init; }

    // Hierarchy
    public Guid? ParentPageId { get; init; }
    public int SortOrder { get; init; }

    // Sections
    public List<PageSectionDto> Sections { get; init; } = [];

    // Publishing
    public DateTime? PublishedAt { get; init; }
    public DateTime? ScheduledPublishAt { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreatePageDto
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PageType PageType { get; init; } = Pages.PageType.Landing;
    public string? Locale { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? RobotsDirective { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? OgType { get; init; }
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }
    public string? StructuredData { get; init; }
    public string? Body { get; init; }
    public string? CustomData { get; init; }
    public Guid? ParentPageId { get; init; }
    public int SortOrder { get; init; }
}

public record UpdatePageDto
{
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public PageType? PageType { get; init; }
    public PageStatus? Status { get; init; }
    public string? Locale { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? RobotsDirective { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? OgType { get; init; }
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }
    public string? StructuredData { get; init; }
    public string? Body { get; init; }
    public string? CustomData { get; init; }
    public Guid? ParentPageId { get; init; }
    public int? SortOrder { get; init; }
    public DateTime? ScheduledPublishAt { get; init; }
}

// ──────────────────────────── PageSection DTOs ────────────────────────────

public record PageSectionDto
{
    public Guid Id { get; init; }
    public Guid PageId { get; init; }
    public string SectionType { get; init; } = nameof(Pages.SectionType.Custom);
    public string? Heading { get; init; }
    public string? Subheading { get; init; }
    public string? Data { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; }
    public string? CssClasses { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreatePageSectionDto
{
    public SectionType SectionType { get; init; }
    public string? Heading { get; init; }
    public string? Subheading { get; init; }
    public string? Data { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; } = true;
    public string? CssClasses { get; init; }
}

public record UpdatePageSectionDto
{
    public SectionType? SectionType { get; init; }
    public string? Heading { get; init; }
    public string? Subheading { get; init; }
    public string? Data { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsVisible { get; init; }
    public string? CssClasses { get; init; }
}

// ──────────────────────────── ContentResource DTOs ────────────────────────────

public record ContentResourceDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Body { get; init; }
    public string ResourceType { get; init; } = nameof(ContentResourceType.Article);
    public string Status { get; init; } = nameof(ContentResourceStatus.Draft);
    public string? Locale { get; init; }
    public string? CategorySlug { get; init; }
    public string? Tags { get; init; }
    public Guid? AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ExternalUrl { get; init; }
    public Guid? LinkedEntityId { get; init; }
    public string? LinkedEntityType { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? StructuredData { get; init; }
    public int? ReadingTimeMinutes { get; init; }
    public long ViewCount { get; init; }
    public bool IsFeatured { get; init; }
    public int SortOrder { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? ScheduledPublishAt { get; init; }
    public string? CustomData { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateContentResourceDto
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Body { get; init; }
    public ContentResourceType ResourceType { get; init; } = ContentResourceType.Article;
    public string? Locale { get; init; }
    public string? CategorySlug { get; init; }
    public string? Tags { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ExternalUrl { get; init; }
    public Guid? LinkedEntityId { get; init; }
    public string? LinkedEntityType { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? StructuredData { get; init; }
    public int? ReadingTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
    public int SortOrder { get; init; }
    public string? CustomData { get; init; }
}

public record UpdateContentResourceDto
{
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public string? Body { get; init; }
    public ContentResourceType? ResourceType { get; init; }
    public ContentResourceStatus? Status { get; init; }
    public string? Locale { get; init; }
    public string? CategorySlug { get; init; }
    public string? Tags { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ExternalUrl { get; init; }
    public Guid? LinkedEntityId { get; init; }
    public string? LinkedEntityType { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? StructuredData { get; init; }
    public int? ReadingTimeMinutes { get; init; }
    public bool? IsFeatured { get; init; }
    public int? SortOrder { get; init; }
    public DateTime? ScheduledPublishAt { get; init; }
    public string? CustomData { get; init; }
}

// ──────────────────────────── OpenGraph DTO ────────────────────────────

/// <summary>
///     Resolved OpenGraph / SEO metadata for a given slug — returned by the public OG endpoint.
/// </summary>
public record OpenGraphMetadataDto
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }
    public string? OgType { get; init; }
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? RobotsDirective { get; init; }
    public string? StructuredData { get; init; }
}
