using FluentAssertions;
using GameGuild.Social.Posts.Events;
using Xunit;

namespace GameGuild.Social.Posts.Tests;

/// <summary>
/// Tests for PostStatistics entity.
/// </summary>
public class PostStatisticsTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var postId = Guid.NewGuid();
        var stats = PostStatistics.Create(postId);

        stats.Id.Should().NotBeEmpty();
        stats.PostId.Should().Be(postId);
        stats.ViewsCount.Should().Be(0);
        stats.UniqueViewersCount.Should().Be(0);
        stats.ExternalSharesCount.Should().Be(0);
        stats.AverageEngagementTime.Should().Be(0);
        stats.EngagementScore.Should().Be(0);
        stats.TrendingScore.Should().Be(0);
    }

    [Fact]
    public void IncrementViews_NotUnique_ShouldOnlyIncrementViews()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.IncrementViews(false);

        stats.ViewsCount.Should().Be(1);
        stats.UniqueViewersCount.Should().Be(0);
    }

    [Fact]
    public void IncrementViews_Unique_ShouldIncrementBoth()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.IncrementViews(true);

        stats.ViewsCount.Should().Be(1);
        stats.UniqueViewersCount.Should().Be(1);
    }

    [Fact]
    public void IncrementExternalShares_ShouldIncrement()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.IncrementExternalShares();
        stats.IncrementExternalShares();

        stats.ExternalSharesCount.Should().Be(2);
    }

    [Fact]
    public void UpdateEngagementTime_FirstView_ShouldSetDirectly()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.UpdateEngagementTime(30.0);

        stats.AverageEngagementTime.Should().Be(30.0);
    }

    [Fact]
    public void UpdateEngagementTime_MultipleViews_ShouldComputeAverage()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.IncrementViews(true);
        stats.IncrementViews(true);
        // ViewsCount is now 2
        stats.UpdateEngagementTime(20.0);

        // With ViewsCount=2: ((0 * 1) + 20.0) / 2 = 10.0
        stats.AverageEngagementTime.Should().Be(10.0);
    }

    [Fact]
    public void RecalculateScores_ShouldComputeEngagementAndTrending()
    {
        var stats = PostStatistics.Create(Guid.NewGuid());
        stats.IncrementViews(true);
        stats.IncrementViews(true);
        stats.IncrementViews(true);
        // UniqueViewersCount = 3

        stats.RecalculateScores(likesCount: 10, commentsCount: 5, sharesCount: 2, hoursOld: 1);

        // EngagementScore = (10*1.0) + (5*2.0) + (2*3.0) + (3*0.1) = 10 + 10 + 6 + 0.3 = 26.3
        stats.EngagementScore.Should().BeApproximately(26.3, 0.01);
        stats.TrendingScore.Should().BeGreaterThan(0);
    }
}

/// <summary>
/// Tests for PostContentReference entity.
/// </summary>
public class PostContentReferenceTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var postId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var reference = PostContentReference.Create(postId, resourceId, "Course", "mention", "context text", 1);

        reference.Id.Should().NotBeEmpty();
        reference.PostId.Should().Be(postId);
        reference.ReferencedResourceId.Should().Be(resourceId);
        reference.ResourceType.Should().Be("Course");
        reference.ReferenceType.Should().Be("mention");
        reference.Context.Should().Be("context text");
        reference.Order.Should().Be(1);
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseMention()
    {
        var reference = PostContentReference.Create(Guid.NewGuid(), Guid.NewGuid(), "Post");

        reference.ReferenceType.Should().Be("mention");
        reference.Context.Should().BeNull();
        reference.Order.Should().Be(0);
    }
}

/// <summary>
/// Tests for PostFollower entity.
/// </summary>
public class PostFollowerTests
{
    [Fact]
    public void Create_ShouldSetDefaultNotifications()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var follower = PostFollower.Create(postId, userId);

