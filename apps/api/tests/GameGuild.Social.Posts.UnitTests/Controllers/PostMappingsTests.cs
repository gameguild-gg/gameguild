using System.Reflection;
using FluentAssertions;

using Xunit;

namespace GameGuild.Social.Posts.Controllers.Tests;

/// <summary>
///     Tests for the PostMappings static mapping methods to boost coverage.
/// </summary>
public class PostMappingsTests
{
    [Fact]
    public void MapToDto_ShouldMapAllPostProperties()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var post = Post.Create(authorId, "Test content", PostVisibility.Followers, tenantId);
        post.Pin();
        post.Edit("Edited content");
        post.IncrementLikes();
        post.IncrementComments();
        post.IncrementShares();
        post.IncrementViews();

        // Act
        var dto = PostMappings.MapToDto(post);

        // Assert
        dto.Id.Should().Be(post.Id);
        dto.AuthorId.Should().Be(authorId);
        dto.TenantId.Should().Be(tenantId);
        dto.Content.Should().Be("Edited content");
        dto.Visibility.Should().Be("Followers");
        dto.IsPinned.Should().BeTrue();
        dto.IsEdited.Should().BeTrue();
        dto.EditedAt.Should().NotBeNull();
        dto.LikesCount.Should().Be(1);
        dto.CommentsCount.Should().Be(1);
        dto.SharesCount.Should().Be(1);
        dto.ViewsCount.Should().Be(1);
        dto.ReplyToPostId.Should().BeNull();
        dto.RepostOfPostId.Should().BeNull();
        dto.CreatedAt.Should().BeAfter(DateTime.MinValue);
        dto.UpdatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void MapToDto_MinimalPost_ShouldMapDefaults()
    {
        // Arrange
        var post = Post.Create(Guid.NewGuid(), "Minimal");

        // Act
        var dto = PostMappings.MapToDto(post);

        // Assert
        dto.Content.Should().Be("Minimal");
        dto.MediaUrl.Should().BeNull();
        dto.MediaType.Should().BeNull();
        dto.Visibility.Should().Be("Public");
        dto.IsPinned.Should().BeFalse();
        dto.IsEdited.Should().BeFalse();
        dto.EditedAt.Should().BeNull();
        dto.LikesCount.Should().Be(0);
        dto.CommentsCount.Should().Be(0);
        dto.SharesCount.Should().Be(0);
        dto.ViewsCount.Should().Be(0);
    }

    [Fact]
    public void MapToDto_WithMediaType_ShouldMapMediaTypeName()
    {
        var post = Post.Create(Guid.NewGuid(), "With media");
        typeof(Post).GetProperty(nameof(Post.MediaUrl), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(post, "https://example.com/image.png");
        typeof(Post).GetProperty(nameof(Post.MediaType), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(post, MediaType.Image);

        var dto = PostMappings.MapToDto(post);

        dto.MediaUrl.Should().Be("https://example.com/image.png");
        dto.MediaType.Should().Be("Image");
    }

    [Fact]
    public void MapCommentToDto_ShouldMapAllCommentProperties()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var comment = PostComment.Create(postId, authorId, "Test comment", parentId);
        comment.Edit("Edited comment");
        comment.IncrementLikes();

        // Act
        var dto = PostMappings.MapCommentToDto(comment);

        // Assert
        dto.Id.Should().Be(comment.Id);
        dto.PostId.Should().Be(postId);
        dto.AuthorId.Should().Be(authorId);
        dto.ParentCommentId.Should().Be(parentId);
        dto.Content.Should().Be("Edited comment");
        dto.IsEdited.Should().BeTrue();
        dto.EditedAt.Should().NotBeNull();
        dto.LikesCount.Should().Be(1);
        dto.CreatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void MapCommentToDto_TopLevelComment_ShouldHaveNullParent()
    {
        // Arrange
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Root comment");

        // Act
        var dto = PostMappings.MapCommentToDto(comment);

        // Assert
        dto.ParentCommentId.Should().BeNull();
        dto.IsEdited.Should().BeFalse();
        dto.LikesCount.Should().Be(0);
    }

    [Fact]
    public void MapTagToDto_ShouldMapAllTagProperties()
    {
        // Arrange
        var tag = PostTag.Create("test-tag", "Test Tag", "gaming", "A test tag", "#FF0000");
        tag.IncrementUsage();
        tag.SetFeatured(true);

        // Act
        var dto = PostMappings.MapTagToDto(tag);

        // Assert
        dto.Id.Should().Be(tag.Id);
        dto.Name.Should().Be("test-tag");
        dto.DisplayName.Should().Be("Test Tag");
        dto.Description.Should().Be("A test tag");
        dto.Category.Should().Be("gaming");
        dto.Color.Should().Be("#FF0000");
        dto.UsageCount.Should().Be(1);
        dto.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void MapTagToDto_MinimalTag_ShouldUseDefaults()
    {
        // Arrange
        var tag = PostTag.Create("simple");

        // Act
        var dto = PostMappings.MapTagToDto(tag);

        // Assert
        dto.Name.Should().Be("simple");
        dto.UsageCount.Should().Be(0);
        dto.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void MapStatisticsToDto_ShouldMapAllStatisticsProperties()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var stats = PostStatistics.Create(postId);
        stats.IncrementViews(isUnique: true);
        stats.IncrementViews(isUnique: false);
        stats.IncrementExternalShares();
        stats.UpdateEngagementTime(30.0);
        stats.RecalculateScores(likesCount: 5, commentsCount: 3, sharesCount: 1, hoursOld: 2);

        // Act
        var dto = PostMappings.MapStatisticsToDto(stats);

        // Assert
        dto.PostId.Should().Be(postId);
        dto.ViewsCount.Should().Be(2);
        dto.UniqueViewersCount.Should().Be(1);
        dto.ExternalSharesCount.Should().Be(1);
        dto.AverageEngagementTime.Should().BeGreaterThan(0);
        dto.EngagementScore.Should().BeGreaterThan(0);
        dto.TrendingScore.Should().BeGreaterThan(0);
        dto.LastCalculatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void MapFollowerToDto_ShouldMapAllFollowerProperties()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var follower = PostFollower.Create(
            postId,
            userId,
            notifyOnComments: true,
            notifyOnLikes: false,
            notifyOnShares: true,
            notifyOnUpdates: false);

        // Act
        var dto = PostMappings.MapFollowerToDto(follower);

        // Assert
        dto.PostId.Should().Be(postId);
        dto.UserId.Should().Be(userId);
        dto.NotifyOnComments.Should().BeTrue();
        dto.NotifyOnLikes.Should().BeFalse();
        dto.NotifyOnShares.Should().BeTrue();
        dto.NotifyOnUpdates.Should().BeFalse();
        dto.CreatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void MapFollowerToDto_DefaultNotifications_ShouldUseDefaults()
    {
        // Arrange
        var follower = PostFollower.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var dto = PostMappings.MapFollowerToDto(follower);

        // Assert
        dto.NotifyOnComments.Should().BeTrue();
        dto.NotifyOnLikes.Should().BeFalse();
        dto.NotifyOnShares.Should().BeFalse();
        dto.NotifyOnUpdates.Should().BeTrue();
    }
}
