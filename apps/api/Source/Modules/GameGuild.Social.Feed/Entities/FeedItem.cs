
namespace GameGuild.Social.Feed;

/// <summary>
/// Represents a cached feed item for a user's personalized feed
/// </summary>
public class FeedItem : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid ContentId { get; private set; }
    public FeedContentType ContentType { get; private set; }
    public Guid AuthorId { get; private set; }
    public double RelevanceScore { get; private set; }
    public FeedItemReason Reason { get; private set; }
    public bool IsRead { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTime ContentCreatedAt { get; private set; }

    private FeedItem() { } // EF Core

    public static FeedItem Create(
        Guid userId,
        Guid contentId,
        FeedContentType contentType,
        Guid authorId,
        FeedItemReason reason,
        DateTime contentCreatedAt,
        double relevanceScore = 1.0)
    {
        return new FeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentId = contentId,
            ContentType = contentType,
            AuthorId = authorId,
            Reason = reason,
            ContentCreatedAt = contentCreatedAt,
            RelevanceScore = relevanceScore,
            IsRead = false,
            IsHidden = false
        };
    }

    public void MarkRead()
    {
        IsRead = true;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Hide()
    {
        IsHidden = true;
        UpdatedAt = SystemClock.UtcNow;
    }
}

public enum FeedContentType
{
    Post,
    BlogPost,
    CourseReview,
    ProjectUpdate,
    Achievement,
    CourseCompletion
}

public enum FeedItemReason
{
    Following,
    Trending,
    Recommended,
    Mentioned,
    Replied,
    Liked,
    InNetwork
}
