using FluentAssertions;
using GameGuild.Learning.Experience.Social.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public sealed class DiscussionServiceTests
{
    [Fact]
    public async Task CreateAndQueries_PersistTenantAndRespectOrderingAndContent()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new DiscussionService(context, NullLogger<DiscussionService>.Instance);
        var courseId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var first = (await service.CreateDiscussionAsync(courseId, Guid.NewGuid(), "First", "Body", contentId, tenantId)).Value;
        var pinned = (await service.CreateDiscussionAsync(courseId, Guid.NewGuid(), "Pinned", "Body", contentId, tenantId)).Value;
        pinned.Pin();
        await context.SaveChangesAsync();

        (await service.GetDiscussionByIdAsync(first.Id)).Value.Should().BeSameAs(first);
        (await service.GetCourseDiscussionsAsync(courseId, pinnedFirst: true)).Value.First().Should().BeSameAs(pinned);
        (await service.GetCourseDiscussionsAsync(courseId, pinnedFirst: false)).Value.Should().HaveCount(2);
        (await service.GetContentDiscussionsAsync(courseId, contentId)).Value.Should().HaveCount(2);
        first.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task LookupAndMutations_ReturnNotFoundForUnknownDiscussion()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new DiscussionService(context, NullLogger<DiscussionService>.Instance);
        var id = Guid.NewGuid();

        (await service.GetDiscussionByIdAsync(id)).IsSuccess.Should().BeFalse();
        (await service.PinDiscussionAsync(id)).IsSuccess.Should().BeFalse();
        (await service.UnpinDiscussionAsync(id)).IsSuccess.Should().BeFalse();
        (await service.MarkDiscussionResolvedAsync(id)).IsSuccess.Should().BeFalse();
        (await service.IncrementDiscussionViewsAsync(id)).IsSuccess.Should().BeFalse();
        (await service.DeleteDiscussionAsync(id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Mutations_ChangeStateAndEnforceOwnership()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new DiscussionService(context, NullLogger<DiscussionService>.Instance);
        var authorId = Guid.NewGuid();
        var discussion = (await service.CreateDiscussionAsync(Guid.NewGuid(), authorId, "Topic", "Body")).Value;

        (await service.DeleteDiscussionAsync(discussion.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.PinDiscussionAsync(discussion.Id)).Value.IsPinned.Should().BeTrue();
        (await service.UnpinDiscussionAsync(discussion.Id)).Value.IsPinned.Should().BeFalse();
        (await service.MarkDiscussionResolvedAsync(discussion.Id)).Value.IsResolved.Should().BeTrue();
        (await service.IncrementDiscussionViewsAsync(discussion.Id)).Value.ViewCount.Should().Be(1);
        (await service.DeleteDiscussionAsync(discussion.Id, authorId)).Value.Should().BeTrue();
        context.Set<CourseDiscussion>().Should().BeEmpty();
    }
}

public sealed class ReplyServiceTests
{
    [Fact]
    public async Task CreateAndList_RequireDiscussionAndPropagateTenant()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReplyService(context, NullLogger<ReplyService>.Instance);
        (await service.CreateReplyAsync(Guid.NewGuid(), Guid.NewGuid(), "Missing")).IsSuccess.Should().BeFalse();

        var tenantId = Guid.NewGuid();
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "Topic", "Body", tenantId: tenantId);
        context.Add(discussion);
        await context.SaveChangesAsync();
        var reply = (await service.CreateReplyAsync(discussion.Id, Guid.NewGuid(), "Reply", Guid.NewGuid())).Value;

        reply.TenantId.Should().Be(tenantId);
        discussion.ReplyCount.Should().Be(1);
        (await service.GetDiscussionRepliesAsync(discussion.Id)).Value.Should().ContainSingle().Which.Should().BeSameAs(reply);
    }

    [Fact]
    public async Task AcceptAnswer_CoversMissingDiscussionWrongAuthorAndSuccess()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReplyService(context, NullLogger<ReplyService>.Instance);
        (await service.AcceptReplyAsAnswerAsync(Guid.NewGuid(), Guid.NewGuid())).IsSuccess.Should().BeFalse();

        var orphan = DiscussionReply.Create(Guid.NewGuid(), Guid.NewGuid(), "Orphan");
        context.Add(orphan);
        await context.SaveChangesAsync();
        (await service.AcceptReplyAsAnswerAsync(orphan.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();

        var authorId = Guid.NewGuid();
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), authorId, "Topic", "Body");
        var reply = DiscussionReply.Create(discussion.Id, Guid.NewGuid(), "Answer");
        context.AddRange(discussion, reply);
        await context.SaveChangesAsync();

        (await service.AcceptReplyAsAnswerAsync(reply.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.AcceptReplyAsAnswerAsync(reply.Id, authorId)).Value.IsAcceptedAnswer.Should().BeTrue();
        discussion.IsResolved.Should().BeTrue();
    }

    [Fact]
    public async Task UpvoteAndDelete_CoverMissingOwnershipAndSuccess()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReplyService(context, NullLogger<ReplyService>.Instance);
        var missingId = Guid.NewGuid();
        (await service.UpvoteReplyAsync(missingId)).IsSuccess.Should().BeFalse();
        (await service.DeleteReplyAsync(missingId, Guid.NewGuid())).IsSuccess.Should().BeFalse();

        var authorId = Guid.NewGuid();
        var reply = DiscussionReply.Create(Guid.NewGuid(), authorId, "Reply");
        context.Add(reply);
        await context.SaveChangesAsync();
        (await service.DeleteReplyAsync(reply.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.UpvoteReplyAsync(reply.Id)).Value.UpvoteCount.Should().Be(1);
        (await service.DeleteReplyAsync(reply.Id, authorId)).Value.Should().BeTrue();
    }
}

