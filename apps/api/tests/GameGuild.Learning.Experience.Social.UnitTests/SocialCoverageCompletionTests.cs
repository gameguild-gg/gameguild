using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Experience.Social;
using GameGuild.Learning.Experience.Social.Configuration;
using GameGuild.Learning.Experience.Social.Controllers;
using GameGuild.Learning.Experience.Social.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public class SocialModuleContractTests
{
    [Fact]
    public void AddSocialModule_ShouldRegisterAllScopedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddSocialModule();

        returned.Should().BeSameAs(services);
        AssertScoped<IReviewService, ReviewService>(services);
        AssertScoped<IWishlistService, WishlistService>(services);
        AssertScoped<IDiscussionService, DiscussionService>(services);
        AssertScoped<IReplyService, ReplyService>(services);
        AssertScoped<ILikeService, LikeService>(services);
        AssertScoped<IFeedService, FeedService>(services);
    }

    [Fact]
    public void Services_Constructors_ShouldCreateInstances()
    {
        var context = new Mock<IApplicationDbContext>().Object;

        new ReviewService(context, NullLogger<ReviewService>.Instance).Should().BeAssignableTo<IReviewService>();
        new WishlistService(context, NullLogger<WishlistService>.Instance).Should().BeAssignableTo<IWishlistService>();
        new DiscussionService(context, NullLogger<DiscussionService>.Instance).Should().BeAssignableTo<IDiscussionService>();
        new ReplyService(context, NullLogger<ReplyService>.Instance).Should().BeAssignableTo<IReplyService>();
        new LikeService(context, NullLogger<LikeService>.Instance).Should().BeAssignableTo<ILikeService>();
        new FeedService(context, NullLogger<FeedService>.Instance).Should().BeAssignableTo<IFeedService>();
    }

    [Fact]
    public void Dtos_ShouldExposeAllValues()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var discussionId = Guid.NewGuid();
        var parentReplyId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var learningPathId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        var stats = new CourseRatingStats(courseId, 4.5, 9, 5, 2, 1, 1, 0, 2);
        var createReview = new CreateReviewRequest(courseId, 5, "Title", "Body", id);
        var createDiscussion = new CreateDiscussionRequest(courseId, "Question", "Content", contentId);
        var createReply = new CreateReplyRequest(discussionId, "Reply", parentReplyId);
        var preferences = new WishlistPreferencesRequest(true, false);
        var review = new CourseReviewDto(id, courseId, userId, 5, "Title", "Body", true, 2, true, false, createdAt);
        var discussion = new CourseDiscussionDto(id, courseId, contentId, userId, "Question", "Content", true, false, 3, 4, createdAt, createdAt);
        var reply = new DiscussionReplyDto(id, discussionId, userId, parentReplyId, "Reply", true, 7, createdAt);
        var wishlist = new CourseWishlistDto(id, courseId, userId, true, false, createdAt);
        var like = new CourseLikeDto(id, courseId, userId, createdAt);
        var feed = new PersonalizedFeedItemDto(id, FeedItemType.LearningPathSuggestion, courseId, discussionId, reviewId, learningPathId, 0.91, "Reason", true, createdAt.AddDays(7), createdAt);

        stats.FeaturedReviewCount.Should().Be(2);
        createReview.EnrollmentId.Should().Be(id);
        createDiscussion.ContentId.Should().Be(contentId);
        createReply.ParentReplyId.Should().Be(parentReplyId);
        preferences.NotifyOnSale.Should().BeTrue();
        review.IsVerifiedPurchase.Should().BeTrue();
        discussion.ReplyCount.Should().Be(3);
        reply.IsAcceptedAnswer.Should().BeTrue();
        wishlist.NotifyOnUpdate.Should().BeFalse();
        like.CreatedAt.Should().Be(createdAt);
        feed.LearningPathId.Should().Be(learningPathId);
    }

    [Fact]
    public void SocialConfigurations_ShouldApplyToModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        new CourseReviewConfiguration().Configure(modelBuilder.Entity<CourseReview>());
        new CourseWishlistConfiguration().Configure(modelBuilder.Entity<CourseWishlist>());
        new CourseDiscussionConfiguration().Configure(modelBuilder.Entity<CourseDiscussion>());
        new DiscussionReplyConfiguration().Configure(modelBuilder.Entity<DiscussionReply>());
        new CourseLikeConfiguration().Configure(modelBuilder.Entity<CourseLike>());
        new PersonalizedFeedItemConfiguration().Configure(modelBuilder.Entity<PersonalizedFeedItem>());
        var model = modelBuilder.FinalizeModel();

        model.FindEntityType(typeof(CourseReview))!.GetTableName().Should().Be("course_reviews");
        model.FindEntityType(typeof(CourseWishlist))!.GetTableName().Should().Be("course_wishlists");
        model.FindEntityType(typeof(CourseDiscussion))!.GetTableName().Should().Be("course_discussions");
        model.FindEntityType(typeof(DiscussionReply))!.GetTableName().Should().Be("discussion_replies");
        model.FindEntityType(typeof(CourseLike))!.GetTableName().Should().Be("course_likes");
        model.FindEntityType(typeof(PersonalizedFeedItem))!.GetTableName().Should().Be("personalized_feed_items");
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}

