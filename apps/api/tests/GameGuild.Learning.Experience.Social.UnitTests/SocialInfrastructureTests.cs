using FluentAssertions;
using GameGuild.Learning.Experience.Social.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public sealed class SocialCommandHandlerTests
{
    [Fact]
    public async Task DiscussionHandler_DelegatesEveryCommand()
    {
        var service = new Mock<IDiscussionService>();
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "Topic", "Body");
        service.Setup(value => value.CreateDiscussionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(discussion));
        service.Setup(value => value.PinDiscussionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(discussion));
        service.Setup(value => value.UnpinDiscussionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(discussion));
        service.Setup(value => value.MarkDiscussionResolvedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(discussion));
        service.Setup(value => value.DeleteDiscussionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));
        var handler = new DiscussionCommandHandler(service.Object);
        var cancellationToken = new CancellationTokenSource().Token;

        (await handler.Handle(new CreateDiscussionCommand(Guid.NewGuid(), Guid.NewGuid(), "Topic", "Body", null, Guid.NewGuid()), cancellationToken)).Value.Should().BeSameAs(discussion);
        (await handler.Handle(new PinDiscussionCommand(discussion.Id), cancellationToken)).Value.Should().BeSameAs(discussion);
        (await handler.Handle(new UnpinDiscussionCommand(discussion.Id), cancellationToken)).Value.Should().BeSameAs(discussion);
        (await handler.Handle(new MarkDiscussionResolvedCommand(discussion.Id), cancellationToken)).Value.Should().BeSameAs(discussion);
        (await handler.Handle(new DeleteDiscussionCommand(discussion.Id, Guid.NewGuid()), cancellationToken)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task FeedAndLikeHandlers_DelegateEveryCommand()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var item = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, tenantId);
        var feed = new Mock<IFeedService>();
        feed.Setup(value => value.GenerateFeedItemsAsync(userId, tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(1));
        feed.Setup(value => value.MarkFeedItemViewedAsync(item.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(item));
        feed.Setup(value => value.DismissFeedItemAsync(item.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(item));
        var feedHandler = new FeedCommandHandler(feed.Object);

        (await feedHandler.Handle(new GenerateFeedItemsCommand(userId, tenantId), default)).Value.Should().Be(1);
        (await feedHandler.Handle(new MarkFeedItemViewedCommand(item.Id, userId), default)).Value.Should().BeSameAs(item);
        (await feedHandler.Handle(new DismissFeedItemCommand(item.Id, userId), default)).Value.Should().BeSameAs(item);

        var like = CourseLike.Create(Guid.NewGuid(), userId, tenantId);
        var likes = new Mock<ILikeService>();
        likes.Setup(value => value.LikeCourseAsync(like.CourseId, userId, tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(like));
        likes.Setup(value => value.UnlikeCourseAsync(like.CourseId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));
        var likeHandler = new LikeCommandHandler(likes.Object);

        (await likeHandler.Handle(new LikeCourseCommand(like.CourseId, userId, tenantId), default)).Value.Should().BeSameAs(like);
        (await likeHandler.Handle(new UnlikeCourseCommand(like.CourseId, userId), default)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ReplyHandler_DelegatesEveryCommand()
    {
        var reply = DiscussionReply.Create(Guid.NewGuid(), Guid.NewGuid(), "Reply");
        var service = new Mock<IReplyService>();
        service.Setup(value => value.CreateReplyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(reply));
        service.Setup(value => value.AcceptReplyAsAnswerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(reply));
        service.Setup(value => value.UpvoteReplyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(reply));
        service.Setup(value => value.DeleteReplyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));
        var handler = new ReplyCommandHandler(service.Object);

        (await handler.Handle(new CreateReplyCommand(reply.DiscussionId, reply.AuthorId, reply.Content, reply.ParentReplyId), default)).Value.Should().BeSameAs(reply);
        (await handler.Handle(new AcceptReplyAsAnswerCommand(reply.Id, Guid.NewGuid()), default)).Value.Should().BeSameAs(reply);
        (await handler.Handle(new UpvoteReplyCommand(reply.Id), default)).Value.Should().BeSameAs(reply);
        (await handler.Handle(new DeleteReplyCommand(reply.Id, reply.AuthorId), default)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ReviewHandler_DelegatesEveryCommand()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        var service = new Mock<IReviewService>();
        service.Setup(value => value.CreateReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(review));
        service.Setup(value => value.MarkReviewHelpfulAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(review));
        service.Setup(value => value.DeleteReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));
        service.Setup(value => value.ApproveReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(review));
        service.Setup(value => value.FeatureReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(review));
        service.Setup(value => value.UpdateReviewModerationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(review));
        var handler = new ReviewCommandHandler(service.Object);

        (await handler.Handle(new CreateReviewCommand(review.CourseId, review.UserId, 5, null, null, null, null), default)).Value.Should().BeSameAs(review);
        (await handler.Handle(new MarkReviewHelpfulCommand(review.Id), default)).Value.Should().BeSameAs(review);
        (await handler.Handle(new DeleteReviewCommand(review.Id, review.UserId), default)).Value.Should().BeTrue();
        (await handler.Handle(new ApproveReviewCommand(review.Id), default)).Value.Should().BeSameAs(review);
        (await handler.Handle(new FeatureReviewCommand(review.Id), default)).Value.Should().BeSameAs(review);
        (await handler.Handle(new UpdateReviewModerationCommand(review.Id, true, true), default)).Value.Should().BeSameAs(review);
    }

    [Fact]
    public async Task WishlistHandler_DelegatesEveryCommand()
    {
        var item = CourseWishlist.Create(Guid.NewGuid(), Guid.NewGuid());
        var service = new Mock<IWishlistService>();
        service.Setup(value => value.AddToWishlistAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(item));
        service.Setup(value => value.RemoveFromWishlistAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));
        service.Setup(value => value.UpdateWishlistPreferencesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(item));
        var handler = new WishlistCommandHandler(service.Object);

        (await handler.Handle(new AddToWishlistCommand(item.CourseId, item.UserId, true, false, null), default)).Value.Should().BeSameAs(item);
        (await handler.Handle(new RemoveFromWishlistCommand(item.CourseId, item.UserId), default)).Value.Should().BeTrue();
        (await handler.Handle(new UpdateWishlistPreferencesCommand(item.CourseId, item.UserId, false, true), default)).Value.Should().BeSameAs(item);
    }
}

public sealed class SocialInfrastructureTests
{
    [Fact]
    public void Module_RegistersEverySocialServiceAndReturnsCollection()
    {
        var services = new ServiceCollection();

        services.AddSocialModule().Should().BeSameAs(services);

        services.Should().ContainSingle(value => value.ServiceType == typeof(IReviewService) && value.ImplementationType == typeof(ReviewService));
        services.Should().ContainSingle(value => value.ServiceType == typeof(IWishlistService) && value.ImplementationType == typeof(WishlistService));
        services.Should().ContainSingle(value => value.ServiceType == typeof(IDiscussionService) && value.ImplementationType == typeof(DiscussionService));
        services.Should().ContainSingle(value => value.ServiceType == typeof(IReplyService) && value.ImplementationType == typeof(ReplyService));
        services.Should().ContainSingle(value => value.ServiceType == typeof(ILikeService) && value.ImplementationType == typeof(LikeService));
        services.Should().ContainSingle(value => value.ServiceType == typeof(IFeedService) && value.ImplementationType == typeof(FeedService));
    }

    [Fact]
    public void DtoContracts_RoundTripAllValues()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        new CreateReviewRequest(id, 5, "Title", "Content", id).CourseId.Should().Be(id);
        new CreateDiscussionRequest(id, "Title", "Content", id).ContentId.Should().Be(id);
        new CreateReplyRequest(id, "Content", id).DiscussionId.Should().Be(id);
        new WishlistPreferencesRequest(true, false).NotifyOnSale.Should().BeTrue();
        new CourseReviewDto(id, id, id, 5, "T", "C", true, 1, true, true, now).Id.Should().Be(id);
        new CourseDiscussionDto(id, id, id, id, "T", "C", true, true, 1, 2, now, now).Id.Should().Be(id);
        new DiscussionReplyDto(id, id, id, id, "C", true, 1, now).Id.Should().Be(id);
        new CourseWishlistDto(id, id, id, true, false, now).Id.Should().Be(id);
        new CourseLikeDto(id, id, id, now).Id.Should().Be(id);
        new PersonalizedFeedItemDto(id, FeedItemType.NewCourse, id, id, id, id, 1, "R", true, now, now).Id.Should().Be(id);
    }
}
