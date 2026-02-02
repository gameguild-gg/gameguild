using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for social learning features
/// </summary>
public class SocialService : ISocialService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SocialService> _logger;

    public SocialService(
        IApplicationDbContext context,
        ILogger<SocialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Course Reviews

    public async Task<Result<CourseReview>> CreateReviewAsync(
        Guid courseId,
        Guid userId,
        int rating,
        string? title = null,
        string? content = null,
        Guid? enrollmentId = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user already reviewed this course
        var existingReview = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId, cancellationToken);

        if (existingReview != null)
        {
            return Result.Failure<CourseReview>(Error.Failure("Review.AlreadyExists", "You have already reviewed this course"));
        }

        var review = CourseReview.Create(courseId, userId, rating, title, content, enrollmentId);
        _context.Set<CourseReview>().Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review created for course {CourseId} by user {UserId} with rating {Rating}", courseId, userId, rating);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {id} not found"));
        }

        return Result.Success(review);
    }

    public async Task<Result<IEnumerable<CourseReview>>> GetCourseReviewsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool approvedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CourseReview>()
            .Where(r => r.CourseId == courseId);

        if (approvedOnly)
        {
            query = query.Where(r => r.IsApproved);
        }

        var reviews = await query
            .OrderByDescending(r => r.IsFeatured)
            .ThenByDescending(r => r.HelpfulCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseReview>>(reviews);
    }

    public async Task<Result<IEnumerable<CourseReview>>> GetUserReviewsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<CourseReview>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseReview>>(reviews);
    }

    public async Task<Result<CourseReview>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.Approve();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} approved", reviewId);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> FeatureReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.Feature();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} featured", reviewId);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> MarkReviewHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.MarkHelpful();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(review);
    }

    public async Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            return Result.Failure<bool>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        if (review.UserId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Review.Unauthorized", "You can only delete your own reviews"));
        }

        _context.Set<CourseReview>().Remove(review);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} deleted by user {UserId}", reviewId, userId);
        return Result.Success(true);
    }

    public async Task<Result<CourseRatingStats>> GetCourseRatingStatsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<CourseReview>()
            .Where(r => r.CourseId == courseId && r.IsApproved)
            .ToListAsync(cancellationToken);

        if (!reviews.Any())
        {
            return Result.Success(new CourseRatingStats(courseId, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        var stats = new CourseRatingStats(
            courseId,
            reviews.Average(r => r.Rating),
            reviews.Count,
            reviews.Count(r => r.Rating == 5),
            reviews.Count(r => r.Rating == 4),
            reviews.Count(r => r.Rating == 3),
            reviews.Count(r => r.Rating == 2),
            reviews.Count(r => r.Rating == 1),
            reviews.Count(r => r.IsFeatured));

        return Result.Success(stats);
    }

    #endregion

    #region Course Wishlist (Bookmarks)

    public async Task<Result<CourseWishlist>> AddToWishlistAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale = true,
        bool notifyOnUpdate = false,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken);

        if (existing != null)
        {
            return Result.Failure<CourseWishlist>(Error.Failure("Wishlist.AlreadyExists", "This course is already in your wishlist"));
        }

        var wishlistItem = CourseWishlist.Create(courseId, userId);
        _context.Set<CourseWishlist>().Add(wishlistItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Course {CourseId} added to wishlist for user {UserId}", courseId, userId);
        return Result.Success(wishlistItem);
    }

    public async Task<Result<bool>> RemoveFromWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var wishlistItem = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken);

        if (wishlistItem == null)
        {
            return Result.Failure<bool>(Error.NotFound("Wishlist.NotFound", "This course is not in your wishlist"));
        }

        _context.Set<CourseWishlist>().Remove(wishlistItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Course {CourseId} removed from wishlist for user {UserId}", courseId, userId);
        return Result.Success(true);
    }

    public async Task<Result<IEnumerable<CourseWishlist>>> GetUserWishlistAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.Set<CourseWishlist>()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseWishlist>>(items);
    }

    public async Task<Result<bool>> IsInWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<CourseWishlist>()
            .AnyAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken);

        return Result.Success(exists);
    }

    public async Task<Result<CourseWishlist>> UpdateWishlistPreferencesAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale,
        bool notifyOnUpdate,
        CancellationToken cancellationToken = default)
    {
        var wishlistItem = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken);

        if (wishlistItem == null)
        {
            return Result.Failure<CourseWishlist>(Error.NotFound("Wishlist.NotFound", "This course is not in your wishlist"));
        }

        // Note: Need to add setter methods to the entity for these properties
        // For now, return the existing item
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(wishlistItem);
    }

    #endregion

    #region Course Discussions

    public async Task<Result<CourseDiscussion>> CreateDiscussionAsync(
        Guid courseId,
        Guid authorId,
        string title,
        string content,
        Guid? contentId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = CourseDiscussion.Create(courseId, authorId, title, content, contentId);
        _context.Set<CourseDiscussion>().Add(discussion);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discussion created for course {CourseId} by user {AuthorId}: {Title}", courseId, authorId, title);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> GetDiscussionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {id} not found"));
        }

        return Result.Success(discussion);
    }

    public async Task<Result<IEnumerable<CourseDiscussion>>> GetCourseDiscussionsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool pinnedFirst = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CourseDiscussion>()
            .Where(d => d.CourseId == courseId);

        if (pinnedFirst)
        {
            query = query.OrderByDescending(d => d.IsPinned)
                .ThenByDescending(d => d.LastActivityAt);
        }
        else
        {
            query = query.OrderByDescending(d => d.LastActivityAt);
        }

        var discussions = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseDiscussion>>(discussions);
    }

    public async Task<Result<IEnumerable<CourseDiscussion>>> GetContentDiscussionsAsync(
        Guid courseId,
        Guid contentId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var discussions = await _context.Set<CourseDiscussion>()
            .Where(d => d.CourseId == courseId && d.ContentId == contentId)
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.LastActivityAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseDiscussion>>(discussions);
    }

    public async Task<Result<CourseDiscussion>> PinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.Pin();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discussion {DiscussionId} pinned", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> UnpinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.Unpin();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discussion {DiscussionId} unpinned", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> MarkDiscussionResolvedAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.MarkResolved();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discussion {DiscussionId} marked as resolved", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<bool>> DeleteDiscussionAsync(Guid discussionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<bool>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        if (discussion.AuthorId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Discussion.Unauthorized", "You can only delete your own discussions"));
        }

        _context.Set<CourseDiscussion>().Remove(discussion);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discussion {DiscussionId} deleted by user {UserId}", discussionId, userId);
        return Result.Success(true);
    }

    public async Task<Result<CourseDiscussion>> IncrementDiscussionViewsAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.IncrementViews();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(discussion);
    }

    #endregion

    #region Discussion Replies

    public async Task<Result<DiscussionReply>> CreateReplyAsync(
        Guid discussionId,
        Guid authorId,
        string content,
        Guid? parentReplyId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken);

        if (discussion == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        var reply = DiscussionReply.Create(discussionId, authorId, content, parentReplyId);
        _context.Set<DiscussionReply>().Add(reply);
        
        discussion.IncrementReplies();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reply created for discussion {DiscussionId} by user {AuthorId}", discussionId, authorId);
        return Result.Success(reply);
    }

    public async Task<Result<IEnumerable<DiscussionReply>>> GetDiscussionRepliesAsync(
        Guid discussionId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var replies = await _context.Set<DiscussionReply>()
            .Where(r => r.DiscussionId == discussionId)
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<DiscussionReply>>(replies);
    }

    public async Task<Result<DiscussionReply>> AcceptReplyAsAnswerAsync(
        Guid replyId,
        Guid discussionAuthorId,
        CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken);

        if (reply == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        // Verify the requester is the discussion author
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == reply.DiscussionId, cancellationToken);

        if (discussion == null || discussion.AuthorId != discussionAuthorId)
        {
            return Result.Failure<DiscussionReply>(Error.Failure("Reply.Unauthorized", "Only the discussion author can accept answers"));
        }

        reply.AcceptAsAnswer();
        discussion.MarkResolved();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reply {ReplyId} accepted as answer for discussion {DiscussionId}", replyId, reply.DiscussionId);
        return Result.Success(reply);
    }

    public async Task<Result<DiscussionReply>> UpvoteReplyAsync(Guid replyId, CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken);

        if (reply == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        reply.Upvote();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(reply);
    }

    public async Task<Result<bool>> DeleteReplyAsync(Guid replyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken);

        if (reply == null)
        {
            return Result.Failure<bool>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        if (reply.AuthorId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Reply.Unauthorized", "You can only delete your own replies"));
        }

        _context.Set<DiscussionReply>().Remove(reply);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reply {ReplyId} deleted by user {UserId}", replyId, userId);
        return Result.Success(true);
    }

    #endregion

    #region Course Likes (Social Proof)

    public async Task<Result<CourseLike>> LikeCourseAsync(Guid courseId, Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<CourseLike>()
            .FirstOrDefaultAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken);

        if (existing != null)
        {
            return Result.Failure<CourseLike>(Error.Failure("Like.AlreadyExists", "You have already liked this course"));
        }

        var like = CourseLike.Create(courseId, userId, tenantId);
        _context.Set<CourseLike>().Add(like);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Course {CourseId} liked by user {UserId}", courseId, userId);
        return Result.Success(like);
    }

    public async Task<Result<bool>> UnlikeCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var like = await _context.Set<CourseLike>()
            .FirstOrDefaultAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken);

        if (like == null)
        {
            return Result.Failure<bool>(Error.NotFound("Like.NotFound", "You haven't liked this course"));
        }

        _context.Set<CourseLike>().Remove(like);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Course {CourseId} unliked by user {UserId}", courseId, userId);
        return Result.Success(true);
    }

    public async Task<Result<bool>> HasUserLikedCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<CourseLike>()
            .AnyAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken);

        return Result.Success(exists);
    }

    public async Task<Result<int>> GetCourseLikeCountAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var count = await _context.Set<CourseLike>()
            .CountAsync(l => l.CourseId == courseId, cancellationToken);

        return Result.Success(count);
    }

    public async Task<Result<IEnumerable<CourseLike>>> GetUserLikedCoursesAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var likes = await _context.Set<CourseLike>()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseLike>>(likes);
    }

    #endregion

    #region Personalized Feed

    public async Task<Result<IEnumerable<PersonalizedFeedItem>>> GetPersonalizedFeedAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        FeedItemType? filterByType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<PersonalizedFeedItem>()
            .Where(f => f.UserId == userId && !f.IsDismissed && f.ExpiresAt > DateTime.UtcNow);

        if (filterByType.HasValue)
        {
            query = query.Where(f => f.ItemType == filterByType.Value);
        }

        var items = await query
            .OrderByDescending(f => f.RelevanceScore)
            .ThenByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<PersonalizedFeedItem>>(items);
    }

    public async Task<Result<int>> GenerateFeedItemsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // This is a simplified feed generation - in production, this would use
        // more sophisticated algorithms based on user behavior, preferences, etc.
        var generatedCount = 0;

        // Get trending discussions (most replied in last 7 days)
        var trendingDiscussions = await _context.Set<CourseDiscussion>()
            .Where(d => d.LastActivityAt > DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(d => d.ReplyCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var discussion in trendingDiscussions)
        {
            var feedItem = PersonalizedFeedItem.Create(
                userId,
                FeedItemType.TrendingDiscussion,
                tenantId,
                courseId: discussion.CourseId,
                discussionId: discussion.Id,
                relevanceScore: 0.7,
                reason: $"Trending discussion with {discussion.ReplyCount} replies");

            _context.Set<PersonalizedFeedItem>().Add(feedItem);
            generatedCount++;
        }

        // Get featured reviews
        var featuredReviews = await _context.Set<CourseReview>()
            .Where(r => r.IsFeatured && r.IsApproved)
            .OrderByDescending(r => r.HelpfulCount)
            .Take(3)
            .ToListAsync(cancellationToken);

        foreach (var review in featuredReviews)
        {
            var feedItem = PersonalizedFeedItem.Create(
                userId,
                FeedItemType.FeaturedReview,
                tenantId,
                courseId: review.CourseId,
                reviewId: review.Id,
                relevanceScore: 0.8,
                reason: "Featured review");

            _context.Set<PersonalizedFeedItem>().Add(feedItem);
            generatedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated {Count} feed items for user {UserId}", generatedCount, userId);
        return Result.Success(generatedCount);
    }

    public async Task<Result<PersonalizedFeedItem>> MarkFeedItemViewedAsync(Guid feedItemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PersonalizedFeedItem>()
            .FirstOrDefaultAsync(f => f.Id == feedItemId, cancellationToken);

        if (item == null)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.NotFound("FeedItem.NotFound", $"Feed item with ID {feedItemId} not found"));
        }

        item.MarkViewed();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(item);
    }

    public async Task<Result<PersonalizedFeedItem>> DismissFeedItemAsync(Guid feedItemId, Guid userId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PersonalizedFeedItem>()
            .FirstOrDefaultAsync(f => f.Id == feedItemId, cancellationToken);

        if (item == null)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.NotFound("FeedItem.NotFound", $"Feed item with ID {feedItemId} not found"));
        }

        if (item.UserId != userId)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.Failure("FeedItem.Unauthorized", "You can only dismiss your own feed items"));
        }

        item.Dismiss();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(item);
    }

    public async Task<Result<int>> ClearExpiredFeedItemsAsync(CancellationToken cancellationToken = default)
    {
        var expiredItems = await _context.Set<PersonalizedFeedItem>()
            .Where(f => f.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.Set<PersonalizedFeedItem>().RemoveRange(expiredItems);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleared {Count} expired feed items", expiredItems.Count);
        return Result.Success(expiredItems.Count);
    }

    #endregion
}