public class SocialControllerMappingTests
{
    [Fact]
    public async Task ReviewsController_CreateReview_ShouldMapCreatedReview()
    {
        var userId = Guid.NewGuid();
        var review = CourseReview.Create(Guid.NewGuid(), userId, 5, "Great", "Useful", Guid.NewGuid());
        review.Approve();
        var service = new Mock<IReviewService>();
        service.Setup(s => s.CreateReviewAsync(
                review.CourseId,
                userId,
                review.Rating,
                review.Title,
                review.Content,
                review.EnrollmentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(review));
        var controller = new ReviewsController(service.Object, ActorAccessor(userId));

        var result = await controller.CreateReview(
            new CreateReviewRequest(review.CourseId, review.Rating, review.Title, review.Content, review.EnrollmentId));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<CourseReviewDto>().Which.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task WishlistsController_AddToWishlist_ShouldMapCreatedWishlist()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var wishlist = CourseWishlist.Create(courseId, userId);
        var service = new Mock<IWishlistService>();
        service.Setup(s => s.AddToWishlistAsync(courseId, userId, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(wishlist));
        var controller = new WishlistsController(service.Object, ActorAccessor(userId));

        var result = await controller.AddToWishlist(courseId);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<CourseWishlistDto>().Which.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task DiscussionsController_CreateDiscussion_ShouldMapCreatedDiscussion()
    {
        var userId = Guid.NewGuid();
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), userId, "Question", "Content", Guid.NewGuid());
        discussion.Pin();
        var service = new Mock<IDiscussionService>();
        service.Setup(s => s.CreateDiscussionAsync(
                discussion.CourseId,
                userId,
                discussion.Title,
                discussion.Content,
                discussion.ContentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(discussion));
        var controller = new DiscussionsController(service.Object, ActorAccessor(userId));

        var result = await controller.CreateDiscussion(new CreateDiscussionRequest(discussion.CourseId, discussion.Title, discussion.Content, discussion.ContentId));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<CourseDiscussionDto>().Which.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task RepliesController_CreateReply_ShouldMapCreatedReply()
    {
        var userId = Guid.NewGuid();
        var discussionId = Guid.NewGuid();
        var reply = DiscussionReply.Create(discussionId, userId, "Answer", Guid.NewGuid());
        reply.AcceptAsAnswer();
        var service = new Mock<IReplyService>();
        service.Setup(s => s.CreateReplyAsync(
                discussionId,
                userId,
                reply.Content,
                reply.ParentReplyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reply));
        var controller = new RepliesController(service.Object, ActorAccessor(userId));

        var result = await controller.CreateReply(discussionId, new CreateReplyRequest(discussionId, reply.Content, reply.ParentReplyId));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<DiscussionReplyDto>().Which.IsAcceptedAnswer.Should().BeTrue();
    }

    [Fact]
    public async Task LikesController_LikeCourse_ShouldMapCreatedLike()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var like = CourseLike.Create(courseId, userId);
        var service = new Mock<ILikeService>();
        service.Setup(s => s.LikeCourseAsync(courseId, userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(like));
        var controller = new LikesController(service.Object, ActorAccessor(userId));

        var result = await controller.LikeCourse(courseId);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<CourseLikeDto>().Which.CourseId.Should().Be(courseId);
    }

    [Fact]
    public async Task FeedController_MarkFeedItemViewed_ShouldMapViewedFeedItem()
    {
        var userId = Guid.NewGuid();
        var item = PersonalizedFeedItem.Create(
            userId,
            FeedItemType.FeaturedReview,
            courseId: Guid.NewGuid(),
            discussionId: Guid.NewGuid(),
            reviewId: Guid.NewGuid(),
            learningPathId: Guid.NewGuid(),
            relevanceScore: 0.7,
            reason: "Featured");
        item.MarkViewed();
        var service = new Mock<IFeedService>();
        service.Setup(s => s.MarkFeedItemViewedAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(item));
        var controller = new FeedController(service.Object, ActorAccessor(userId));

        var result = await controller.MarkFeedItemViewed(item.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PersonalizedFeedItemDto>().Which.IsViewed.Should().BeTrue();
    }

    private static IActorContextAccessor ActorAccessor(Guid userId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = null,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        return accessor.Object;
    }
}
