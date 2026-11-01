using FluentAssertions;
using GameGuild.Social.Blog.Services;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

/// <summary>
/// Ghost export → internal blog migration (issue #96 "port all old content"): parser shape,
/// HTML→Markdown conversion, author mapping, idempotency, original-date preservation, dry run.
/// </summary>
public class GhostImportServiceTests
{
    private readonly BlogServiceTestHarness _h = new();

    private GhostImportService CreateService() => new(_h.Context, new BlogSlugService(_h.Context));

    private static string Export(params string[] postsJson)
        => $$"""
        {
          "db": [{
            "meta": { "version": "5.0" },
            "data": {
              "posts": [{{string.Join(",", postsJson)}}],
              "tags": [
                { "id": "tag-1", "name": "Announcements", "slug": "announcements" },
                { "id": "tag-2", "name": "Deep Dive", "slug": "deep-dive" }
              ],
              "posts_tags": [
                { "id": "pt-1", "post_id": "post-1", "tag_id": "tag-1" },
                { "id": "pt-2", "post_id": "post-1", "tag_id": "tag-2" }
              ],
              "users": [
                { "id": "user-1", "slug": "ghost-author", "name": "Ghost Author", "email": "author@ghost.local" }
              ],
              "comments": [
                { "id": "c-1", "post_id": "post-1", "status": "published", "created_at": "2024-02-01T10:00:00.000Z" }
              ]
            }
          }]
        }
        """;

    private static string Post(
        string id = "post-1",
        string title = "Hello Ghost",
        string slug = "hello-ghost",
        string? html = "<p>First <strong>bold</strong> paragraph.</p>",
        string? publishedAt = "2024-02-01T09:00:00.000Z",
        string type = "post",
        string? authorId = "user-1",
        string status = "published")
        => $$"""
        {
          "id": "{{id}}",
          "title": "{{title}}",
          "slug": "{{slug}}",
          "html": {{JsonString(html)}},
          "plaintext": "First bold paragraph.",
          "type": "{{type}}",
          "status": "{{status}}",
          "published_at": {{JsonString(publishedAt)}},
          "created_at": "2024-01-15T08:00:00.000Z",
          "custom_excerpt": "Migrated excerpt",
          "meta_title": "Meta title",
          "meta_description": "Meta description",
          "og_image": "https://ghost.local/og.png",
          "canonical_url": "https://blog.gameguild.gg/hello-ghost/",
          "author_id": "{{authorId}}"
        }
        """;

    private static string JsonString(string? value)
        => value is null ? "null" : $"\"{value}\"";

    [Fact]
    public async Task Import_MapsPostToInternalBlog_WithSeoFieldsAndTags()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        var report = await CreateService().ImportAsync(
            Export(Post()),
            new GhostImportOptions(DryRun: false, PublishImported: true, FallbackAuthorId: null));

        report.Imported.Should().Be(1);
        report.SkippedDuplicate.Should().Be(0);
        report.CommentsInExport.Should().Be(1);

