using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public class BlogPostTests
{
    [Fact]
    public void Create_SetsDefaults()
    {
        var authorId = Guid.NewGuid();
        var post = BlogPost.Create(authorId, "Test Title", "test-title", "Hello world content here.");

        post.AuthorId.Should().Be(authorId);
        post.Title.Should().Be("Test Title");
        post.Slug.Should().Be("test-title");
        post.Content.Should().Be("Hello world content here.");
        post.Status.Should().Be(BlogPostStatus.Draft);
        post.AllowComments.Should().BeTrue();
        post.ViewsCount.Should().Be(0);
        post.LikesCount.Should().Be(0);
        post.CommentsCount.Should().Be(0);
        post.IsFeatured.Should().BeFalse();
        post.PublishedAt.Should().BeNull();
        post.ReadTimeMinutes.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Create_WithTenantId()
    {
        var tenantId = Guid.NewGuid();
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "Content", tenantId);
        post.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_CalculatesReadTime()
    {
        // 200 words = 1 minute, 400 words = 2 minutes
        var longContent = string.Join(" ", Enumerable.Repeat("word", 400));
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", longContent);
        post.ReadTimeMinutes.Should().Be(2);
    }

    [Fact]
    public void Publish_SetsStatusAndDate()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.Publish();
        post.Status.Should().Be(BlogPostStatus.Published);
        post.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Unpublish_SetsBackToDraft()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.Publish();
        post.Unpublish();
        post.Status.Should().Be(BlogPostStatus.Draft);
    }

    [Fact]
    public void Feature_Unfeature()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.Feature();
        post.IsFeatured.Should().BeTrue();
        post.Unfeature();
        post.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void IncrementViews()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.IncrementViews();
        post.IncrementViews();
        post.ViewsCount.Should().Be(2);
    }

    [Fact]
    public void IncrementDecrementLikes()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.IncrementLikes();
        post.IncrementLikes();
        post.LikesCount.Should().Be(2);

        post.DecrementLikes();
        post.LikesCount.Should().Be(1);

        // Decrement below zero guard
        post.DecrementLikes();
        post.DecrementLikes();
        post.LikesCount.Should().Be(0);
    }

    [Fact]
    public void IncrementDecrementComments()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "T", "t", "C");
        post.IncrementComments();
        post.CommentsCount.Should().Be(1);
        post.DecrementComments();
        post.CommentsCount.Should().Be(0);
        post.DecrementComments(); // below 0 guard
        post.CommentsCount.Should().Be(0);
    }
}

public class BlogPostStatusTests
{
    [Fact]
    public void AllValues()
    {
        Enum.GetValues<BlogPostStatus>().Should().HaveCount(3);
    }
}
