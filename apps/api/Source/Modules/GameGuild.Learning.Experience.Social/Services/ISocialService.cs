namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Composite interface for social learning features. Kept for backward compatibility.
/// Prefer injecting the specific interfaces (IReviewService, IWishlistService, etc.) directly.
/// </summary>
public interface ISocialService : IReviewService, IWishlistService, IDiscussionService, IReplyService, ILikeService, IFeedService
{
}

#region DTOs

/// <summary>
/// Course rating statistics
/// </summary>
public record CourseRatingStats(
    Guid CourseId,
    double AverageRating,
    int TotalReviews,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    int FeaturedReviewCount);

/// <summary>
/// DTO for creating a review
/// </summary>
public record CreateReviewRequest(
    Guid CourseId,
    int Rating,
    string? Title = null,
    string? Content = null,
    Guid? EnrollmentId = null);

/// <summary>
/// DTO for creating a discussion
/// </summary>
public record CreateDiscussionRequest(
    Guid CourseId,
    string Title,
    string Content,
    Guid? ContentId = null);

/// <summary>
/// DTO for creating a reply
/// </summary>
public record CreateReplyRequest(
    Guid DiscussionId,
    string Content,
    Guid? ParentReplyId = null);

/// <summary>
/// DTO for wishlist preferences
/// </summary>
public record WishlistPreferencesRequest(
    bool NotifyOnSale,
    bool NotifyOnUpdate);

/// <summary>
/// DTO for course review response
/// </summary>
public record CourseReviewDto(
    Guid Id,
    Guid CourseId,
    Guid UserId,
    int Rating,
    string? Title,
    string? Content,
    bool IsVerifiedPurchase,
    int HelpfulCount,
    bool IsApproved,
    bool IsFeatured,
    DateTime CreatedAt);

/// <summary>
/// DTO for discussion response
/// </summary>
public record CourseDiscussionDto(
    Guid Id,
    Guid CourseId,
    Guid? ContentId,
    Guid AuthorId,
    string Title,
    string Content,
    bool IsPinned,
    bool IsResolved,
    int ReplyCount,
    int ViewCount,
    DateTime? LastActivityAt,
    DateTime CreatedAt);

/// <summary>
/// DTO for discussion reply response
/// </summary>
public record DiscussionReplyDto(
    Guid Id,
    Guid DiscussionId,
    Guid AuthorId,
    Guid? ParentReplyId,
    string Content,
    bool IsAcceptedAnswer,
    int UpvoteCount,
    DateTime CreatedAt);

/// <summary>
/// DTO for wishlist item response
/// </summary>
public record CourseWishlistDto(
    Guid Id,
    Guid CourseId,
    Guid UserId,
    bool NotifyOnSale,
    bool NotifyOnUpdate,
    DateTime CreatedAt);

/// <summary>
/// DTO for course like response
/// </summary>
public record CourseLikeDto(
    Guid Id,
    Guid CourseId,
    Guid UserId,
    DateTime CreatedAt);

/// <summary>
/// DTO for personalized feed item response
/// </summary>
public record PersonalizedFeedItemDto(
    Guid Id,
    FeedItemType ItemType,
    Guid? CourseId,
    Guid? DiscussionId,
    Guid? ReviewId,
    Guid? LearningPathId,
    double RelevanceScore,
    string? Reason,
    bool IsViewed,
    DateTime ExpiresAt,
    DateTime CreatedAt);

#endregion
