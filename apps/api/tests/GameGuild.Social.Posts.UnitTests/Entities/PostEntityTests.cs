using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Posts.Tests;

/// <summary>
/// Unit tests for Post entity domain logic.
/// </summary>
public class PostEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var authorId = Guid.NewGuid();
        var post = Post.Create(authorId, "Hello world");

        post.Id.Should().NotBeEmpty();
        post.AuthorId.Should().Be(authorId);
        post.Content.Should().Be("Hello world");
        post.Visibility.Should().Be(PostVisibility.Public);
        post.IsPinned.Should().BeFalse();
        post.IsEdited.Should().BeFalse();
        post.LikesCount.Should().Be(0);
        post.CommentsCount.Should().Be(0);
        post.SharesCount.Should().Be(0);
        post.ViewsCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithVisibility_ShouldSetVisibility()
    {
        var post = Post.Create(Guid.NewGuid(), "Private post", PostVisibility.Private);
        post.Visibility.Should().Be(PostVisibility.Private);
    }

    [Fact]
    public void Edit_ShouldUpdateContentAndMarkEdited()
    {
        var post = Post.Create(Guid.NewGuid(), "Original");

        post.Edit("Updated");

        post.Content.Should().Be("Updated");
        post.IsEdited.Should().BeTrue();
        post.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public void Pin_ShouldSetIsPinnedTrue()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.Pin();
        post.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void Unpin_ShouldSetIsPinnedFalse()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.Pin();
        post.Unpin();
        post.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void IncrementLikes_ShouldIncreaseLikesCount()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.IncrementLikes();
        post.IncrementLikes();
        post.LikesCount.Should().Be(2);
    }

    [Fact]
    public void DecrementLikes_ShouldNotGoBelowZero()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.DecrementLikes();
        post.LikesCount.Should().Be(0);
    }

    [Fact]
    public void DecrementLikes_ShouldDecrease()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.IncrementLikes();
        post.IncrementLikes();
        post.DecrementLikes();
        post.LikesCount.Should().Be(1);
    }

    [Fact]
    public void IncrementComments_ShouldIncreaseCount()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.IncrementComments();
        post.CommentsCount.Should().Be(1);
    }

    [Fact]
    public void DecrementComments_ShouldNotGoBelowZero()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.DecrementComments();
        post.CommentsCount.Should().Be(0);
    }

    [Fact]
    public void IncrementShares_ShouldIncreaseCount()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.IncrementShares();
        post.SharesCount.Should().Be(1);
    }

    [Fact]
    public void IncrementViews_ShouldIncreaseCount()
    {
        var post = Post.Create(Guid.NewGuid(), "Test");
        post.IncrementViews();
        post.IncrementViews();
        post.ViewsCount.Should().Be(2);
    }
}

/// <summary>
/// Unit tests for PostComment entity domain logic.
/// </summary>
public class PostCommentEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var comment = PostComment.Create(postId, authorId, "Nice post!");

        comment.Id.Should().NotBeEmpty();
        comment.PostId.Should().Be(postId);
        comment.AuthorId.Should().Be(authorId);
        comment.Content.Should().Be("Nice post!");
        comment.ParentCommentId.Should().BeNull();
        comment.IsEdited.Should().BeFalse();
        comment.LikesCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithParent_ShouldSetParentCommentId()
    {
        var parentId = Guid.NewGuid();
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Reply", parentId);
        comment.ParentCommentId.Should().Be(parentId);
    }

    [Fact]
    public void Edit_ShouldUpdateContentAndMarkEdited()
    {
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");
        comment.Edit("Updated");

        comment.Content.Should().Be("Updated");
        comment.IsEdited.Should().BeTrue();
        comment.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public void IncrementLikes_ShouldIncrease()
    {
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Test");
        comment.IncrementLikes();
        comment.LikesCount.Should().Be(1);
    }

    [Fact]
    public void DecrementLikes_ShouldNotGoBelowZero()
    {
        var comment = PostComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Test");
        comment.DecrementLikes();
        comment.LikesCount.Should().Be(0);
    }
}
