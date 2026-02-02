using GameGuild.Models;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for social learning features: reviews, discussions, wishlists, likes, and personalized feed
/// </summary>
public interface ISocialService
{
    #region Course Reviews

    /// <summary>
    /// Creates a new course review
    /// </summary>
    Task<Result<CourseReview>> CreateReviewAsync(
        Guid courseId,
        Guid userId,
        int rating,
        string? title = null,
        string? content = null,
        Guid? enrollmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    Task<Result<CourseReview>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a course
    /// </summary>
    Task<Result<IEnumerable<CourseReview>>> GetCourseReviewsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool approvedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews by a user
    /// </summary>
    Task<Result<IEnumerable<CourseReview>>> GetUserReviewsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a review (admin action)
    /// </summary>
    Task<Result<CourseReview>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Features a review (admin action)
    /// </summary>
    Task<Result<CourseReview>> FeatureReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a review as helpful
    /// </summary>
    Task<Result<CourseReview>> MarkReviewHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a review
    /// </summary>
    Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average rating for a course
    /// </summary>
    Task<Result<CourseRatingStats>> GetCourseRatingStatsAsync(Guid courseId, CancellationToken cancellationToken = default);

    #endregion

    #region Course Wishlist (Bookmarks)

    /// <summary>
    /// Adds a course to user's wishlist
    /// </summary>
    Task<Result<CourseWishlist>> AddToWishlistAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale = true,
        bool notifyOnUpdate = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a course from user's wishlist
    /// </summary>
    Task<Result<bool>> RemoveFromWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's wishlist
    /// </summary>
    Task<Result<IEnumerable<CourseWishlist>>> GetUserWishlistAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a course is in user's wishlist
    /// </summary>
    Task<Result<bool>> IsInWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates wishlist notification preferences
    /// </summary>
    Task<Result<CourseWishlist>> UpdateWishlistPreferencesAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale,
        bool notifyOnUpdate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Course Discussions

    /// <summary>
    /// Creates a new discussion thread
    /// </summary>
    Task<Result<CourseDiscussion>> CreateDiscussionAsync(
        Guid courseId,
        Guid authorId,
        string title,
        string content,
        Guid? contentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a discussion by ID
    /// </summary>
    Task<Result<CourseDiscussion>> GetDiscussionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discussions for a course
    /// </summary>
    Task<Result<IEnumerable<CourseDiscussion>>> GetCourseDiscussionsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool pinnedFirst = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discussions for specific content within a course
    /// </summary>
    Task<Result<IEnumerable<CourseDiscussion>>> GetContentDiscussionsAsync(
        Guid courseId,
        Guid contentId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pins a discussion (instructor/admin action)
    /// </summary>
    Task<Result<CourseDiscussion>> PinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpins a discussion
    /// </summary>
    Task<Result<CourseDiscussion>> UnpinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a discussion as resolved
    /// </summary>
    Task<Result<CourseDiscussion>> MarkDiscussionResolvedAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a discussion
    /// </summary>
    Task<Result<bool>> DeleteDiscussionAsync(Guid discussionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments discussion view count
    /// </summary>
    Task<Result<CourseDiscussion>> IncrementDiscussionViewsAsync(Guid discussionId, CancellationToken cancellationToken = default);

    #endregion

    #region Discussion Replies

    /// <summary>
    /// Creates a reply to a discussion
    /// </summary>
    Task<Result<DiscussionReply>> CreateReplyAsync(
        Guid discussionId,
        Guid authorId,
        string content,
        Guid? parentReplyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replies for a discussion
    /// </summary>
    Task<Result<IEnumerable<DiscussionReply>>> GetDiscussionRepliesAsync(
        Guid discussionId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a reply as the answer
    /// </summary>
    Task<Result<DiscussionReply>> AcceptReplyAsAnswerAsync(
        Guid replyId,
        Guid discussionAuthorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upvotes a reply
    /// </summary>
    Task<Result<DiscussionReply>> UpvoteReplyAsync(Guid replyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a reply
    /// </summary>
    Task<Result<bool>> DeleteReplyAsync(Guid replyId, Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Course Likes (Social Proof)

    /// <summary>
    /// Likes a course
    /// </summary>
    Task<Result<CourseLike>> LikeCourseAsync(Guid courseId, Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlikes a course
    /// </summary>
    Task<Result<bool>> UnlikeCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has liked a course
    /// </summary>
    Task<Result<bool>> HasUserLikedCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the like count for a course
    /// </summary>
    Task<Result<int>> GetCourseLikeCountAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all courses liked by a user
    /// </summary>
    Task<Result<IEnumerable<CourseLike>>> GetUserLikedCoursesAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    #endregion

    #region Personalized Feed

    /// <summary>
    /// Gets personalized feed for a user
    /// </summary>
    Task<Result<IEnumerable<PersonalizedFeedItem>>> GetPersonalizedFeedAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        FeedItemType? filterByType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates new feed items for a user
    /// </summary>
    Task<Result<int>> GenerateFeedItemsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a feed item as viewed
    /// </summary>
    Task<Result<PersonalizedFeedItem>> MarkFeedItemViewedAsync(Guid feedItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a feed item
    /// </summary>
    Task<Result<PersonalizedFeedItem>> DismissFeedItemAsync(Guid feedItemId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears expired feed items
    /// </summary>
    Task<Result<int>> ClearExpiredFeedItemsAsync(CancellationToken cancellationToken = default);

    #endregion
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