        follower.Id.Should().NotBeEmpty();
        follower.PostId.Should().Be(postId);
        follower.UserId.Should().Be(userId);
        follower.NotifyOnComments.Should().BeTrue();
        follower.NotifyOnLikes.Should().BeFalse();
        follower.NotifyOnShares.Should().BeFalse();
        follower.NotifyOnUpdates.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCustomNotifications_ShouldSetCorrectly()
    {
        var follower = PostFollower.Create(Guid.NewGuid(), Guid.NewGuid(),
            notifyOnComments: false, notifyOnLikes: true,
            notifyOnShares: true, notifyOnUpdates: false);

        follower.NotifyOnComments.Should().BeFalse();
        follower.NotifyOnLikes.Should().BeTrue();
        follower.NotifyOnShares.Should().BeTrue();
        follower.NotifyOnUpdates.Should().BeFalse();
    }

    [Fact]
    public void UpdatePreferences_ShouldUpdateOnlyProvidedValues()
    {
        var follower = PostFollower.Create(Guid.NewGuid(), Guid.NewGuid());

        follower.UpdatePreferences(
            notifyOnComments: false,
            notifyOnLikes: true,
            notifyOnShares: null,
            notifyOnUpdates: null);

        follower.NotifyOnComments.Should().BeFalse();
        follower.NotifyOnLikes.Should().BeTrue();
        follower.NotifyOnShares.Should().BeFalse(); // unchanged default
        follower.NotifyOnUpdates.Should().BeTrue(); // unchanged default
    }

    [Fact]
    public void UpdatePreferences_ShouldUpdateShareAndUpdateFlags()
    {
        var follower = PostFollower.Create(Guid.NewGuid(), Guid.NewGuid());

        follower.UpdatePreferences(
            notifyOnComments: null,
            notifyOnLikes: null,
            notifyOnShares: true,
            notifyOnUpdates: false);

        follower.NotifyOnComments.Should().BeTrue();
        follower.NotifyOnLikes.Should().BeFalse();
        follower.NotifyOnShares.Should().BeTrue();
        follower.NotifyOnUpdates.Should().BeFalse();
    }
}

/// <summary>
/// Tests for PostTag entity.
/// </summary>
public class PostTagTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var tag = PostTag.Create("GameDev");

        tag.Id.Should().NotBeEmpty();
        tag.Name.Should().Be("gamedev"); // lowered & trimmed
        tag.DisplayName.Should().Be("GameDev");
        tag.Category.Should().Be("general");
        tag.Description.Should().BeNull();
        tag.Color.Should().BeNull();
        tag.UsageCount.Should().Be(0);
        tag.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAllParams_ShouldSetProperties()
    {
        var tag = PostTag.Create("  Unity3D  ", "Unity 3D", "engine", "Unity game engine", "#FF0000");

        tag.Name.Should().Be("unity3d");
        tag.DisplayName.Should().Be("Unity 3D");
        tag.Category.Should().Be("engine");
        tag.Description.Should().Be("Unity game engine");
        tag.Color.Should().Be("#FF0000");
    }

    [Fact]
    public void IncrementUsage_ShouldIncrease()
    {
        var tag = PostTag.Create("test");
        tag.IncrementUsage();
        tag.IncrementUsage();

        tag.UsageCount.Should().Be(2);
    }

    [Fact]
    public void DecrementUsage_ShouldNotGoBelowZero()
    {
        var tag = PostTag.Create("test");
        tag.DecrementUsage();

        tag.UsageCount.Should().Be(0);
    }

    [Fact]
    public void DecrementUsage_FromPositive_ShouldDecrease()
    {
        var tag = PostTag.Create("test");
        tag.IncrementUsage();
        tag.IncrementUsage();
        tag.DecrementUsage();

        tag.UsageCount.Should().Be(1);
    }

    [Fact]
    public void SetFeatured_ShouldToggle()
    {
        var tag = PostTag.Create("test");
        tag.SetFeatured(true);
        tag.IsFeatured.Should().BeTrue();

        tag.SetFeatured(false);
        tag.IsFeatured.Should().BeFalse();
    }
}