        var post = _h.Context.Set<BlogPost>().Single(p => p.Slug == "hello-ghost");
        post.PrimaryAuthorId.Should().Be(_h.Primary);
        post.Title.Should().Be("Hello Ghost");
        post.Format.Should().Be(BlogContentFormat.Markdown);
        post.Content.Should().Be("First **bold** paragraph.");
        post.Excerpt.Should().Be("Migrated excerpt");
        post.MetaTitle.Should().Be("Meta title");
        post.MetaDescription.Should().Be("Meta description");
        post.OgImageUrl.Should().Be("https://ghost.local/og.png");
        post.CanonicalUrlOverride.Should().Be("https://blog.gameguild.gg/hello-ghost/");
        post.Tags.Should().BeEquivalentTo(["announcements", "deep dive"]);
        post.Status.Should().Be(BlogPostStatus.Published);
    }

    [Fact]
    public async Task Import_PreservesOriginalPublishDate()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        await CreateService().ImportAsync(
            Export(Post(publishedAt: "2023-06-15T12:00:00.000Z")),
            new GhostImportOptions(DryRun: false, PublishImported: true));

        var post = _h.Context.Set<BlogPost>().Single(p => p.Slug == "hello-ghost");
        post.PublishedAt.Should().Be(new DateTime(2023, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        post.CreatedAt.Should().Be(new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Import_GhostDraft_StaysDraft_EvenWithPublishImported()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        await CreateService().ImportAsync(
            Export(Post(publishedAt: null, status: "draft")),
            new GhostImportOptions(DryRun: false, PublishImported: true));

        var post = _h.Context.Set<BlogPost>().Single(p => p.Slug == "hello-ghost");
        post.Status.Should().Be(BlogPostStatus.Draft);
        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task Import_SecondRun_SkipsDuplicates()
    {
        _h.AddProfile(_h.Primary, "ghost-author");
        var service = CreateService();
        var export = Export(Post());

        var first = await service.ImportAsync(export, new GhostImportOptions(DryRun: false));
        var second = await service.ImportAsync(export, new GhostImportOptions(DryRun: false));

        first.Imported.Should().Be(1);
        second.Imported.Should().Be(0);
        second.SkippedDuplicate.Should().Be(1);
        _h.Context.Set<BlogPost>().Count(p => p.Slug == "hello-ghost").Should().Be(1);
    }

    [Fact]
    public async Task Import_UnmappedAuthor_FallsBackToProvidedAuthor()
    {
        var report = await CreateService().ImportAsync(
            Export(Post()),
            new GhostImportOptions(DryRun: false, FallbackAuthorId: _h.Primary));

        report.Imported.Should().Be(1);
        _h.Context.Set<BlogPost>().Single(p => p.Slug == "hello-ghost")
            .PrimaryAuthorId.Should().Be(_h.Primary);
        report.Outcomes.Single(o => o.Result == GhostImportPostResult.Imported)
            .Note.Should().Contain("fallback");
    }

    [Fact]
    public async Task Import_UnmappedAuthor_WithoutFallback_Skips()
    {
        var report = await CreateService().ImportAsync(
            Export(Post()),
            new GhostImportOptions(DryRun: false, FallbackAuthorId: null));

        report.Imported.Should().Be(0);
        report.SkippedNoAuthor.Should().Be(1);
        _h.Context.Set<BlogPost>().Should().BeEmpty();
    }

    [Fact]
    public async Task Import_DryRun_PersistsNothing()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        var report = await CreateService().ImportAsync(
            Export(Post()),
            new GhostImportOptions(DryRun: true, PublishImported: true));

        report.DryRun.Should().BeTrue();
        report.Imported.Should().Be(1);
        _h.Context.Set<BlogPost>().Should().BeEmpty();
    }

    [Fact]
    public async Task Import_SkipsGhostPages()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        var report = await CreateService().ImportAsync(
            Export(Post(id: "page-1", slug: "about", type: "page")),
            new GhostImportOptions(DryRun: false));

        report.SkippedNonPost.Should().Be(1);
        report.Imported.Should().Be(0);
    }

    [Fact]
    public async Task Import_FallsBackToPlaintext_WhenHtmlMissing()
    {
        _h.AddProfile(_h.Primary, "ghost-author");

        await CreateService().ImportAsync(
            Export(Post(html: null)),
            new GhostImportOptions(DryRun: false));

        _h.Context.Set<BlogPost>().Single(p => p.Slug == "hello-ghost")
            .Content.Should().Be("First bold paragraph.");
    }

    [Fact]
    public void Parser_AcceptsBareDataEnvelope()
    {
        var export = GhostExportParser.Parse("""{ "posts": [ { "id": "p1", "title": "T", "slug": "t" } ] }""");

        export.Posts.Should().HaveCount(1);
        export.Posts[0].Title.Should().Be("T");
    }

    [Fact]
    public void Parser_PromotesFirstPostsAuthorsRow_ForLegacyMultiAuthorPosts()
    {
        var export = GhostExportParser.Parse("""
        {
          "db": [{
            "data": {
              "posts": [ { "id": "p1", "title": "T", "slug": "t", "type": "post" } ],
              "users": [ { "id": "u1", "slug": "author", "name": "A" } ],
              "posts_authors": [ { "id": "pa1", "post_id": "p1", "author_id": "u1" } ]
            }
          }]
        }
        """);

        export.Posts.Single().AuthorId.Should().Be("u1");
    }
}

/// <summary>HTML→Markdown conversion for Ghost bodies (issue #96 content migration).</summary>
public class GhostHtmlToMarkdownTests
{
    [Theory]
    [InlineData("<h1>Title</h1>", "# Title")]
    [InlineData("<h3>Sub</h3>", "### Sub")]
    [InlineData("<p>Plain <strong>bold</strong> and <em>italic</em>.</p>", "Plain **bold** and *italic*.")]
    [InlineData("<p>A <a href=\"https://x.y/z\">link</a> and <code>inline()</code> code.</p>", "A [link](https://x.y/z) and `inline()` code.")]
    [InlineData("<p>Line one<br>Line two</p>", "Line one\nLine two")]
    [InlineData("<hr>", "---")]
    [InlineData("<p>A &amp; B &lt;tags&gt; &#8212; ok</p>", "A & B <tags> — ok")]
    public void Convert_CoreInlineAndHeadings(string html, string expected)
        => GhostHtmlToMarkdown.Convert(html).Should().Be(expected);

    [Fact]
    public void Convert_NestedLists()
    {
        var html = "<ul><li>one</li><li>two<ol><li>two-a</li><li>two-b</li></ol></li><li>three</li></ul>";

        GhostHtmlToMarkdown.Convert(html).Should().Be(
            """
            - one
            - two
              1. two-a
              2. two-b
            - three
            """);
    }

    [Fact]
    public void Convert_BlockquoteWithParagraph()
    {
        var html = "<blockquote><p>Quoted line.</p><p>Second line.</p></blockquote>";

        GhostHtmlToMarkdown.Convert(html).Should().Be(
            """
            > Quoted line.
            > Second line.
            """);
    }

    [Fact]
    public void Convert_FencedCodeBlockWithLanguage()
    {
        var html = "<pre><code class=\"language-js\">var x = 1 &amp; 2;</code></pre>";

        GhostHtmlToMarkdown.Convert(html).Should().Be(
            """
            ```js
            var x = 1 & 2;
            ```
            """);
    }

    [Fact]
    public void Convert_ImageAndFigure()
    {
        GhostHtmlToMarkdown.Convert("<figure class=\"kg-image-card\"><img src=\"https://img/x.png\" alt=\"A picture\"><figcaption>Caption</figcaption></figure>")
            .Should().Be("![A picture](https://img/x.png)\n\n*Caption*");
    }

    [Fact]
    public void Convert_Table()
    {
        var html = "<table><thead><tr><th>Col A</th><th>Col B</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>";

        GhostHtmlToMarkdown.Convert(html).Should().Be(
            """
            | Col A | Col B |
            | --- | --- |
            | 1 | 2 |
            """);
    }

    [Fact]
    public void Convert_StripsKgCardComments()
        => GhostHtmlToMarkdown.Convert("<!--kg-card-begin: markdown--><p>Kept</p><!--kg-card-end: markdown-->")
            .Should().Be("Kept");
}
