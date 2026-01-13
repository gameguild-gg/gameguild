using GameGuild.Entities;

namespace GameGuild.Social.Blog;

/// <summary>
/// Represents a long-form blog article
/// </summary>
public class BlogPost : EntityBase
{
    public Guid AuthorId { get; private set; }
    public Guid? TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? CoverImageUrl { get; private set; }
    public BlogPostStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool AllowComments { get; private set; }
    public int ViewsCount { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public int ReadTimeMinutes { get; private set; }

    private BlogPost() { } // EF Core

    public static BlogPost Create(Guid authorId, string title, string slug, string content, Guid? tenantId = null)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            TenantId = tenantId,
            Title = title,
            Slug = slug,
            Content = content,
            Status = BlogPostStatus.Draft,
            AllowComments = true,
            ViewsCount = 0,
            LikesCount = 0,
            CommentsCount = 0,
            ReadTimeMinutes = CalculateReadTime(content),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Publish()
    {
        Status = BlogPostStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        Status = BlogPostStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Feature() { IsFeatured = true; UpdatedAt = DateTime.UtcNow; }
    public void Unfeature() { IsFeatured = false; UpdatedAt = DateTime.UtcNow; }
    public void IncrementViews() => ViewsCount++;
    public void IncrementLikes() => LikesCount++;
    public void DecrementLikes() { if (LikesCount > 0) LikesCount--; }
    public void IncrementComments() => CommentsCount++;
    public void DecrementComments() { if (CommentsCount > 0) CommentsCount--; }

    private static int CalculateReadTime(string content)
    {
        const int wordsPerMinute = 200;
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)wordsPerMinute));
    }
}

public enum BlogPostStatus
{
    Draft,
    Published,
    Archived
}
