using System.Reflection;
using FluentAssertions;
using GameGuild.Social.Blog.Queries;
using GameGuild.Social.Blog.Services;
using GameGuild.Social.Reactions;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

/// <summary>Public read side: summary shape, published-only filtering, keyset paging, detail 404-equivalence, comments, view counter.</summary>
public class BlogPublicQueriesTests
{
    private readonly BlogServiceTestHarness _h = new();

    private async Task<BlogPost> PublishWithBodyAsync(string title, string? content = "one two three four five six seven eight nine ten")
    {
        var post = await _h.CreatePostAsync(title: title);
        if (content is not null)
            post = await _h.Service.UpdateDraftAsync(_h.Primary, post.Id, new Commands.UpdateBlogPostDraftCommand(post.Id, post.Revision, Content: content));
        return await _h.PublishAsync(post.Id);
    }

    [Fact]
    public async Task SummaryDto_ExcludesBodyFields()
    {
        _h.AddProfile(_h.Primary, "writer");
        await PublishWithBodyAsync("Body Is Secret");

        var page = await new ListPublicBlogSummariesQueryHandler(_h.Context).Handle(new ListPublicBlogSummariesQuery(), CancellationToken.None);

        page.Items.Should().HaveCount(1);
        var propertyNames = typeof(BlogPostSummaryDto).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();
        propertyNames.Should().NotContain("Content");
        propertyNames.Should().NotContain("JsonBody");
        propertyNames.Where(name => name.Contains("Body", StringComparison.Ordinal) || name.Contains("Content", StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public async Task Index_ReturnsPublishedOnly()
    {
        _h.AddProfile(_h.Primary, "writer");
        await PublishWithBodyAsync("Published One");
        await _h.CreatePostAsync(title: "Still A Draft");
        var deleted = await _h.CreatePostAsync(title: "Deleted Later");
        await _h.PublishAsync(deleted.Id);
        await _h.DeletePostAsync(deleted.Id);

        var page = await new ListPublicBlogSummariesQueryHandler(_h.Context).Handle(new ListPublicBlogSummariesQuery(), CancellationToken.None);

        page.Items.Select(s => s.Title).Should().ContainSingle("Published One");
    }

    [Fact]
    public async Task AuthorList_UnknownHandle_ReturnsEmpty()
    {
        var page = await new ListAuthorBlogSummariesQueryHandler(_h.Context).Handle(new ListAuthorBlogSummariesQuery("ghost"), CancellationToken.None);
        page.Items.Should().BeEmpty();
        page.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task KeysetPaging_AcrossBoundary_ReturnsAllPostsExactlyOnce()
    {
        _h.AddProfile(_h.Primary, "writer");
        for (var i = 1; i <= 13; i++)
            await PublishWithBodyAsync($"Keyset Post {i:D2}");

        var handler = new ListPublicBlogSummariesQueryHandler(_h.Context);
        var first = await handler.Handle(new ListPublicBlogSummariesQuery(), CancellationToken.None);
        first.Items.Should().HaveCount(12);
        first.HasMore.Should().BeTrue();

        var last = first.Items[^1];
        var second = await handler.Handle(new ListPublicBlogSummariesQuery(last.PublishedAt, last.Id), CancellationToken.None);
        second.Items.Should().ContainSingle();
        second.Items[0].Title.Should().Be("Keyset Post 01");
        second.HasMore.Should().BeFalse();

        var all = first.Items.Concat(second.Items).Select(s => s.Title).ToList();
        all.Should().HaveCount(13);
        all.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AuthorList_FiltersToAuthorAndResolvesHandles()
    {
        _h.AddProfile(_h.Primary, "writer");
        _h.AddProfile(_h.Coauthor, "coauthor");
        await PublishWithBodyAsync("Mine");
        var other = await _h.CreatePostAsync(_h.Coauthor, title: "Theirs");
        await _h.Service.PublishAsync(_h.Coauthor, other.Id);

        var mine = await new ListAuthorBlogSummariesQueryHandler(_h.Context).Handle(new ListAuthorBlogSummariesQuery("writer"), CancellationToken.None);
        var theirs = await new ListAuthorBlogSummariesQueryHandler(_h.Context).Handle(new ListAuthorBlogSummariesQuery("coauthor"), CancellationToken.None);

        mine.Items.Should().ContainSingle().Which.Title.Should().Be("Mine");
        mine.Items[0].PrimaryAuthorHandle.Should().Be("writer");
        mine.Items[0].PrimaryAuthorDisplayName.Should().Be("writer");
        theirs.Items.Should().ContainSingle().Which.Title.Should().Be("Theirs");
    }

    [Fact]
    public async Task Summary_IncludesCommentCount_ReactionCount_CoAuthorHandles_Tags()
    {
        _h.AddProfile(_h.Primary, "writer");
        _h.AddProfile(_h.Coauthor, "coauthor");
        var post = await PublishWithBodyAsync("Rich Summary");
        post = await _h.Service.UpdateDraftAsync(_h.Primary, post.Id, new Commands.UpdateBlogPostDraftCommand(post.Id, post.Revision, Tags: ["Csharp", "Api"]));
        await _h.AddCoauthorAsync(post.Id, _h.Coauthor);
        await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "first!", null);
        _h.AddReaction(_h.Stranger, post.Id, ReactionType.Like);
        _h.AddReaction(_h.Coauthor, post.Id, ReactionType.Celebrate);

        var page = await new ListPublicBlogSummariesQueryHandler(_h.Context).Handle(new ListPublicBlogSummariesQuery(), CancellationToken.None);

        var summary = page.Items.Should().ContainSingle().Subject;
        summary.CommentCount.Should().Be(1);
        summary.ReactionCount.Should().Be(2);
        summary.CoAuthorHandles.Should().Equal(["coauthor"]);
        summary.Tags.Should().Equal(["csharp", "api"]);
        summary.ReadTimeMinutes.Should().BeGreaterThanOrEqualTo(1);
        summary.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Detail_DraftPost_ReturnsNull_LikeNonexistent()
    {
        _h.AddProfile(_h.Primary, "writer");
        var draft = await _h.CreatePostAsync(title: "Unpublished Secret");

        var draftResult = await new GetPublicBlogPostDetailQueryHandler(_h.Context).Handle(new GetPublicBlogPostDetailQuery("writer", draft.Slug), CancellationToken.None);
        var missingResult = await new GetPublicBlogPostDetailQueryHandler(_h.Context).Handle(new GetPublicBlogPostDetailQuery("writer", "never-existed"), CancellationToken.None);

        draftResult.Should().BeNull();
        missingResult.Should().BeNull();
    }

    [Fact]
    public async Task Detail_DeletedPost_ReturnsNull()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await PublishWithBodyAsync("Gone Soon");
        await _h.DeletePostAsync(post.Id);

        var result = await new GetPublicBlogPostDetailQueryHandler(_h.Context).Handle(new GetPublicBlogPostDetailQuery("writer", post.Slug), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Detail_PublishedPost_CarriesBodyFormatSeoRevision()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await PublishWithBodyAsync("Full Detail");
        post = await _h.Service.UpdateDraftAsync(_h.Primary, post.Id, new Commands.UpdateBlogPostDraftCommand(
            post.Id, post.Revision,
            Content: "full body with words",
            Excerpt: "short summary",
            MetaTitle: "SEO Title",
            MetaDescription: "SEO description",
            OgImageUrl: "https://cdn.example/og.png",
            CanonicalUrlOverride: "https://example.com/canonical",
            TwitterCard: "summary",
            StructuredDataOverride: "{\"@type\":\"BlogPosting\"}"));

        var result = await new GetPublicBlogPostDetailQueryHandler(_h.Context).Handle(new GetPublicBlogPostDetailQuery("writer", post.Slug), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Content.Should().Be("full body with words");
        result.Format.Should().Be(BlogContentFormat.Markdown);
        result.JsonBody.Should().BeNull();
        result.Excerpt.Should().Be("short summary");
        result.MetaTitle.Should().Be("SEO Title");
        result.MetaDescription.Should().Be("SEO description");
        result.OgImageUrl.Should().Be("https://cdn.example/og.png");
        result.CanonicalUrlOverride.Should().Be("https://example.com/canonical");
        result.TwitterCard.Should().Be("summary");
        result.StructuredDataOverride.Should().NotBeNull();
        result.Revision.Should().Be(post.Revision);
        result.UpdatedAt.Should().Be(post.UpdatedAt);
        result.PrimaryAuthorHandle.Should().Be("writer");
    }

    [Fact]
    public async Task Detail_LexicalFormat_CarriesJsonBody()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await _h.CreatePostAsync(title: "Rich Post", format: BlogContentFormat.Lexical);
        post = await _h.Service.UpdateDraftAsync(_h.Primary, post.Id, new Commands.UpdateBlogPostDraftCommand(post.Id, post.Revision, JsonBody: "{\"root\":{}}"));
        await _h.PublishAsync(post.Id);

        var result = await new GetPublicBlogPostDetailQueryHandler(_h.Context).Handle(new GetPublicBlogPostDetailQuery("writer", post.Slug), CancellationToken.None);

        result!.Format.Should().Be(BlogContentFormat.Lexical);
        result.JsonBody.Should().Be("{\"root\":{}}");
    }

    [Fact]
    public async Task Comments_OldestFirst_FlattensRepliesUnderParents_ExcludesSoftDeleted()
    {
        _h.AddProfile(_h.Primary, "writer");
        _h.AddProfile(_h.Stranger, "commenter");
        var post = await PublishWithBodyAsync("Commented");
        var first = await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "oldest", null);
        var second = await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "newer root", null);
        var reply = await _h.Service.AddCommentAsync(_h.Primary, post.Id, "reply to first", first.Id);
        var doomed = await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "deleted soon", null);
        await _h.Service.DeleteCommentAsync(_h.Stranger, doomed.Id);

        var page = await new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation).Handle(new ListBlogCommentsQuery(post.Id), CancellationToken.None);

        page.Items.Select(c => c.Content).Should().Equal(["oldest", "reply to first", "newer root"]);
        page.Items[0].AuthorHandle.Should().Be("commenter");
        page.Items[1].ParentCommentId.Should().Be(first.Id);
        page.Items[1].AuthorHandle.Should().Be("writer");
        page.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Comments_UnpublishedPost_ReturnsEmpty()
    {
        var post = await _h.CreatePostAsync(title: "Draft With Comments");

        var page = await new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation).Handle(new ListBlogCommentsQuery(post.Id), CancellationToken.None);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Comments_MutedAuthorHiddenForViewer_VisibleForAnonymous()
    {
        _h.AddProfile(_h.Primary, "writer");
        _h.AddProfile(_h.Stranger, "commenter");
        var post = await PublishWithBodyAsync("Muted Experiment");
        var plain = await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "normal comment", null);
        var noisy = Guid.NewGuid();
        _h.AddProfile(noisy, "noisy");
        await _h.Service.AddCommentAsync(noisy, post.Id, "annoying comment", null);
        _h.Moderation.MutedPairs.Add((_h.Primary, noisy));

        var forViewer = await new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation).Handle(new ListBlogCommentsQuery(post.Id, ViewerId: _h.Primary), CancellationToken.None);
        var anonymous = await new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation).Handle(new ListBlogCommentsQuery(post.Id), CancellationToken.None);

