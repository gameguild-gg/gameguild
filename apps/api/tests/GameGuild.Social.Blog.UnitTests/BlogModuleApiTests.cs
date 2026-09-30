using System.Reflection;
using FluentAssertions;
using GameGuild.Social.Blog.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
namespace GameGuild.Social.Blog.UnitTests;

/// <summary>
/// Model snapshot tests mirroring the EfConfigAndServicesTests pattern: build the module
/// model via <see cref="BlogModelConfiguration"/> and assert the rebuilt schema.
/// </summary>
public class BlogModelConfigurationTests
{
    [Fact]
    public void BlogPost_MapsTableColumnsAndIndexes()
    {
        var entity = BuildModel().FindEntityType(typeof(BlogPost))!;

        entity.GetTableName().Should().Be("social_blog_posts");
        entity.FindPrimaryKey()!.Properties.Single().Name.Should().Be(nameof(BlogPost.Id));

        entity.FindProperty(nameof(BlogPost.Title))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(BlogPost.Slug))!.GetMaxLength().Should().Be(220);
        entity.FindProperty(nameof(BlogPost.Excerpt))!.GetMaxLength().Should().Be(500);
        entity.FindProperty(nameof(BlogPost.MetaTitle))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(BlogPost.MetaDescription))!.GetMaxLength().Should().Be(300);
        entity.FindProperty(nameof(BlogPost.OgImageUrl))!.GetMaxLength().Should().Be(1000);
        entity.FindProperty(nameof(BlogPost.CanonicalUrlOverride))!.GetMaxLength().Should().Be(1000);
        entity.FindProperty(nameof(BlogPost.Status))!.GetMaxLength().Should().Be(40);
        entity.FindProperty(nameof(BlogPost.Format))!.GetMaxLength().Should().Be(40);

        entity.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogPost.PrimaryAuthorId), nameof(BlogPost.Slug) }));
        entity.GetIndexes().Should().Contain(index =>
            !index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogPost.PrimaryAuthorId), nameof(BlogPost.Status), nameof(BlogPost.PublishedAt) }));
        entity.GetIndexes().Should().Contain(index =>
            !index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogPost.Status), nameof(BlogPost.PublishedAt) }));
    }

    [Fact]
    public void BlogPost_HasNoGlobalUniqueSlugIndex()
    {
        var entity = BuildModel().FindEntityType(typeof(BlogPost))!;

        // Precise form: no unique index over exactly [Slug].
        entity.GetIndexes().Should().NotContain(index =>
            index.IsUnique
            && index.Properties.Count == 1
            && index.Properties[0].Name == nameof(BlogPost.Slug));
    }

    [Fact]
    public void BlogPost_DropsLegacyColumns_AndKeepsCommentsCount()
    {
        var entity = BuildModel().FindEntityType(typeof(BlogPost))!;

        entity.GetProperties().Select(property => property.Name).Should().NotContain("IsFeatured");
        entity.GetProperties().Select(property => property.Name).Should().NotContain("LikesCount");
        entity.GetProperties().Select(property => property.Name).Should().NotContain("Archived");
        entity.GetProperties().Select(property => property.Name).Should().Contain(nameof(BlogPost.CommentsCount));
        entity.GetProperties().Select(property => property.Name).Should().Contain(nameof(BlogPost.Revision));
        entity.FindProperty(nameof(BlogPost.TagsJson))!.GetColumnName(StoreObjectIdentifier.Table("social_blog_posts")).Should().Be("Tags");
    }
    [Fact]
    public void AllNewTables_AreConfigured()
    {
        var model = BuildModel();

        var expected = new[]
        {
            "social_blog_posts",
            "social_blog_post_authors",
            "social_blog_slug_history",
            "social_blog_comments",
            "social_blog_ai_conversations",
            "social_blog_ai_runs",
            "social_blog_ai_messages",
            "social_blog_ai_proposals",
            "social_blog_ai_stream_events",
        };

        var tables = model.GetEntityTypes().Select(entityType => entityType.GetTableName()).ToHashSet();
        expected.Should().BeSubsetOf(tables);
    }

    [Fact]
    public void BlogPostAuthor_AndSlugHistory_HaveUniqueIndexes()
    {
        var model = BuildModel();

        var authors = model.FindEntityType(typeof(BlogPostAuthor))!;
        authors.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogPostAuthor.BlogPostId), nameof(BlogPostAuthor.UserId) }));
        authors.GetIndexes().Should().Contain(index =>
            index.Properties.Single().Name == nameof(BlogPostAuthor.UserId));

        var history = model.FindEntityType(typeof(BlogSlugHistory))!;
        history.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogSlugHistory.PreviousPrimaryAuthorId), nameof(BlogSlugHistory.PreviousSlug) }));
        history.GetIndexes().Should().Contain(index =>
            index.Properties.Single().Name == nameof(BlogSlugHistory.BlogPostId));
    }

    [Fact]
    public void BlogComment_HasPostCreatedIndex()
    {
        var comments = BuildModel().FindEntityType(typeof(BlogComment))!;
        comments.GetTableName().Should().Be("social_blog_comments");
        comments.GetIndexes().Should().Contain(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(BlogComment.BlogPostId), nameof(BlogComment.CreatedAt) }));
    }

    [Fact]
    public void SocialBlogModule_RegistersAndMapsNothing()
    {
        var module = new SocialBlogModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var endpoints = new Mock<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>().Object;

        var configuredServices = module.ConfigureServices(services, configuration);
        var mappedEndpoints = module.MapEndpoints(endpoints);

        module.Name.Should().Be("Social.Blog");
        module.Order.Should().Be(163);
        configuredServices.Should().BeSameAs(services);
        mappedEndpoints.Should().BeSameAs(endpoints);
    }

    private static readonly IModel Model = CreateModel();

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<BlogModelTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new BlogModelTestDbContext(options);
        return context.Model;
    }

    private sealed class BlogModelTestDbContext(DbContextOptions<BlogModelTestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new BlogModelConfiguration().Configure(modelBuilder);
        }
    }

    private static IModel BuildModel() => Model;
}

/// <summary>
/// Asserts the RebuildSocialBlog migration contents. The migration lives in the
/// GameGuild.API assembly, so its source is located on disk from the test output directory.
/// </summary>
public class RebuildSocialBlogMigrationTests
{
    private static readonly Lazy<string> MigrationSource = new(ReadMigrationSource);

    private static string ReadMigrationSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("GameGuild.sln").Length == 0)
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the GameGuild.sln root must be reachable from the test output directory");
        var migration = directory!
            .GetFiles("*.cs", SearchOption.AllDirectories)
            .Single(file => file.Name.EndsWith("RebuildSocialBlog.cs", StringComparison.Ordinal)
                            && file.FullName.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"));
        return File.ReadAllText(migration.FullName);
    }

    [Fact]
    public void Migration_DropsLegacyBlogTable()
        => MigrationSource.Value.Should().Contain("DropTable").And.Contain("social_blog_posts");

    [Fact]
    public void Migration_PurgesBlogPostReactions()
        => MigrationSource.Value.Should()
            .Contain("DELETE FROM \\\"social_reactions\\\" WHERE \\\"TargetType\\\" = 'BlogPost'");
}
