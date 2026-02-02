using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Attributes;
using GameGuild.Learning.Experience.Social.Services;
using GameGuild.Learning.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Experience.Social.Controllers;

/// <summary>
/// API controller for social learning features: reviews, discussions, wishlists, likes, and personalized feed
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[LxpCapabilityFilter]
[LxpCapability(LxpCapabilities.Social)]
public class SocialController : LearningControllerBase
{
    private readonly ISocialService _socialService;

    public SocialController(
        ISocialService socialService,
        IActorContextAccessor actorContextAccessor) : base(actorContextAccessor)
    {
        _socialService = socialService;
    }

    #region Course Reviews

    /// <summary>
    /// Creates a new course review
    /// </summary>
    [HttpPost("reviews")]
    [ProducesResponseType(typeof(CourseReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.CreateReviewAsync(
            request.CourseId,
            userId,
            request.Rating,
            request.Title,
            request.Content,
            request.EnrollmentId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetReview), new { id = result.Value.Id }, MapToReviewDto(result.Value));
    }

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    [HttpGet("reviews/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CourseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetReviewByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReviewDto(result.Value));
    }

    /// <summary>
    /// Gets all reviews for a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CourseReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseReviews(
        Guid courseId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool approvedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetCourseReviewsAsync(courseId, skip, take, approvedOnly, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToReviewDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Gets the current user's reviews
    /// </summary>
    [HttpGet("reviews/me")]
    [ProducesResponseType(typeof(IEnumerable<CourseReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.GetUserReviewsAsync(userId, skip, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToReviewDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Marks a review as helpful
    /// </summary>
    [HttpPost("reviews/{id:guid}/helpful")]
    [ProducesResponseType(typeof(CourseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewHelpful(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.MarkReviewHelpfulAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReviewDto(result.Value));
    }

    /// <summary>
    /// Deletes a review (owner only)
    /// </summary>
    [HttpDelete("reviews/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.DeleteReviewAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets rating statistics for a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/rating-stats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CourseRatingStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseRatingStats(Guid courseId, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetCourseRatingStatsAsync(courseId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Approves a review (admin only)
    /// </summary>
    [HttpPost("reviews/{id:guid}/approve")]
    [ProducesResponseType(typeof(CourseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.ApproveReviewAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReviewDto(result.Value));
    }

    /// <summary>
    /// Features a review (admin only)
    /// </summary>
    [HttpPost("reviews/{id:guid}/feature")]
    [ProducesResponseType(typeof(CourseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FeatureReview(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.FeatureReviewAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReviewDto(result.Value));
    }

    #endregion

    #region Course Wishlist (Bookmarks)

    /// <summary>
    /// Adds a course to the current user's wishlist
    /// </summary>
    [HttpPost("wishlist/{courseId:guid}")]
    [ProducesResponseType(typeof(CourseWishlistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToWishlist(
        Guid courseId,
        [FromQuery] bool notifyOnSale = true,
        [FromQuery] bool notifyOnUpdate = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.AddToWishlistAsync(courseId, userId, notifyOnSale, notifyOnUpdate, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetMyWishlist), null, MapToWishlistDto(result.Value));
    }

    /// <summary>
    /// Removes a course from the current user's wishlist
    /// </summary>
    [HttpDelete("wishlist/{courseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWishlist(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.RemoveFromWishlistAsync(courseId, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the current user's wishlist
    /// </summary>
    [HttpGet("wishlist/me")]
    [ProducesResponseType(typeof(IEnumerable<CourseWishlistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.GetUserWishlistAsync(userId, skip, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToWishlistDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Checks if a course is in the current user's wishlist
    /// </summary>
    [HttpGet("wishlist/{courseId:guid}/check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsInWishlist(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.IsInWishlistAsync(courseId, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { isInWishlist = result.Value });
    }

    /// <summary>
    /// Updates wishlist notification preferences
    /// </summary>
    [HttpPut("wishlist/{courseId:guid}/preferences")]
    [ProducesResponseType(typeof(CourseWishlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWishlistPreferences(
        Guid courseId,
        [FromBody] WishlistPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.UpdateWishlistPreferencesAsync(
            courseId, userId, request.NotifyOnSale, request.NotifyOnUpdate, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToWishlistDto(result.Value));
    }

    #endregion

    #region Course Discussions

    /// <summary>
    /// Creates a new discussion thread
    /// </summary>
    [HttpPost("discussions")]
    [ProducesResponseType(typeof(CourseDiscussionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDiscussion(
        [FromBody] CreateDiscussionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.CreateDiscussionAsync(
            request.CourseId,
            userId,
            request.Title,
            request.Content,
            request.ContentId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetDiscussion), new { id = result.Value.Id }, MapToDiscussionDto(result.Value));
    }

    /// <summary>
    /// Gets a discussion by ID
    /// </summary>
    [HttpGet("discussions/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CourseDiscussionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscussion(Guid id, CancellationToken cancellationToken = default)
    {
        // Increment view count
        await _socialService.IncrementDiscussionViewsAsync(id, cancellationToken);

        var result = await _socialService.GetDiscussionByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToDiscussionDto(result.Value));
    }

    /// <summary>
    /// Gets discussions for a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/discussions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CourseDiscussionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseDiscussions(
        Guid courseId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool pinnedFirst = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetCourseDiscussionsAsync(courseId, skip, take, pinnedFirst, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToDiscussionDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Gets discussions for specific content within a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/content/{contentId:guid}/discussions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CourseDiscussionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContentDiscussions(
        Guid courseId,
        Guid contentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetContentDiscussionsAsync(courseId, contentId, skip, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToDiscussionDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Pins a discussion (instructor/admin only)
    /// </summary>
    [HttpPost("discussions/{id:guid}/pin")]
    [ProducesResponseType(typeof(CourseDiscussionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PinDiscussion(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.PinDiscussionAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToDiscussionDto(result.Value));
    }

    /// <summary>
    /// Unpins a discussion
    /// </summary>
    [HttpPost("discussions/{id:guid}/unpin")]
    [ProducesResponseType(typeof(CourseDiscussionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpinDiscussion(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.UnpinDiscussionAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToDiscussionDto(result.Value));
    }

    /// <summary>
    /// Marks a discussion as resolved
    /// </summary>
    [HttpPost("discussions/{id:guid}/resolve")]
    [ProducesResponseType(typeof(CourseDiscussionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkDiscussionResolved(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.MarkDiscussionResolvedAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToDiscussionDto(result.Value));
    }

    /// <summary>
    /// Deletes a discussion (owner only)
    /// </summary>
    [HttpDelete("discussions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDiscussion(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.DeleteDiscussionAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    #endregion

    #region Discussion Replies

    /// <summary>
    /// Creates a reply to a discussion
    /// </summary>
    [HttpPost("discussions/{discussionId:guid}/replies")]
    [ProducesResponseType(typeof(DiscussionReplyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReply(
        Guid discussionId,
        [FromBody] CreateReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.CreateReplyAsync(
            discussionId,
            userId,
            request.Content,
            request.ParentReplyId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetDiscussionReplies), new { discussionId }, MapToReplyDto(result.Value));
    }

    /// <summary>
    /// Gets replies for a discussion
    /// </summary>
    [HttpGet("discussions/{discussionId:guid}/replies")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<DiscussionReplyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscussionReplies(
        Guid discussionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetDiscussionRepliesAsync(discussionId, skip, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToReplyDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Accepts a reply as the answer (discussion author only)
    /// </summary>
    [HttpPost("replies/{id:guid}/accept")]
    [ProducesResponseType(typeof(DiscussionReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptReplyAsAnswer(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.AcceptReplyAsAnswerAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReplyDto(result.Value));
    }

    /// <summary>
    /// Upvotes a reply
    /// </summary>
    [HttpPost("replies/{id:guid}/upvote")]
    [ProducesResponseType(typeof(DiscussionReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpvoteReply(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.UpvoteReplyAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToReplyDto(result.Value));
    }

    /// <summary>
    /// Deletes a reply (owner only)
    /// </summary>
    [HttpDelete("replies/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReply(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.DeleteReplyAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    #endregion

    #region Course Likes (Social Proof)

    /// <summary>
    /// Likes a course
    /// </summary>
    [HttpPost("courses/{courseId:guid}/like")]
    [ProducesResponseType(typeof(CourseLikeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LikeCourse(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.LikeCourseAsync(courseId, userId, null, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetLikedCourses), null, MapToLikeDto(result.Value));
    }

    /// <summary>
    /// Unlikes a course
    /// </summary>
    [HttpDelete("courses/{courseId:guid}/like")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlikeCourse(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.UnlikeCourseAsync(courseId, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Checks if the current user has liked a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/like/check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> HasLikedCourse(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.HasUserLikedCourseAsync(courseId, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { hasLiked = result.Value });
    }

    /// <summary>
    /// Gets the like count for a course
    /// </summary>
    [HttpGet("courses/{courseId:guid}/like/count")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseLikeCount(Guid courseId, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.GetCourseLikeCountAsync(courseId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { likeCount = result.Value });
    }

    /// <summary>
    /// Gets the current user's liked courses
    /// </summary>
    [HttpGet("likes/me")]
    [ProducesResponseType(typeof(IEnumerable<CourseLikeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLikedCourses(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.GetUserLikedCoursesAsync(userId, skip, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToLikeDto);
        return Ok(dtos);
    }

    #endregion

    #region Personalized Feed

    /// <summary>
    /// Gets the current user's personalized feed
    /// </summary>
    [HttpGet("feed/me")]
    [ProducesResponseType(typeof(IEnumerable<PersonalizedFeedItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonalizedFeed(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] FeedItemType? filterByType = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.GetPersonalizedFeedAsync(userId, skip, take, filterByType, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToFeedItemDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Generates new feed items for the current user
    /// </summary>
    [HttpPost("feed/me/generate")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFeedItems(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.GenerateFeedItemsAsync(userId, null, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { generatedCount = result.Value });
    }

    /// <summary>
    /// Marks a feed item as viewed
    /// </summary>
    [HttpPost("feed/{id:guid}/viewed")]
    [ProducesResponseType(typeof(PersonalizedFeedItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkFeedItemViewed(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _socialService.MarkFeedItemViewedAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToFeedItemDto(result.Value));
    }

    /// <summary>
    /// Dismisses a feed item
    /// </summary>
    [HttpPost("feed/{id:guid}/dismiss")]
    [ProducesResponseType(typeof(PersonalizedFeedItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissFeedItem(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _socialService.DismissFeedItemAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(MapToFeedItemDto(result.Value));
    }

    #endregion

    #region DTO Mapping

    private static CourseReviewDto MapToReviewDto(CourseReview review) =>
        new(review.Id,
            review.CourseId,
            review.UserId,
            review.Rating,
            review.Title,
            review.Content,
            review.IsVerifiedPurchase,
            review.HelpfulCount,
            review.IsApproved,
            review.IsFeatured,
            review.CreatedAt);

    private static CourseDiscussionDto MapToDiscussionDto(CourseDiscussion discussion) =>
        new(discussion.Id,
            discussion.CourseId,
            discussion.ContentId,
            discussion.AuthorId,
            discussion.Title,
            discussion.Content,
            discussion.IsPinned,
            discussion.IsResolved,
            discussion.ReplyCount,
            discussion.ViewCount,
            discussion.LastActivityAt,
            discussion.CreatedAt);

    private static DiscussionReplyDto MapToReplyDto(DiscussionReply reply) =>
        new(reply.Id,
            reply.DiscussionId,
            reply.AuthorId,
            reply.ParentReplyId,
            reply.Content,
            reply.IsAcceptedAnswer,
            reply.UpvoteCount,
            reply.CreatedAt);

    private static CourseWishlistDto MapToWishlistDto(CourseWishlist wishlist) =>
        new(wishlist.Id,
            wishlist.CourseId,
            wishlist.UserId,
            wishlist.NotifyOnSale,
            wishlist.NotifyOnUpdate,
            wishlist.CreatedAt);

    private static CourseLikeDto MapToLikeDto(CourseLike like) =>
        new(like.Id,
            like.CourseId,
            like.UserId,
            like.CreatedAt);

    private static PersonalizedFeedItemDto MapToFeedItemDto(PersonalizedFeedItem item) =>
        new(item.Id,
            item.ItemType,
            item.CourseId,
            item.DiscussionId,
            item.ReviewId,
            item.LearningPathId,
            item.RelevanceScore,
            item.Reason,
            item.IsViewed,
            item.ExpiresAt,
            item.CreatedAt);

    #endregion
}
