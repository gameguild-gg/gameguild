using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Experience.Discovery;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Discovery.UnitTests;

public class DiscoveryContractCoverageTests
{
    [Fact]
    public void FeaturedContent_IsCurrentlyActive_ShouldCoverInactiveAndDateWindowBranches()
    {
        var inactive = FeaturedContent.Create(FeaturedContentType.NewRelease, "Inactive", 1);
        SetPrivate(inactive, nameof(FeaturedContent.IsActive), false);

        var future = FeaturedContent.Create(FeaturedContentType.NewRelease, "Future", 1);
        SetPrivate(future, nameof(FeaturedContent.StartsAt), SystemClock.UtcNow.AddDays(1));

        var expired = FeaturedContent.Create(FeaturedContentType.NewRelease, "Expired", 1);
        SetPrivate(expired, nameof(FeaturedContent.EndsAt), SystemClock.UtcNow.AddDays(-1));

        var activeWindow = FeaturedContent.Create(FeaturedContentType.NewRelease, "Active", 1);
        SetPrivate(activeWindow, nameof(FeaturedContent.StartsAt), SystemClock.UtcNow.AddDays(-1));
        SetPrivate(activeWindow, nameof(FeaturedContent.EndsAt), SystemClock.UtcNow.AddDays(1));

        inactive.IsCurrentlyActive().Should().BeFalse();
        future.IsCurrentlyActive().Should().BeFalse();
        expired.IsCurrentlyActive().Should().BeFalse();
        activeWindow.IsCurrentlyActive().Should().BeTrue();
    }

    [Fact]
    public void RequestDtos_ShouldExposeAllValues()
    {
        var courseId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var start = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(10);
        var createFeatured = new CreateFeaturedContentDto(
            FeaturedContentType.HeroBanner,
            "Hero",
            1,
            courseId,
            pathId,
            "Subtitle",
            "image.png",
            "https://example.test",
            start,
            end,
            "{\"role\":\"student\"}");
        var updateFeatured = new UpdateFeaturedContentDto("Updated", "Sub", "new.png", "https://new.test", 2, start, end, false, "audience");
        var createCollection = new CreateCourseCollectionDto("Collection", CollectionType.Category, "Description", "image.png");
        var updateCollection = new UpdateCourseCollectionDto("Updated collection", "Updated description", "new.png", true);
        var recordSearch = new RecordSearchDto("unity", 10, "{\"level\":\"beginner\"}");
        var recordClick = new RecordSearchClickDto(Guid.NewGuid(), courseId);

        createFeatured.LearningPathId.Should().Be(pathId);
        updateFeatured.IsActive.Should().BeFalse();
        createCollection.Type.Should().Be(CollectionType.Category);
        updateCollection.IsFeatured.Should().BeTrue();
        recordSearch.Filters.Should().Contain("beginner");
        recordClick.ClickedCourseId.Should().Be(courseId);
    }

    [Fact]
    public void RemainingCommandsAndQueries_ShouldExposeAllValues()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var curatorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        new RecordSearchClickCommand(id, courseId).ClickedCourseId.Should().Be(courseId);
        new GetActiveFeaturedContentQuery(tenantId, 1, 2).TenantId.Should().Be(tenantId);
        new GetFeaturedContentByTypeQuery(FeaturedContentType.SeasonalPromotion, tenantId, 3, 4).Type.Should().Be(FeaturedContentType.SeasonalPromotion);
        new GetAllFeaturedContentQuery(tenantId, true, 5, 6).IncludeInactive.Should().BeTrue();
        new GetPublishedCollectionsQuery(tenantId, CollectionType.Skill, 7, 8).Type.Should().Be(CollectionType.Skill);
        new GetFeaturedCollectionsQuery(tenantId, 9).Take.Should().Be(9);
        new GetCollectionsByCuratorQuery(curatorId, true, 10, 11).IncludeUnpublished.Should().BeTrue();
        new GetAllCollectionsQuery(tenantId, false, 12, 13).IncludeUnpublished.Should().BeFalse();
        new GetUserSearchHistoryQuery(curatorId, 14).Take.Should().Be(14);
        new GetPopularSearchesQuery(15, 16).DaysBack.Should().Be(15);

        var popular = new PopularSearchResult("unity", 20, 5, 25);
        popular.ClickThroughRate.Should().Be(25);
    }

    [Fact]
    public void ConstructorsAndPrivateHelpers_ShouldBeCovered()
    {
        var context = new Mock<IApplicationDbContext>().Object;

        new DiscoveryController(new Mock<IDiscoveryService>().Object).Should().NotBeNull();
        new DiscoveryService(new Mock<IMediator>().Object).Should().BeAssignableTo<IDiscoveryService>();
        new DiscoveryCommandHandlers(context, NullLogger<DiscoveryCommandHandlers>.Instance).Should().NotBeNull();
        new DiscoveryQueryHandlers(context, NullLogger<DiscoveryQueryHandlers>.Instance).Should().NotBeNull();

        var method = typeof(DiscoveryCommandHandlers)
            .GetMethod("GenerateSlug", BindingFlags.NonPublic | BindingFlags.Static);
        var slug = method!.Invoke(null, new object[] { "Featured, Topic! Isn't Hard?" });

        slug.Should().Be("featured-topic-isnt-hard");
    }

    private static void SetPrivate<T>(FeaturedContent content, string propertyName, T value)
    {
        typeof(FeaturedContent)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(content, value);
    }
}
