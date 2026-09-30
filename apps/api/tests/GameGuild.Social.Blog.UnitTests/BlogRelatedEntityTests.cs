using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public class BlogRelatedEntityTests
{
    [Fact]
    public void BlogPostAuthor_Create_SetsFields()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var author = BlogPostAuthor.Create(postId, userId, addedBy, now);

        author.BlogPostId.Should().Be(postId);
        author.UserId.Should().Be(userId);
        author.AddedByUserId.Should().Be(addedBy);
        author.AddedAt.Should().Be(now);
    }

    [Fact]
    public void BlogSlugHistory_Create_SetsFields()
    {
        var postId = Guid.NewGuid();
        var previousAuthor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var history = BlogSlugHistory.Create(postId, previousAuthor, "old-slug", now);

        history.BlogPostId.Should().Be(postId);
        history.PreviousPrimaryAuthorId.Should().Be(previousAuthor);
        history.PreviousSlug.Should().Be("old-slug");
        history.ChangedAt.Should().Be(now);
    }

    [Fact]
    public void BlogComment_Create_TrimsContentAndDefaults()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = BlogComment.Create(postId, authorId, "  Hello!  ");

        comment.BlogPostId.Should().Be(postId);
        comment.AuthorUserId.Should().Be(authorId);
        comment.Content.Should().Be("Hello!");
        comment.ParentCommentId.Should().BeNull();
        comment.DeletedAt.Should().BeNull();
        comment.DeletedByUserId.Should().BeNull();
    }

    [Fact]
    public void BlogComment_Create_WithParent_SetsParentId()
    {
        var parent = Guid.NewGuid();
        var comment = BlogComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Reply", parent);
        comment.ParentCommentId.Should().Be(parent);
    }

    [Theory]
    [InlineData(2001)]
    [InlineData(5000)]
    public void BlogComment_Create_RejectsContentOver2000Chars(int length)
    {
        var act = () => BlogComment.Create(Guid.NewGuid(), Guid.NewGuid(), new string('x', length));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BlogComment_Delete_SoftDeletes()
    {
        var moderator = Guid.NewGuid();
        var comment = BlogComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Hello");

        comment.Delete(moderator, DateTimeOffset.UtcNow);

        comment.DeletedAt.Should().NotBeNull();
        comment.DeletedByUserId.Should().Be(moderator);
    }

    [Fact]
    public void BlogComment_Delete_IsIdempotent()
    {
        var moderator = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var comment = BlogComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Hello");
        comment.Delete(moderator, now);
        var deletedAt = comment.DeletedAt;

        comment.Delete(Guid.NewGuid(), now.AddHours(1));

        comment.DeletedAt.Should().Be(deletedAt);
        comment.DeletedByUserId.Should().Be(moderator);
    }
}