/// <summary>
/// Tests for PostTagAssignment entity.
/// </summary>
public class PostTagAssignmentTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var postId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var assignment = PostTagAssignment.Create(postId, tagId, 2);

        assignment.Id.Should().NotBeEmpty();
        assignment.PostId.Should().Be(postId);
        assignment.TagId.Should().Be(tagId);
        assignment.Order.Should().Be(2);
    }

    [Fact]
    public void Create_DefaultOrder_ShouldBeZero()
    {
        var assignment = PostTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid());
        assignment.Order.Should().Be(0);
    }
}

/// <summary>
/// Tests for PostView entity.
/// </summary>
public class PostViewTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var view = PostView.Create(postId, userId, "192.168.1.1", "Mozilla/5.0", "https://example.com");

        view.Id.Should().NotBeEmpty();
        view.PostId.Should().Be(postId);
        view.UserId.Should().Be(userId);
        view.IpAddress.Should().Be("192.168.1.1");
        view.UserAgent.Should().Be("Mozilla/5.0");
        view.Referrer.Should().Be("https://example.com");
        view.DurationSeconds.Should().Be(0);
        view.IsEngaged.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullUser_ShouldBeNull()
    {
        var view = PostView.Create(Guid.NewGuid(), null);
        view.UserId.Should().BeNull();
    }

    [Fact]
    public void UpdateDuration_ShouldSetDurationAndEngaged()
    {
        var view = PostView.Create(Guid.NewGuid(), Guid.NewGuid());
        view.UpdateDuration(120, true);

        view.DurationSeconds.Should().Be(120);
        view.IsEngaged.Should().BeTrue();
    }
}

/// <summary>
/// Tests for PostLike entity.
/// </summary>
public class PostLikeTests
{
    [Fact]
    public void Create_ShouldSetDefaultReactionType()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var like = PostLike.Create(postId, userId);

        like.Id.Should().NotBeEmpty();
        like.PostId.Should().Be(postId);
        like.UserId.Should().Be(userId);
        like.ReactionType.Should().Be("like");
    }

    [Fact]
    public void Create_WithCustomReaction_ShouldSetType()
    {
        var like = PostLike.Create(Guid.NewGuid(), Guid.NewGuid(), "love");
        like.ReactionType.Should().Be("love");
    }

    [Fact]
    public void ChangeReactionType_ShouldUpdateType()
    {
        var like = PostLike.Create(Guid.NewGuid(), Guid.NewGuid());
        like.ChangeReactionType("celebrate");

        like.ReactionType.Should().Be("celebrate");
    }
}

/// <summary>
/// Tests for PostVisibility and MediaType enums.
/// </summary>
public class PostEnumsTests
{
    [Theory]
    [InlineData(PostVisibility.Public, 0)]
    [InlineData(PostVisibility.Followers, 1)]
    [InlineData(PostVisibility.Private, 2)]
    [InlineData(PostVisibility.Unlisted, 3)]
    public void PostVisibility_ShouldHaveCorrectValues(PostVisibility vis, int expected)
    {
        ((int)vis).Should().Be(expected);
    }

    [Theory]
    [InlineData(MediaType.Image, 0)]
    [InlineData(MediaType.Video, 1)]
    [InlineData(MediaType.Audio, 2)]
    [InlineData(MediaType.Document, 3)]
    public void MediaType_ShouldHaveCorrectValues(MediaType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

/// <summary>
/// Tests for domain events.
/// </summary>
public class PostEventsTests
{
    [Fact]
    public void PostCreatedEvent_ShouldStoreAllProperties()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new PostCreatedEvent(postId, authorId, "Hello", PostVisibility.Public, tenantId, now);

        evt.PostId.Should().Be(postId);
        evt.AuthorId.Should().Be(authorId);
        evt.Content.Should().Be("Hello");
        evt.Visibility.Should().Be(PostVisibility.Public);
        evt.TenantId.Should().Be(tenantId);
        evt.CreatedAt.Should().Be(now);
        evt.EntityId.Should().Be(postId);
        evt.EntityType.Should().Be(nameof(Post));
        evt.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void PostUpdatedEvent_ShouldStoreAllProperties()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new PostUpdatedEvent(postId, authorId, "old", "new", now, null);

        evt.PostId.Should().Be(postId);
        evt.OldContent.Should().Be("old");
        evt.NewContent.Should().Be("new");
        evt.TenantId.Should().BeNull();
    }

    [Fact]
    public void PostDeletedEvent_ShouldStoreAllProperties()
    {
        var evt = new PostDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow, null);
        evt.IsSoftDelete.Should().BeTrue();
    }