public sealed class LikeServiceTests
{
    [Fact]
    public async Task LikeLifecycle_EnforcesUniquenessAndSupportsQueries()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new LikeService(context, NullLogger<LikeService>.Instance);
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var like = (await service.LikeCourseAsync(courseId, userId, tenantId)).Value;
        like.TenantId.Should().Be(tenantId);
        (await service.LikeCourseAsync(courseId, userId, tenantId)).IsSuccess.Should().BeFalse();
        (await service.HasUserLikedCourseAsync(courseId, userId)).Value.Should().BeTrue();
        (await service.HasUserLikedCourseAsync(Guid.NewGuid(), userId)).Value.Should().BeFalse();
        (await service.GetCourseLikeCountAsync(courseId)).Value.Should().Be(1);
        (await service.GetUserLikedCoursesAsync(userId)).Value.Should().ContainSingle();
        (await service.UnlikeCourseAsync(Guid.NewGuid(), userId)).IsSuccess.Should().BeFalse();
        (await service.UnlikeCourseAsync(courseId, userId)).Value.Should().BeTrue();
    }
}

public sealed class WishlistServiceTests
{
    [Fact]
    public async Task WishlistLifecycle_PersistsPreferencesAndCoversFailures()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new WishlistService(context, NullLogger<WishlistService>.Instance);
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var item = (await service.AddToWishlistAsync(courseId, userId, false, true, tenantId)).Value;
        item.NotifyOnSale.Should().BeFalse();
        item.NotifyOnUpdate.Should().BeTrue();
        item.TenantId.Should().Be(tenantId);
        (await service.AddToWishlistAsync(courseId, userId)).IsSuccess.Should().BeFalse();
        (await service.IsInWishlistAsync(courseId, userId)).Value.Should().BeTrue();
        (await service.IsInWishlistAsync(Guid.NewGuid(), userId)).Value.Should().BeFalse();
        (await service.GetUserWishlistAsync(userId)).Value.Should().ContainSingle();
        (await service.UpdateWishlistPreferencesAsync(Guid.NewGuid(), userId, true, false)).IsSuccess.Should().BeFalse();

