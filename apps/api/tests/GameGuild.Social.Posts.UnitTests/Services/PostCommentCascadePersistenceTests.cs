using FluentAssertions;
using GameGuild.Social.Posts.Configuration;
using GameGuild.Social.Posts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Social.Posts.Tests.Services;

public sealed class PostCommentCascadePersistenceTests
{
    [Fact]
    public async Task DeleteCommentAsync_Parent_PersistsArbitrarilyDeepActiveCascadeAndExactCount()
    {
        await using var context = CreateContext();
        var post = PersistedPost(commentCount: 4);
        var otherPost = PersistedPost(commentCount: 1);
        var actorId = Guid.NewGuid();
        var root = PersistedComment(post.Id, actorId, "Root");
        var child = PersistedComment(post.Id, Guid.NewGuid(), "Child", root.Id);
        var nested = PersistedComment(post.Id, Guid.NewGuid(), "Nested", child.Id);
        var sibling = PersistedComment(post.Id, Guid.NewGuid(), "Sibling");
        var crossPost = PersistedComment(otherPost.Id, Guid.NewGuid(), "Cross-post", root.Id);
        context.AddRange(post, otherPost, root, child, nested, sibling, crossPost);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var result = await service.DeleteCommentAsync(root.Id, actorId);

        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();
        var persistedComments = await context.Set<PostComment>().ToDictionaryAsync(comment => comment.Id);
        persistedComments[root.Id].IsDeleted.Should().BeTrue();
        persistedComments[child.Id].IsDeleted.Should().BeTrue();
        persistedComments[nested.Id].IsDeleted.Should().BeTrue();
        persistedComments[sibling.Id].IsDeleted.Should().BeFalse();
        persistedComments[crossPost.Id].IsDeleted.Should().BeFalse();
        (await context.Set<Post>().SingleAsync(candidate => candidate.Id == post.Id)).CommentsCount.Should().Be(1);
        (await context.Set<Post>().SingleAsync(candidate => candidate.Id == otherPost.Id)).CommentsCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteCommentAsync_Leaf_PersistsOnlyLeafAndDecrementsOnce()
    {
        await using var context = CreateContext();
        var post = PersistedPost(commentCount: 3);
        var actorId = Guid.NewGuid();
        var root = PersistedComment(post.Id, Guid.NewGuid(), "Root");
        var leaf = PersistedComment(post.Id, actorId, "Leaf", root.Id);
        var sibling = PersistedComment(post.Id, Guid.NewGuid(), "Sibling", root.Id);
        context.AddRange(post, root, leaf, sibling);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateService(context).DeleteCommentAsync(leaf.Id, actorId);

        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();
        var persistedComments = await context.Set<PostComment>().ToDictionaryAsync(comment => comment.Id);
        persistedComments[root.Id].IsDeleted.Should().BeFalse();
        persistedComments[leaf.Id].IsDeleted.Should().BeTrue();
        persistedComments[sibling.Id].IsDeleted.Should().BeFalse();
        (await context.Set<Post>().SingleAsync()).CommentsCount.Should().Be(2);
    }

    private static PostCommentService CreateService(IApplicationDbContext context) =>
        new(context, NullLogger<PostCommentService>.Instance);

    private static Post PersistedPost(int commentCount)
    {
        var post = Post.Create(Guid.NewGuid(), "Post", PostVisibility.Public);
        post.Version = 1;
        for (var index = 0; index < commentCount; index++) post.IncrementComments();
        return post;
    }

    private static PostComment PersistedComment(Guid postId, Guid authorId, string content, Guid? parentId = null)
    {
        var comment = PostComment.Create(postId, authorId, content, parentId);
        comment.Version = 1;
        return comment;
    }

    private static TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"social-post-comments-{Guid.NewGuid():N}")
            .Options;
        return new TestContext(options);
    }

    private sealed class TestContext(DbContextOptions<TestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new PostsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A single SaveChanges call is atomic for this persistence test.");
    }
}