    [Fact]
    public void PostLikedEvent_ShouldStoreAllProperties()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var likedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt = new PostLikedEvent(postId, authorId, likedBy, "love", 5, now, null);

        evt.LikedByUserId.Should().Be(likedBy);
        evt.ReactionType.Should().Be("love");
        evt.NewLikesCount.Should().Be(5);
    }

    [Fact]
    public void PostUnlikedEvent_ShouldStoreProperties()
    {
        var evt = new PostUnlikedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, DateTime.UtcNow, null);
        evt.NewLikesCount.Should().Be(3);
    }

    [Fact]
    public void PostCommentedEvent_ShouldStoreProperties()
    {
        var parentId = Guid.NewGuid();
        var evt = new PostCommentedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), parentId, 10, DateTime.UtcNow, null);

        evt.ParentCommentId.Should().Be(parentId);
        evt.NewCommentsCount.Should().Be(10);
    }

    [Fact]
    public void PostSharedEvent_ShouldStoreProperties()
    {
        var evt = new PostSharedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 7, DateTime.UtcNow, null);
        evt.NewSharesCount.Should().Be(7);
    }

    [Fact]
    public void PostPinnedEvent_ShouldStoreProperties()
    {
        var evt = new PostPinnedEvent(Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow, null);
        evt.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void PostViewedEvent_ShouldStoreProperties()
    {
        var evt = new PostViewedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, 42, DateTime.UtcNow, null);
        evt.IsUniqueViewer.Should().BeTrue();
        evt.NewViewsCount.Should().Be(42);
    }

    [Fact]
    public void PostTrendingEvent_ShouldStoreProperties()
    {
        var evt = new PostTrendingEvent(Guid.NewGuid(), Guid.NewGuid(), 95.5, 1, DateTime.UtcNow, null);
        evt.TrendingScore.Should().Be(95.5);
        evt.TrendingRank.Should().Be(1);
    }

    [Fact]
    public void PostTaggedEvent_ShouldStoreProperties()
    {
        var evt = new PostTaggedEvent(Guid.NewGuid(), Guid.NewGuid(), "gamedev", DateTime.UtcNow, null);
        evt.TagName.Should().Be("gamedev");
    }

    [Fact]
    public void PostContentReferencedEvent_ShouldStoreProperties()
    {
        var evt = new PostContentReferencedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Course", "mention", DateTime.UtcNow, null);
        evt.ResourceType.Should().Be("Course");
        evt.ReferenceType.Should().Be("mention");
    }

    [Fact]
    public void DomainEventBase_ShouldGenerateEventIdAndOccurredAt()
    {
        var evt = new PostCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", PostVisibility.Public, null, DateTime.UtcNow);

        evt.EventId.Should().NotBeEmpty();
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

/// <summary>
/// Additional Post entity edge cases.
/// </summary>
public class PostEntityEdgeCaseTests
{
    [Fact]
    public void Post_Create_WithTenantId_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var post = Post.Create(Guid.NewGuid(), "Content", PostVisibility.Private, tenantId);

        post.TenantId.Should().Be(tenantId);
        post.Visibility.Should().Be(PostVisibility.Private);
    }

    [Fact]
    public void PostComment_DecrementLikes_FromPositive_ShouldDecrease()
    {
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "comment");
        comment.IncrementLikes();
        comment.IncrementLikes();
        comment.DecrementLikes();

        comment.LikesCount.Should().Be(1);
    }

    [Fact]
    public void Post_DecrementComments_FromPositive_ShouldDecrease()
    {
        var post = Post.Create(Guid.NewGuid(), "Content");
        post.IncrementComments();
        post.IncrementComments();
        post.DecrementComments();

        post.CommentsCount.Should().Be(1);
    }
}
