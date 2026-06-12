using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Learning.Experience.Discovery.UnitTests;

public sealed class DiscoveryInfrastructureCoverageTests
{
    [Fact]
    public void DiscoveryModelConfiguration_AppliesDiscoveryMappings()
    {
        using var context = CreateContext();
        var featuredEntity = context.Model.FindEntityType(typeof(FeaturedContent));
        var collectionEntity = context.Model.FindEntityType(typeof(CourseCollection));
        var searchEntity = context.Model.FindEntityType(typeof(SearchHistory));

        featuredEntity.Should().NotBeNull();
        collectionEntity.Should().NotBeNull();
        searchEntity.Should().NotBeNull();
        var featured = featuredEntity!;
        var collection = collectionEntity!;
        var search = searchEntity!;

        featured.GetTableName().Should().Be("learning_featured_content");
        featured.FindProperty(nameof(FeaturedContent.Title))!.GetMaxLength().Should().Be(300);
        featured.FindProperty(nameof(FeaturedContent.Title))!.IsNullable.Should().BeFalse();
        featured.FindProperty(nameof(FeaturedContent.Subtitle))!.GetMaxLength().Should().Be(500);
        featured.FindProperty(nameof(FeaturedContent.ImageUrl))!.GetMaxLength().Should().Be(1000);
        featured.FindProperty(nameof(FeaturedContent.LinkUrl))!.GetMaxLength().Should().Be(1000);
        featured.FindProperty(nameof(FeaturedContent.TargetAudience))!.GetMaxLength().Should().Be(4000);
        featured.FindProperty(nameof(FeaturedContent.Type))!.GetMaxLength().Should().Be(60);

        collection.GetTableName().Should().Be("learning_course_collections");
        collection.FindProperty(nameof(CourseCollection.Title))!.GetMaxLength().Should().Be(300);
        collection.FindProperty(nameof(CourseCollection.Title))!.IsNullable.Should().BeFalse();
        collection.FindProperty(nameof(CourseCollection.Slug))!.GetMaxLength().Should().Be(220);
        collection.FindProperty(nameof(CourseCollection.Slug))!.IsNullable.Should().BeFalse();
        collection.FindProperty(nameof(CourseCollection.Description))!.GetMaxLength().Should().Be(2000);
        collection.FindProperty(nameof(CourseCollection.ImageUrl))!.GetMaxLength().Should().Be(1000);
        collection.FindProperty(nameof(CourseCollection.Type))!.GetMaxLength().Should().Be(60);

        search.GetTableName().Should().Be("learning_search_history");
        search.FindProperty(nameof(SearchHistory.Query))!.GetMaxLength().Should().Be(500);
        search.FindProperty(nameof(SearchHistory.Query))!.IsNullable.Should().BeFalse();
        search.FindProperty(nameof(SearchHistory.Filters))!.GetMaxLength().Should().Be(4000);
    }

    [Fact]
    public void DiscoveryModule_RegistersDiscoveryService()
    {
        var services = new ServiceCollection();

        var configured = services.AddDiscoveryModule();

        configured.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDiscoveryService) && descriptor.ImplementationType == typeof(DiscoveryService));
    }

    private static DiscoveryConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DiscoveryConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DiscoveryConfigurationDbContext(options);
    }

    private sealed class DiscoveryConfigurationDbContext(DbContextOptions<DiscoveryConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new DiscoveryModelConfiguration().Configure(modelBuilder);
        }
    }
}
