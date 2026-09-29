using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public class BlogPostTests
{
    [Fact]
    public void Create_SetsDefaults()
    {
        var authorId = Guid.NewGuid();
        var post = BlogPost.Create(authorId, "Test Title", "test-title", BlogContentFormat.Markdown);

        post.PrimaryAuthorId.Should().Be(authorId);
        post.Title.Should().Be("Test Title");
        post.Slug.Should().Be("test-title");
        post.Format.Should().Be(BlogContentFormat.Markdown);
        post.Status.Should().Be(BlogPostStatus.Draft);
        post.AllowComments.Should().BeTrue();
        post.ViewsCount.Should().Be(0);
        post.CommentsCount.Should().Be(0);
        post.PublishedAt.Should().BeNull();
        post.ReadTimeMinutes.Should().BeGreaterOrEqualTo(1);
        post.Revision.Should().Be(1);
        post.JsonBody.Should().BeNull();
    }

    [Fact]
    public void Create_WithTenantId()
    {
        var tenantId = Guid.NewGuid();
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Lexical, tenantId);
        post.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Publish_ThenUnpublish_RoundTripsStatus()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);
        var before = post.UpdatedAt;

        post.Publish(DateTimeOffset.UtcNow.AddSeconds(1));

        post.Status.Should().Be(BlogPostStatus.Published);
        post.PublishedAt.Should().NotBeNull();

        post.Unpublish(DateTimeOffset.UtcNow.AddSeconds(2));

        post.Status.Should().Be(BlogPostStatus.Draft);
        post.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void ApplyDraftEdit_BumpsRevisionAndAppliesFields()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);
        var before = post.UpdatedAt;

        post.ApplyDraftEdit(
            expectedRevision: 1,
            title: "New title",
            content: "New body",
            jsonBody: null,
            excerpt: "New excerpt",
            tags: ["Alpha", "beta", "ALPHA"],
            metaTitle: "Meta",
            metaDescription: "Desc",
            ogImageUrl: "https://example.test/og.png",
            canonicalUrlOverride: "https://example.test/canonical",
            twitterCard: "summary",
            structuredDataOverride: null,
            allowComments: false,
            readTimeMinutes: 3,
            now: DateTimeOffset.UtcNow.AddSeconds(1));

        post.Revision.Should().Be(2);
        post.Title.Should().Be("New title");
        post.Content.Should().Be("New body");
        post.Excerpt.Should().Be("New excerpt");
        post.Tags.Should().BeEquivalentTo(["alpha", "beta"]);
        post.MetaTitle.Should().Be("Meta");
        post.AllowComments.Should().BeFalse();
        post.ReadTimeMinutes.Should().Be(3);
        post.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void ApplyDraftEdit_StaleRevision_Throws()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);

        var act = () => post.ApplyDraftEdit(
            expectedRevision: 7,
            title: "X",
            content: null,
            jsonBody: null,
            excerpt: null,
            tags: null,
            metaTitle: null,
            metaDescription: null,
            ogImageUrl: null,
            canonicalUrlOverride: null,
            twitterCard: null,
            structuredDataOverride: null,
            allowComments: null,
            readTimeMinutes: null);

        act.Should().Throw<BlogRevisionConflictException>()
            .Which.ExpectedRevision.Should().Be(7);
    }

    [Fact]
    public void ApplyDraftEdit_MarkdownPost_RejectsJsonBody()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);

        var act = () => post.ApplyDraftEdit(
            expectedRevision: 1,
            title: null,
            content: null,
            jsonBody: """{"root":{}}""",
            excerpt: null,
            tags: null,
            metaTitle: null,
            metaDescription: null,
            ogImageUrl: null,
            canonicalUrlOverride: null,
            twitterCard: null,
            structuredDataOverride: null,
            allowComments: null,
            readTimeMinutes: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangeSlug_BumpsRevision()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);
        post.ChangeSlug("new-slug");
        post.Slug.Should().Be("new-slug");
        post.Revision.Should().Be(2);
    }

    [Fact]
    public void TransferPrimary_BumpsRevisionAndSetsAuthor()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown);
        var coAuthor = Guid.NewGuid();
        post.TransferPrimary(coAuthor);
        post.PrimaryAuthorId.Should().Be(coAuthor);
        post.Revision.Should().Be(2);
    }

    [Fact]
    public void IncrementViews_AndCommentCounters_DoNotBumpUpdatedAt()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", BlogContentFormat.Markdown, now: DateTimeOffset.UtcNow);
        var updatedAt = post.UpdatedAt;

        post.IncrementViews();
        post.IncrementViews();
        post.IncrementComments();
        post.DecrementComments();
        post.DecrementComments();

        post.ViewsCount.Should().Be(2);
        post.CommentsCount.Should().Be(0);
        post.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void NormalizeTags_DedupesLowercasesAndCaps()
    {
        var tags = BlogPost.NormalizeTags(["Alpha", " BETA ", "alpha", "gamma"]);
        tags.Should().BeEquivalentTo(["alpha", "beta", "gamma"], options => options.WithStrictOrdering());
    }

    [Fact]
    public void BlogPostStatus_HasExactlyDraftAndPublished()
    {
        Enum.GetValues<BlogPostStatus>().Should().BeEquivalentTo([BlogPostStatus.Draft, BlogPostStatus.Published]);
    }
}