        var updated = (await service.UpdateWishlistPreferencesAsync(courseId, userId, true, false)).Value;
        updated.NotifyOnSale.Should().BeTrue();
        updated.NotifyOnUpdate.Should().BeFalse();
        (await service.RemoveFromWishlistAsync(Guid.NewGuid(), userId)).IsSuccess.Should().BeFalse();
        (await service.RemoveFromWishlistAsync(courseId, userId)).Value.Should().BeTrue();
    }
}

public sealed class ReviewServiceTests
{
    [Fact]
    public async Task CreateAndQueries_CoverDuplicateApprovalFiltersAndTenant()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReviewService(context, NullLogger<ReviewService>.Instance);
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var review = (await service.CreateReviewAsync(courseId, userId, 5, "Title", "Body", tenantId: tenantId)).Value;

        review.TenantId.Should().Be(tenantId);
        (await service.CreateReviewAsync(courseId, userId, 4)).IsSuccess.Should().BeFalse();
        (await service.GetReviewByIdAsync(Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.GetReviewByIdAsync(review.Id)).Value.Should().BeSameAs(review);
        (await service.GetCourseReviewsAsync(courseId, approvedOnly: true)).Value.Should().BeEmpty();
        (await service.GetCourseReviewsAsync(courseId, approvedOnly: false)).Value.Should().ContainSingle();
        (await service.GetUserReviewsAsync(userId)).Value.Should().ContainSingle();
    }

    [Fact]
    public async Task ModerationHelpfulAndDelete_CoverMissingUnauthorizedAndSuccess()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReviewService(context, NullLogger<ReviewService>.Instance);
        var missingId = Guid.NewGuid();
        (await service.ApproveReviewAsync(missingId)).IsSuccess.Should().BeFalse();
        (await service.FeatureReviewAsync(missingId)).IsSuccess.Should().BeFalse();
        (await service.UpdateReviewModerationAsync(missingId, true, true)).IsSuccess.Should().BeFalse();
        (await service.MarkReviewHelpfulAsync(missingId)).IsSuccess.Should().BeFalse();
        (await service.DeleteReviewAsync(missingId, Guid.NewGuid())).IsSuccess.Should().BeFalse();

