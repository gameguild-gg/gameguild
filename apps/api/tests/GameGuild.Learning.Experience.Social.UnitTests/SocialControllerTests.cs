using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Experience.Social.Controllers;
using GameGuild.Learning.Experience.Social.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public sealed class SocialControllerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<ISender> _sender = new();
    private readonly IActorContextAccessor _actors;

    public SocialControllerTests()
    {
        var actors = new Mock<IActorContextAccessor>();
        actors.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            SubjectId = _userId.ToString(),
            TenantId = _tenantId,
            ActorKind = ActorKind.User,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        _actors = actors.Object;
    }

    [Fact]
    public async Task DiscussionsController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<IDiscussionService>();
        var controller = new DiscussionsController(service.Object, _sender.Object, _actors);
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), _userId, "Topic", "Body", Guid.NewGuid(), _tenantId);
        var failure = Failure<CourseDiscussion>();
        SetupSender<CreateDiscussionCommand, CourseDiscussion>(failure, Result.Success(discussion));
        SetupSender<PinDiscussionCommand, CourseDiscussion>(failure, Result.Success(discussion));
        SetupSender<UnpinDiscussionCommand, CourseDiscussion>(failure, Result.Success(discussion));
        SetupSender<MarkDiscussionResolvedCommand, CourseDiscussion>(failure, Result.Success(discussion));
        SetupSender<DeleteDiscussionCommand, bool>(Failure<bool>(), Result.Success(true));
        service.SetupSequence(value => value.GetDiscussionByIdAsync(discussion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure).ReturnsAsync(Result.Success(discussion));
        service.Setup(value => value.IncrementDiscussionViewsAsync(discussion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(discussion));
        SetupServiceSequence(
            service,
            value => value.GetCourseDiscussionsAsync(discussion.CourseId, 0, 20, true, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseDiscussion>>(),
            Result.Success<IEnumerable<CourseDiscussion>>([discussion]));
        SetupServiceSequence(
            service,
            value => value.GetContentDiscussionsAsync(discussion.CourseId, discussion.ContentId!.Value, 0, 20, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseDiscussion>>(),
            Result.Success<IEnumerable<CourseDiscussion>>([discussion]));
        var createRequest = new CreateDiscussionRequest(discussion.CourseId, "Topic", "Body", discussion.ContentId);

        (await controller.CreateDiscussion(createRequest)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CreateDiscussion(createRequest)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.GetDiscussion(discussion.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.GetDiscussion(discussion.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.GetCourseDiscussions(discussion.CourseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCourseDiscussions(discussion.CourseId)).Should().BeOfType<OkObjectResult>();
        (await controller.GetContentDiscussions(discussion.CourseId, discussion.ContentId!.Value)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetContentDiscussions(discussion.CourseId, discussion.ContentId!.Value)).Should().BeOfType<OkObjectResult>();
        (await controller.PinDiscussion(discussion.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.PinDiscussion(discussion.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.UnpinDiscussion(discussion.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.UnpinDiscussion(discussion.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.MarkDiscussionResolved(discussion.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.MarkDiscussionResolved(discussion.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.DeleteDiscussion(discussion.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteDiscussion(discussion.Id)).Should().BeOfType<NoContentResult>();

        _sender.Verify(value => value.Send<Result<CourseDiscussion>>(
            It.Is<CreateDiscussionCommand>(command => command.TenantId == _tenantId && command.AuthorId == _userId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RepliesController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<IReplyService>();
        var controller = new RepliesController(service.Object, _sender.Object, _actors);
        var discussionId = Guid.NewGuid();
        var reply = DiscussionReply.Create(discussionId, _userId, "Reply", Guid.NewGuid(), _tenantId);
        var failure = Failure<DiscussionReply>();
        SetupSender<CreateReplyCommand, DiscussionReply>(failure, Result.Success(reply));
        SetupSender<AcceptReplyAsAnswerCommand, DiscussionReply>(failure, Result.Success(reply));
        SetupSender<UpvoteReplyCommand, DiscussionReply>(failure, Result.Success(reply));
        SetupSender<DeleteReplyCommand, bool>(Failure<bool>(), Result.Success(true));
        SetupServiceSequence(
            service,
            value => value.GetDiscussionRepliesAsync(discussionId, 0, 50, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<DiscussionReply>>(),
            Result.Success<IEnumerable<DiscussionReply>>([reply]));
        var request = new CreateReplyRequest(discussionId, "Reply", reply.ParentReplyId);

        (await controller.CreateReply(discussionId, request)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CreateReply(discussionId, request)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.GetDiscussionReplies(discussionId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetDiscussionReplies(discussionId)).Should().BeOfType<OkObjectResult>();
        (await controller.AcceptReplyAsAnswer(reply.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.AcceptReplyAsAnswer(reply.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.UpvoteReply(reply.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.UpvoteReply(reply.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.DeleteReply(reply.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteReply(reply.Id)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task LikesController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<ILikeService>();
        var controller = new LikesController(service.Object, _sender.Object, _actors);
        var courseId = Guid.NewGuid();
        var like = CourseLike.Create(courseId, _userId, _tenantId);
        SetupSender<LikeCourseCommand, CourseLike>(Failure<CourseLike>(), Result.Success(like));
        SetupSender<UnlikeCourseCommand, bool>(Failure<bool>(), Result.Success(true));
        service.SetupSequence(value => value.HasUserLikedCourseAsync(courseId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure<bool>()).ReturnsAsync(Result.Success(true));
        service.SetupSequence(value => value.GetCourseLikeCountAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure<int>()).ReturnsAsync(Result.Success(1));
        SetupServiceSequence(
            service,
            value => value.GetUserLikedCoursesAsync(_userId, 0, 50, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseLike>>(),
            Result.Success<IEnumerable<CourseLike>>([like]));

        (await controller.LikeCourse(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.LikeCourse(courseId)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.UnlikeCourse(courseId)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.UnlikeCourse(courseId)).Should().BeOfType<NoContentResult>();
        (await controller.HasLikedCourse(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.HasLikedCourse(courseId)).Should().BeOfType<OkObjectResult>();
        (await controller.GetCourseLikeCount(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCourseLikeCount(courseId)).Should().BeOfType<OkObjectResult>();
        (await controller.GetLikedCourses()).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetLikedCourses()).Should().BeOfType<OkObjectResult>();

        _sender.Verify(value => value.Send<Result<CourseLike>>(
            It.Is<LikeCourseCommand>(command => command.TenantId == _tenantId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WishlistsController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<IWishlistService>();
        var controller = new WishlistsController(service.Object, _sender.Object, _actors);
        var courseId = Guid.NewGuid();
        var item = CourseWishlist.Create(courseId, _userId, tenantId: _tenantId);
        SetupSender<AddToWishlistCommand, CourseWishlist>(Failure<CourseWishlist>(), Result.Success(item));
        SetupSender<RemoveFromWishlistCommand, bool>(Failure<bool>(), Result.Success(true));
        SetupSender<UpdateWishlistPreferencesCommand, CourseWishlist>(Failure<CourseWishlist>(), Result.Success(item));
        SetupServiceSequence(
            service,
            value => value.GetUserWishlistAsync(_userId, 0, 50, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseWishlist>>(),
            Result.Success<IEnumerable<CourseWishlist>>([item]));
        service.SetupSequence(value => value.IsInWishlistAsync(courseId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure<bool>()).ReturnsAsync(Result.Success(true));
        var preferences = new WishlistPreferencesRequest(true, false);

        (await controller.AddToWishlist(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.AddToWishlist(courseId)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.RemoveFromWishlist(courseId)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.RemoveFromWishlist(courseId)).Should().BeOfType<NoContentResult>();
        (await controller.GetMyWishlist()).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetMyWishlist()).Should().BeOfType<OkObjectResult>();
        (await controller.IsInWishlist(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.IsInWishlist(courseId)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateWishlistPreferences(courseId, preferences)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.UpdateWishlistPreferences(courseId, preferences)).Should().BeOfType<OkObjectResult>();

        _sender.Verify(value => value.Send<Result<CourseWishlist>>(
            It.Is<AddToWishlistCommand>(command => command.TenantId == _tenantId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FeedController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<IFeedService>();
        var controller = new FeedController(service.Object, _sender.Object, _actors);
        var item = PersonalizedFeedItem.Create(_userId, FeedItemType.NewCourse, _tenantId);
        SetupServiceSequence(
            service,
            value => value.GetPersonalizedFeedAsync(_userId, 0, 20, null, _tenantId, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<PersonalizedFeedItem>>(),
            Result.Success<IEnumerable<PersonalizedFeedItem>>([item]));
        SetupSender<GenerateFeedItemsCommand, int>(Failure<int>(), Result.Success(1));
        SetupSender<MarkFeedItemViewedCommand, PersonalizedFeedItem>(Failure<PersonalizedFeedItem>(), Result.Success(item));
        SetupSender<DismissFeedItemCommand, PersonalizedFeedItem>(Failure<PersonalizedFeedItem>(), Result.Success(item));

        (await controller.GetPersonalizedFeed()).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetPersonalizedFeed()).Should().BeOfType<OkObjectResult>();
        (await controller.GenerateFeedItems()).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GenerateFeedItems()).Should().BeOfType<OkObjectResult>();
        (await controller.MarkFeedItemViewed(item.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.MarkFeedItemViewed(item.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.DismissFeedItem(item.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DismissFeedItem(item.Id)).Should().BeOfType<OkObjectResult>();

        _sender.Verify(value => value.Send<Result<PersonalizedFeedItem>>(
            It.Is<MarkFeedItemViewedCommand>(command => command.UserId == _userId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReviewsController_CoversEverySuccessAndFailureResponse()
    {
        var service = new Mock<IReviewService>();
        var controller = new ReviewsController(service.Object, _sender.Object, _actors);
        var courseId = Guid.NewGuid();
        var review = CourseReview.Create(courseId, _userId, 5, "Title", "Body", tenantId: _tenantId);
        SetupSender<CreateReviewCommand, CourseReview>(Failure<CourseReview>(), Result.Success(review));
        SetupSender<MarkReviewHelpfulCommand, CourseReview>(Failure<CourseReview>(), Result.Success(review));
        SetupSender<DeleteReviewCommand, bool>(Failure<bool>(), Result.Success(true));
        SetupSender<ApproveReviewCommand, CourseReview>(Failure<CourseReview>(), Result.Success(review));
        SetupSender<FeatureReviewCommand, CourseReview>(Failure<CourseReview>(), Result.Success(review));
        SetupSender<UpdateReviewModerationCommand, CourseReview>(Failure<CourseReview>(), Result.Success(review));
        service.SetupSequence(value => value.GetReviewByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure<CourseReview>()).ReturnsAsync(Result.Success(review));
        SetupServiceSequence(
            service,
            value => value.GetCourseReviewsAsync(courseId, 0, 20, true, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseReview>>(),
            Result.Success<IEnumerable<CourseReview>>([review]));
        SetupServiceSequence(
            service,
            value => value.GetUserReviewsAsync(_userId, 0, 20, It.IsAny<CancellationToken>()),
            Failure<IEnumerable<CourseReview>>(),
            Result.Success<IEnumerable<CourseReview>>([review]));
        service.SetupSequence(value => value.GetCourseRatingStatsAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure<CourseRatingStats>())
            .ReturnsAsync(Result.Success(new CourseRatingStats(courseId, 5, 1, 1, 0, 0, 0, 0, 0)));
        var request = new CreateReviewRequest(courseId, 5, "Title", "Body");
        var moderation = new UpdateReviewModerationRequest(true, true);

        (await controller.CreateReview(request)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.CreateReview(request)).Should().BeOfType<CreatedAtActionResult>();
        (await controller.GetReview(review.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.GetReview(review.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.GetCourseReviews(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCourseReviews(courseId)).Should().BeOfType<OkObjectResult>();
        (await controller.GetMyReviews()).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetMyReviews()).Should().BeOfType<OkObjectResult>();
        (await controller.MarkReviewHelpful(review.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.MarkReviewHelpful(review.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.DeleteReview(review.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteReview(review.Id)).Should().BeOfType<NoContentResult>();
        (await controller.GetCourseRatingStats(courseId)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCourseRatingStats(courseId)).Should().BeOfType<OkObjectResult>();
        (await controller.ApproveReview(review.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.ApproveReview(review.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.FeatureReview(review.Id)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.FeatureReview(review.Id)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateReviewModeration(review.Id, moderation)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.UpdateReviewModeration(review.Id, moderation)).Should().BeOfType<OkObjectResult>();

        _sender.Verify(value => value.Send<Result<CourseReview>>(
            It.Is<CreateReviewCommand>(command => command.TenantId == _tenantId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void SetupSender<TCommand, TValue>(Result<TValue> first, Result<TValue> second)
        where TCommand : class, IRequest<Result<TValue>>
    {
        _sender.SetupSequence(value => value.Send<Result<TValue>>(
                It.IsAny<TCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(first)
            .ReturnsAsync(second);
    }

    private static void SetupServiceSequence<TService, TValue>(
        Mock<TService> service,
        System.Linq.Expressions.Expression<Func<TService, Task<Result<TValue>>>> expression,
        Result<TValue> first,
        Result<TValue> second)
        where TService : class
    {
        service.SetupSequence(expression).ReturnsAsync(first).ReturnsAsync(second);
    }

    private static Result<T> Failure<T>() =>
        Result.Failure<T>(Error.Failure("Test.Failure", "Expected failure"));
}
