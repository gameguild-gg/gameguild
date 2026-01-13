using GameGuild.Entities;

namespace GameGuild.Learning.Experience.Social;

/// <summary>
/// Represents a course rating and review by a student
/// </summary>
public class CourseReview : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? EnrollmentId { get; private set; }
    public int Rating { get; private set; } // 1-5
    public string? Title { get; private set; }
    public string? Content { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulCount { get; private set; }
    public bool IsApproved { get; private set; }
    public bool IsFeatured { get; private set; }

    private CourseReview() { } // EF Core

    public static CourseReview Create(
        Guid courseId,
        Guid userId,
        int rating,
        string? title = null,
        string? content = null,
        Guid? enrollmentId = null)
    {
        return new CourseReview
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            EnrollmentId = enrollmentId,
            Rating = Math.Clamp(rating, 1, 5),
            Title = title,
            Content = content,
            IsVerifiedPurchase = enrollmentId.HasValue,
            HelpfulCount = 0,
            IsApproved = false,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkHelpful() => HelpfulCount++;
    public void Approve() { IsApproved = true; UpdatedAt = DateTime.UtcNow; }
    public void Feature() { IsFeatured = true; UpdatedAt = DateTime.UtcNow; }
}

/// <summary>
/// Represents a user's wishlist item for a course
/// </summary>
public class CourseWishlist : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public bool NotifyOnSale { get; private set; }
    public bool NotifyOnUpdate { get; private set; }

    private CourseWishlist() { } // EF Core

    public static CourseWishlist Create(Guid courseId, Guid userId)
    {
        return new CourseWishlist
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            NotifyOnSale = true,
            NotifyOnUpdate = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents a discussion thread within a course
/// </summary>
public class CourseDiscussion : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid? ContentId { get; private set; } // Optional: linked to specific content
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsPinned { get; private set; }
    public bool IsResolved { get; private set; }
    public int ReplyCount { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    private CourseDiscussion() { } // EF Core

    public static CourseDiscussion Create(
        Guid courseId,
        Guid authorId,
        string title,
        string content,
        Guid? contentId = null)
    {
        return new CourseDiscussion
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            ContentId = contentId,
            AuthorId = authorId,
            Title = title,
            Content = content,
            IsPinned = false,
            IsResolved = false,
            ReplyCount = 0,
            ViewCount = 0,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Pin() { IsPinned = true; UpdatedAt = DateTime.UtcNow; }
    public void Unpin() { IsPinned = false; UpdatedAt = DateTime.UtcNow; }
    public void MarkResolved() { IsResolved = true; UpdatedAt = DateTime.UtcNow; }
    public void IncrementViews() => ViewCount++;
    public void IncrementReplies() { ReplyCount++; LastActivityAt = DateTime.UtcNow; }
}

/// <summary>
/// Represents a reply in a course discussion
/// </summary>
public class DiscussionReply : EntityBase
{
    public Guid DiscussionId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentReplyId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsAcceptedAnswer { get; private set; }
    public int UpvoteCount { get; private set; }

    private DiscussionReply() { } // EF Core

    public static DiscussionReply Create(Guid discussionId, Guid authorId, string content, Guid? parentReplyId = null)
    {
        return new DiscussionReply
        {
            Id = Guid.NewGuid(),
            DiscussionId = discussionId,
            AuthorId = authorId,
            ParentReplyId = parentReplyId,
            Content = content,
            IsAcceptedAnswer = false,
            UpvoteCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AcceptAsAnswer() { IsAcceptedAnswer = true; UpdatedAt = DateTime.UtcNow; }
    public void Upvote() => UpvoteCount++;
}
