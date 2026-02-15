using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Feed.UnitTests;

public class FeedItemTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-1);

        var item = FeedItem.Create(userId, contentId, FeedContentType.Post, authorId,
            FeedItemReason.Following, createdAt, 2.5);

        item.UserId.Should().Be(userId);
        item.ContentId.Should().Be(contentId);
        item.ContentType.Should().Be(FeedContentType.Post);
        item.AuthorId.Should().Be(authorId);
        item.Reason.Should().Be(FeedItemReason.Following);
        item.ContentCreatedAt.Should().Be(createdAt);
        item.RelevanceScore.Should().Be(2.5);
        item.IsRead.Should().BeFalse();
        item.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void Create_DefaultRelevanceScore()
    {
        var item = FeedItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            FeedContentType.BlogPost, Guid.NewGuid(), FeedItemReason.Trending, DateTime.UtcNow);
        item.RelevanceScore.Should().Be(1.0);
    }

    [Fact]
    public void MarkRead_SetsIsRead()
    {
        var item = FeedItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            FeedContentType.Achievement, Guid.NewGuid(), FeedItemReason.Recommended, DateTime.UtcNow);
        item.MarkRead();
        item.IsRead.Should().BeTrue();
    }

    [Fact]
    public void Hide_SetsIsHidden()
    {
        var item = FeedItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            FeedContentType.CourseReview, Guid.NewGuid(), FeedItemReason.InNetwork, DateTime.UtcNow);
        item.Hide();
        item.IsHidden.Should().BeTrue();
    }
}

public class FeedEnumTests
{
    [Fact]
    public void FeedContentType_AllValues()
    {
        var values = Enum.GetValues<FeedContentType>();
        values.Should().Contain(FeedContentType.Post);
        values.Should().Contain(FeedContentType.BlogPost);
        values.Should().Contain(FeedContentType.CourseReview);
        values.Should().Contain(FeedContentType.ProjectUpdate);
        values.Should().Contain(FeedContentType.Achievement);
        values.Should().Contain(FeedContentType.CourseCompletion);
    }

    [Fact]
    public void FeedItemReason_AllValues()
    {
        var values = Enum.GetValues<FeedItemReason>();
        values.Should().Contain(FeedItemReason.Following);
        values.Should().Contain(FeedItemReason.Trending);
        values.Should().Contain(FeedItemReason.Recommended);
        values.Should().Contain(FeedItemReason.Mentioned);
        values.Should().Contain(FeedItemReason.Replied);
        values.Should().Contain(FeedItemReason.Liked);
        values.Should().Contain(FeedItemReason.InNetwork);
    }
}
