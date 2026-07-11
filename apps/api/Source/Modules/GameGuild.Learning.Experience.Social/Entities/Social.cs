
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
            IsFeatured = false
        };
    }

    public void MarkHelpful() => HelpfulCount++;
    public void Approve() => SetModeration(true, IsFeatured);
    public void Feature() => SetModeration(IsApproved, true);

    public void SetModeration(bool isApproved, bool isFeatured)
    {
        IsApproved = isApproved;
        IsFeatured = isFeatured;
        UpdatedAt = SystemClock.UtcNow;
    }
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
            NotifyOnUpdate = false
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
            LastActivityAt = SystemClock.UtcNow
        };
    }

    public void Pin() { IsPinned = true; UpdatedAt = SystemClock.UtcNow; }
    public void Unpin() { IsPinned = false; UpdatedAt = SystemClock.UtcNow; }
    public void MarkResolved() { IsResolved = true; UpdatedAt = SystemClock.UtcNow; }
    public void IncrementViews() => ViewCount++;
    public void IncrementReplies() { ReplyCount++; LastActivityAt = SystemClock.UtcNow; }
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
            UpvoteCount = 0
        };
    }

    public void AcceptAsAnswer() { IsAcceptedAnswer = true; UpdatedAt = SystemClock.UtcNow; }
    public void Upvote() => UpvoteCount++;
}

/// <summary>
/// Represents a like/upvote on a course (Social Proof feature)
/// </summary>
public class CourseLike : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public new Guid? TenantId { get; private set; }

    private CourseLike() { } // EF Core

    public static CourseLike Create(Guid courseId, Guid userId, Guid? tenantId = null)
    {
        return new CourseLike
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            TenantId = tenantId
        };
    }
}

/// <summary>
/// Represents a personalized feed item for a user
/// </summary>
public class PersonalizedFeedItem : EntityBase
{
    public Guid UserId { get; private set; }
    public new Guid? TenantId { get; private set; }
    public FeedItemType ItemType { get; private set; }
    public Guid? CourseId { get; private set; }
    public Guid? DiscussionId { get; private set; }
    public Guid? ReviewId { get; private set; }
    public Guid? LearningPathId { get; private set; }
    public double RelevanceScore { get; private set; }
    public string? Reason { get; private set; }
    public bool IsViewed { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private PersonalizedFeedItem() { } // EF Core

    public static PersonalizedFeedItem Create(
        Guid userId,
        FeedItemType itemType,
        Guid? tenantId = null,
        Guid? courseId = null,
        Guid? discussionId = null,
        Guid? reviewId = null,
        Guid? learningPathId = null,
        double relevanceScore = 0.5,
        string? reason = null,
        int expiresInDays = 7)
    {
        return new PersonalizedFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            ItemType = itemType,
            CourseId = courseId,
            DiscussionId = discussionId,
            ReviewId = reviewId,
            LearningPathId = learningPathId,
            RelevanceScore = Math.Clamp(relevanceScore, 0.0, 1.0),
            Reason = reason,
            IsViewed = false,
            IsDismissed = false,
            ExpiresAt = SystemClock.UtcNow.AddDays(expiresInDays)
        };
    }

    public void MarkViewed() { IsViewed = true; UpdatedAt = SystemClock.UtcNow; }
    public void Dismiss() { IsDismissed = true; UpdatedAt = SystemClock.UtcNow; }
}

/// <summary>
/// Types of items that can appear in a personalized feed
/// </summary>
public enum FeedItemType
{
    NewCourse = 0,
    PopularCourse = 1,
    TrendingDiscussion = 2,
    FeaturedReview = 3,
    LearningPathSuggestion = 4,
    CourseUpdate = 5,
    InstructorActivity = 6,
    PeerActivity = 7,
    AchievementUnlocked = 8,
    SkillMilestone = 9
}
