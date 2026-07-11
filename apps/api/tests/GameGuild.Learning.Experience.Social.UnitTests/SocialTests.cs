using FluentAssertions;
using GameGuild.Learning.Experience.Social;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public class CourseReviewTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        var review = CourseReview.Create(
            courseId, userId, 4,
            title: "Great course",
            content: "Learned a lot",
            enrollmentId: enrollmentId);

        review.Id.Should().NotBeEmpty();
        review.CourseId.Should().Be(courseId);
        review.UserId.Should().Be(userId);
        review.Rating.Should().Be(4);
        review.Title.Should().Be("Great course");
        review.Content.Should().Be("Learned a lot");
        review.EnrollmentId.Should().Be(enrollmentId);
        review.IsVerifiedPurchase.Should().BeTrue();
        review.HelpfulCount.Should().Be(0);
        review.IsApproved.Should().BeFalse();
        review.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutEnrollment_ShouldNotBeVerified()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 3);

        review.IsVerifiedPurchase.Should().BeFalse();
        review.EnrollmentId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldClampRating()
    {
        var tooHigh = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        var tooLow = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), -5);

        tooHigh.Rating.Should().Be(5);
        tooLow.Rating.Should().Be(1);
    }

    [Fact]
    public void MarkHelpful_ShouldIncrementCount()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        review.MarkHelpful();
        review.MarkHelpful();

        review.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void Approve_ShouldSetFlag()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        review.Approve();

        review.IsApproved.Should().BeTrue();
    }

    [Fact]
    public void Feature_ShouldSetFlag()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        review.Feature();

        review.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void SetModeration_ShouldAllowApprovingFeaturingAndRevertingBothFlags()
    {
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        review.SetModeration(isApproved: true, isFeatured: true);
        review.IsApproved.Should().BeTrue();
        review.IsFeatured.Should().BeTrue();

        review.SetModeration(isApproved: false, isFeatured: false);
        review.IsApproved.Should().BeFalse();
        review.IsFeatured.Should().BeFalse();
    }
}

public class CourseWishlistTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var wishlist = CourseWishlist.Create(courseId, userId);

        wishlist.Id.Should().NotBeEmpty();
        wishlist.CourseId.Should().Be(courseId);
        wishlist.UserId.Should().Be(userId);
        wishlist.NotifyOnSale.Should().BeTrue();
        wishlist.NotifyOnUpdate.Should().BeFalse();
    }
}

public class CourseDiscussionTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        var discussion = CourseDiscussion.Create(
            courseId, authorId, "Help with lesson 3",
            "I'm stuck on the third exercise",
            contentId);

        discussion.Id.Should().NotBeEmpty();
        discussion.CourseId.Should().Be(courseId);
        discussion.AuthorId.Should().Be(authorId);
        discussion.ContentId.Should().Be(contentId);
        discussion.Title.Should().Be("Help with lesson 3");
        discussion.Content.Should().Be("I'm stuck on the third exercise");
        discussion.IsPinned.Should().BeFalse();
        discussion.IsResolved.Should().BeFalse();
        discussion.ReplyCount.Should().Be(0);
        discussion.ViewCount.Should().Be(0);
        discussion.LastActivityAt.Should().NotBeNull();
    }

    [Fact]
    public void Pin_Unpin_ShouldToggle()
    {
        var disc = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "T", "C");

        disc.Pin();
        disc.IsPinned.Should().BeTrue();

        disc.Unpin();
        disc.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void MarkResolved_ShouldSetFlag()
    {
        var disc = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "T", "C");

        disc.MarkResolved();

        disc.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void IncrementViews_ShouldIncrement()
    {
        var disc = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "T", "C");

        disc.IncrementViews();
        disc.IncrementViews();

        disc.ViewCount.Should().Be(2);
    }

    [Fact]
    public void IncrementReplies_ShouldIncrementAndUpdateActivity()
    {
        var disc = CourseDiscussion.Create(Guid.NewGuid(), Guid.NewGuid(), "T", "C");

        disc.IncrementReplies();

        disc.ReplyCount.Should().Be(1);
        disc.LastActivityAt.Should().NotBeNull();
    }
}

public class DiscussionReplyTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var discId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var reply = DiscussionReply.Create(discId, authorId, "Try this approach", parentId);

        reply.Id.Should().NotBeEmpty();
        reply.DiscussionId.Should().Be(discId);
        reply.AuthorId.Should().Be(authorId);
        reply.ParentReplyId.Should().Be(parentId);
        reply.Content.Should().Be("Try this approach");
        reply.IsAcceptedAnswer.Should().BeFalse();
        reply.UpvoteCount.Should().Be(0);
    }

    [Fact]
    public void AcceptAsAnswer_ShouldSetFlag()
    {
        var reply = DiscussionReply.Create(Guid.NewGuid(), Guid.NewGuid(), "Answer");

        reply.AcceptAsAnswer();

        reply.IsAcceptedAnswer.Should().BeTrue();
    }

    [Fact]
    public void Upvote_ShouldIncrement()
    {
        var reply = DiscussionReply.Create(Guid.NewGuid(), Guid.NewGuid(), "Reply");

        reply.Upvote();
        reply.Upvote();
        reply.Upvote();

        reply.UpvoteCount.Should().Be(3);
    }
}

public class CourseLikeTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var like = CourseLike.Create(courseId, userId, tenantId);

        like.Id.Should().NotBeEmpty();
        like.CourseId.Should().Be(courseId);
        like.UserId.Should().Be(userId);
        like.TenantId.Should().Be(tenantId);
    }
}

public class PersonalizedFeedItemTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var item = PersonalizedFeedItem.Create(
            userId, FeedItemType.NewCourse,
            courseId: courseId,
            relevanceScore: 0.85,
            reason: "Based on your interests");

        item.Id.Should().NotBeEmpty();
        item.UserId.Should().Be(userId);
        item.ItemType.Should().Be(FeedItemType.NewCourse);
        item.CourseId.Should().Be(courseId);
        item.RelevanceScore.Should().BeApproximately(0.85, 0.001);
        item.Reason.Should().Be("Based on your interests");
        item.IsViewed.Should().BeFalse();
        item.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldClampRelevanceScore()
    {
        var high = PersonalizedFeedItem.Create(Guid.NewGuid(), FeedItemType.NewCourse, relevanceScore: 5.0);
        var low = PersonalizedFeedItem.Create(Guid.NewGuid(), FeedItemType.NewCourse, relevanceScore: -1.0);

        high.RelevanceScore.Should().Be(1.0);
        low.RelevanceScore.Should().Be(0.0);
    }

    [Fact]
    public void MarkViewed_ShouldSetFlag()
    {
        var item = PersonalizedFeedItem.Create(Guid.NewGuid(), FeedItemType.PopularCourse);

        item.MarkViewed();

        item.IsViewed.Should().BeTrue();
    }

    [Fact]
    public void Dismiss_ShouldSetFlag()
    {
        var item = PersonalizedFeedItem.Create(Guid.NewGuid(), FeedItemType.TrendingDiscussion);

        item.Dismiss();

        item.IsDismissed.Should().BeTrue();
    }
}

public class FeedItemTypeEnumTests
{
    [Fact]
    public void ShouldHave10Values()
    {
        Enum.GetValues<FeedItemType>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData(FeedItemType.NewCourse, 0)]
    [InlineData(FeedItemType.SkillMilestone, 9)]
    public void ExtremeValues(FeedItemType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}
