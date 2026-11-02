using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Blog.Commands;
using GameGuild.Social.Blog.Queries;
using GameGuild.Social.Blog.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

/// <summary>
/// SystemAdmin moderation surface (issue #96): force-unpublish/soft-delete any post, remove any
/// comment with the moderator recorded, platform-wide admin reads, and legacy slug resolution.
/// </summary>
public class BlogAdminServiceTests
{
    private readonly BlogServiceTestHarness _h = new();

    [Fact]
    public async Task AdminUnpublish_TakesDownAnyPost_RegardlessOfAuthor()
    {
        var post = await _h.CreatePostAsync();
        await _h.PublishAsync(post.Id);

        var takenDown = await _h.Service.AdminUnpublishAsync(Guid.NewGuid(), post.Id);

        takenDown.Status.Should().Be(BlogPostStatus.Draft);
    }

    [Fact]
    public async Task AdminUnpublish_UnknownPost_Throws404()
    {
        var act = () => _h.Service.AdminUnpublishAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AdminDeletePost_SoftDeletesAnyPost()
    {
        var post = await _h.CreatePostAsync();
        _h.Context.Entry(post).Property(p => p.Version).CurrentValue = 1;

        await _h.Service.AdminDeletePostAsync(Guid.NewGuid(), post.Id);

        post.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminDeleteComment_RemovesAnyComment_AndRecordsModerator()
    {
        var post = await _h.CreatePostAsync();
        await _h.PublishAsync(post.Id);
        var comment = await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "Needs moderation");
        var moderator = Guid.NewGuid();
        post.CommentsCount.Should().Be(1);

        await _h.Service.AdminDeleteCommentAsync(moderator, comment.Id);

        comment.DeletedAt.Should().NotBeNull();
        comment.DeletedByUserId.Should().Be(moderator);
        (await _h.Context.Set<BlogPost>().SingleAsync(p => p.Id == post.Id)).CommentsCount.Should().Be(0);
    }

    [Fact]
    public async Task AdminDeleteComment_UnknownComment_Throws404()
    {
        var act = () => _h.Service.AdminDeleteCommentAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void AdminCommands_RequireSystemAdminActor()
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(ActorContext.Anonymous);

        var act = () => BlogAdminCommandHandlers.RequireSystemAdmin(accessor.Object);
        act.Should().Throw<UnauthorizedAccessException>();
    }
}

/// <summary>Admin read-side queries (issue #96): list-all posts, moderation comment feed, stats, legacy slug resolution.</summary>
public class BlogAdminQueryTests
{
    private readonly BlogServiceTestHarness _h = new();

    private async Task<(Guid author, Guid stranger)> SeedAsync()
    {
        _h.AddProfile(_h.Primary, "alpha");
        _h.AddProfile(_h.Stranger, "beta");

        var published = await _h.CreatePostAsync(_h.Primary, "Published one");
        await _h.PublishAsync(published.Id);
        await _h.Service.AddCommentAsync(_h.Stranger, published.Id, "Nice post");

        var draft = await _h.CreatePostAsync(_h.Stranger, "Draft two");
        await Task.Delay(10); // distinct UpdatedAt ordering
        return (published.Id, draft.Id);
    }

    [Fact]
    public async Task AdminPosts_ListsAllStatusesAcrossAuthors()
    {
        await SeedAsync();

        var page = await new ListBlogAdminPostsQueryHandler(_h.Context).Handle(new ListBlogAdminPostsQuery(), CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.HasMore.Should().BeFalse();
        page.Items.Select(p => p.Status).Should().Contain(BlogPostStatus.Published).And.Contain(BlogPostStatus.Draft);
        page.Items.Select(p => p.PrimaryAuthorHandle).Should().Contain("alpha").And.Contain("beta");
    }

    [Fact]
    public async Task AdminPosts_StatusFilterNarrows()
    {
        await SeedAsync();

        var page = await new ListBlogAdminPostsQueryHandler(_h.Context)
            .Handle(new ListBlogAdminPostsQuery(0, BlogPostStatus.Draft), CancellationToken.None);

        page.Items.Should().OnlyContain(p => p.Status == BlogPostStatus.Draft);
    }

    [Fact]
    public async Task AdminPosts_ExcludesSoftDeleted()
    {
        var (publishedId, _) = await SeedAsync();
        var published = await _h.Context.Set<BlogPost>().SingleAsync(p => p.Id == publishedId);
        _h.Context.Entry(published).Property(p => p.Version).CurrentValue = 1;
        await _h.Service.AdminDeletePostAsync(Guid.NewGuid(), publishedId);

        var page = await new ListBlogAdminPostsQueryHandler(_h.Context).Handle(new ListBlogAdminPostsQuery(), CancellationToken.None);

        page.Items.Should().OnlyContain(p => p.Id != publishedId);
    }

    [Fact]
    public async Task AdminComments_ListsNewestFirstWithPostContext()
    {
        var (publishedId, _) = await SeedAsync();

        var page = await new ListBlogAdminCommentsQueryHandler(_h.Context).Handle(new ListBlogAdminCommentsQuery(), CancellationToken.None);

        page.Items.Should().ContainSingle();
        var comment = page.Items[0];
        comment.BlogPostId.Should().Be(publishedId);
        comment.AuthorHandle.Should().Be("beta");
        comment.PostHandle.Should().Be("alpha");
        comment.PostSlug.Should().NotBeNullOrEmpty();
        comment.Content.Should().Be("Nice post");
    }

    [Fact]
    public async Task AdminStats_CountsStatusesAndRecentComments()
    {
        await SeedAsync();

        var stats = await new GetBlogAdminStatsQueryHandler(_h.Context).Handle(new GetBlogAdminStatsQuery(), CancellationToken.None);

        stats.PublishedPosts.Should().Be(1);
        stats.DraftPosts.Should().Be(1);
        stats.CommentsLast30Days.Should().Be(1);
    }

    [Fact]
    public async Task LegacySlugResolution_UniqueSlug_Resolves()
    {
        var (publishedId, _) = await SeedAsync();
        var published = await _h.Context.Set<BlogPost>().SingleAsync(p => p.Id == publishedId);

        var resolution = await new ResolveLegacyBlogSlugQueryHandler(_h.Context)
            .Handle(new ResolveLegacyBlogSlugQuery(published.Slug), CancellationToken.None);

        resolution.Should().NotBeNull();
        resolution!.Handle.Should().Be("alpha");
        resolution.Slug.Should().Be(published.Slug);
    }

    [Fact]
    public async Task LegacySlugResolution_AmbiguousSlug_ReturnsNull()
    {
        _h.AddProfile(_h.Primary, "alpha");
        _h.AddProfile(_h.Stranger, "beta");
        var one = await _h.CreatePostAsync(_h.Primary, "Same title");
        var two = await _h.CreatePostAsync(_h.Stranger, "Same title");
        await _h.PublishAsync(one.Id);
        await _h.Service.PublishAsync(_h.Stranger, two.Id);

        var resolution = await new ResolveLegacyBlogSlugQueryHandler(_h.Context)
            .Handle(new ResolveLegacyBlogSlugQuery("same-title"), CancellationToken.None);

        resolution.Should().BeNull();
    }

    [Fact]
    public async Task LegacySlugResolution_DraftOnlySlug_ReturnsNull()
    {
        await _h.CreatePostAsync(_h.Primary, "Unpublished thing");

        var resolution = await new ResolveLegacyBlogSlugQueryHandler(_h.Context)
            .Handle(new ResolveLegacyBlogSlugQuery("unpublished-thing"), CancellationToken.None);

        resolution.Should().BeNull();
    }
}