        var userId = Guid.NewGuid();
        var review = (await service.CreateReviewAsync(Guid.NewGuid(), userId, 4)).Value;
        (await service.DeleteReviewAsync(review.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.ApproveReviewAsync(review.Id)).Value.IsApproved.Should().BeTrue();
        (await service.FeatureReviewAsync(review.Id)).Value.IsFeatured.Should().BeTrue();
        var moderated = (await service.UpdateReviewModerationAsync(review.Id, false, false)).Value;
        moderated.IsApproved.Should().BeFalse();
        moderated.IsFeatured.Should().BeFalse();
        (await service.MarkReviewHelpfulAsync(review.Id)).Value.HelpfulCount.Should().Be(1);
        (await service.DeleteReviewAsync(review.Id, userId)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task RatingStats_CoverEmptyAndPopulatedCourses()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new ReviewService(context, NullLogger<ReviewService>.Instance);
        var courseId = Guid.NewGuid();
        var empty = (await service.GetCourseRatingStatsAsync(courseId)).Value;
        empty.TotalReviews.Should().Be(0);

        foreach (var rating in new[] { 1, 2, 3, 4, 5, 5 })
        {
            var review = CourseReview.Create(courseId, Guid.NewGuid(), rating);
            review.Approve();
            if (rating == 5) review.Feature();
            context.Add(review);
        }

        var ignored = CourseReview.Create(courseId, Guid.NewGuid(), 5);
        context.Add(ignored);
        await context.SaveChangesAsync();
        var stats = (await service.GetCourseRatingStatsAsync(courseId)).Value;
        stats.TotalReviews.Should().Be(6);
        stats.FiveStarCount.Should().Be(2);
        stats.FourStarCount.Should().Be(1);
        stats.ThreeStarCount.Should().Be(1);
        stats.TwoStarCount.Should().Be(1);
        stats.OneStarCount.Should().Be(1);
        stats.FeaturedReviewCount.Should().Be(2);
    }
}

public sealed class FeedServiceTests
{
    [Fact]
    public async Task PersonalizedFeed_ScopesTenantStateExpiryAndType()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new FeedService(context, NullLogger<FeedService>.Instance);
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var included = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, tenantId, relevanceScore: 0.8);
        var filtered = PersonalizedFeedItem.Create(userId, FeedItemType.PopularCourse, tenantId, relevanceScore: 0.9);
        var dismissed = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, tenantId);
        dismissed.Dismiss();
        var expired = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, tenantId, expiresInDays: -1);
        var otherTenant = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, Guid.NewGuid());
        context.AddRange(included, filtered, dismissed, expired, otherTenant);
        await context.SaveChangesAsync();

        (await service.GetPersonalizedFeedAsync(userId, tenantId: tenantId)).Value.Should().Equal(filtered, included);
        (await service.GetPersonalizedFeedAsync(userId, filterByType: FeedItemType.NewCourse, tenantId: tenantId)).Value.Should().ContainSingle().Which.Should().BeSameAs(included);
    }

    [Fact]
    public async Task Generation_IsTenantScopedAndIdempotent()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new FeedService(context, NullLogger<FeedService>.Instance);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var discussion = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "Topic", "Body", tenantId: tenantId);
        discussion.IncrementReplies();
        var otherDiscussion = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "Other", "Body", tenantId: Guid.NewGuid());
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5, tenantId: tenantId);
        review.SetModeration(true, true);
        var otherReview = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5, tenantId: Guid.NewGuid());
        otherReview.SetModeration(true, true);
        context.AddRange(discussion, otherDiscussion, review, otherReview);
        await context.SaveChangesAsync();

        (await service.GenerateFeedItemsAsync(userId, tenantId)).Value.Should().Be(2);
        (await service.GenerateFeedItemsAsync(userId, tenantId)).Value.Should().Be(0);
        context.Set<PersonalizedFeedItem>().Should().HaveCount(2);
    }

    [Fact]
    public async Task ViewDismissAndCleanup_EnforceOwnershipAndCoverFailures()
    {
        await using var context = SocialTestDbContext.Create();
        var service = new FeedService(context, NullLogger<FeedService>.Instance);
        var userId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        (await service.MarkFeedItemViewedAsync(missingId, userId)).IsSuccess.Should().BeFalse();
        (await service.DismissFeedItemAsync(missingId, userId)).IsSuccess.Should().BeFalse();

        var item = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse);
        context.Add(item);
        await context.SaveChangesAsync();
        (await service.MarkFeedItemViewedAsync(item.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.DismissFeedItemAsync(item.Id, Guid.NewGuid())).IsSuccess.Should().BeFalse();
        (await service.MarkFeedItemViewedAsync(item.Id, userId)).Value.IsViewed.Should().BeTrue();
        (await service.DismissFeedItemAsync(item.Id, userId)).Value.IsDismissed.Should().BeTrue();

        var expired = PersonalizedFeedItem.Create(userId, FeedItemType.NewCourse, expiresInDays: -1);
        context.Add(expired);
        await context.SaveChangesAsync();
        (await service.ClearExpiredFeedItemsAsync()).Value.Should().Be(1);
        (await service.ClearExpiredFeedItemsAsync()).Value.Should().Be(0);
    }
}

internal sealed class SocialTestDbContext(DbContextOptions<SocialTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public static SocialTestDbContext Create() => new(
        new DbContextOptionsBuilder<SocialTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new SocialModelConfiguration().Configure(modelBuilder);
    }
}