        forViewer.Items.Select(c => c.Content).Should().Equal(["normal comment"]);
        anonymous.Items.Select(c => c.Content).Should().Equal(["normal comment", "annoying comment"]);
        plain.Content.Should().Be("normal comment");
    }

    [Fact]
    public async Task Comments_ReplyToMutedParent_HiddenToo()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await PublishWithBodyAsync("Muted Parent");
        var noisy = Guid.NewGuid();
        var root = await _h.Service.AddCommentAsync(noisy, post.Id, "muted root", null);
        await _h.Service.AddCommentAsync(_h.Stranger, post.Id, "reply to muted", root.Id);
        _h.Moderation.MutedPairs.Add((_h.Primary, noisy));

        var page = await new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation).Handle(new ListBlogCommentsQuery(post.Id, ViewerId: _h.Primary), CancellationToken.None);

        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Comments_KeysetPaging_PageSize20()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await PublishWithBodyAsync("Many Comments");
        for (var i = 1; i <= 21; i++)
            await _h.Service.AddCommentAsync(_h.Stranger, post.Id, $"comment {i:D2}", null);

        var handler = new ListBlogCommentsQueryHandler(_h.Context, _h.Moderation);
        var first = await handler.Handle(new ListBlogCommentsQuery(post.Id), CancellationToken.None);
        first.Items.Should().HaveCount(20);
        first.HasMore.Should().BeTrue();

        var last = first.Items[^1];
        var second = await handler.Handle(new ListBlogCommentsQuery(post.Id, AfterCreatedAt: last.CreatedAt, AfterId: last.Id), CancellationToken.None);
        second.Items.Should().ContainSingle().Which.Content.Should().Be("comment 21");
        second.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ViewCounter_IncrementsPublished_WithoutUpdatedAtChange()
    {
        _h.AddProfile(_h.Primary, "writer");
        var post = await PublishWithBodyAsync("Watched");
        var updatedAtBefore = _h.Context.Set<BlogPost>().Single(p => p.Id == post.Id).UpdatedAt;

        var incremented = await _h.ViewCounter.IncrementAsync(post.Id);
        var stored = _h.Context.Set<BlogPost>().Single(p => p.Id == post.Id);

        incremented.Should().BeTrue();
        stored.ViewsCount.Should().Be(1);
        stored.UpdatedAt.Should().Be(updatedAtBefore);

        await _h.ViewCounter.IncrementAsync(post.Id);
        _h.Context.Set<BlogPost>().Single(p => p.Id == post.Id).ViewsCount.Should().Be(2);
    }

    [Fact]
    public async Task ViewCounter_DraftOrUnknown_ReturnsFalse_NoIncrement()
    {
        var draft = await _h.CreatePostAsync(title: "Unwatched Draft");

        (await _h.ViewCounter.IncrementAsync(draft.Id)).Should().BeFalse();
        (await _h.ViewCounter.IncrementAsync(Guid.NewGuid())).Should().BeFalse();
        _h.Context.Set<BlogPost>().Single(p => p.Id == draft.Id).ViewsCount.Should().Be(0);
    }
}
