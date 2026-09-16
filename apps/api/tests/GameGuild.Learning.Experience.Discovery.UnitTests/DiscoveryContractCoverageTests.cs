using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Experience.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Discovery.UnitTests;

public sealed class DiscoveryContractCoverageTests
{
    [Fact]
    public void DtosAndQueries_PreserveEveryPublicValue()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var curatorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        new CreateFeaturedContentDto(FeaturedContentType.StaffPick, "Title", 2, id, null, "Sub", "image", "link", now, now.AddDays(1), "audience")
            .TargetAudience.Should().Be("audience");
        new UpdateFeaturedContentDto("Title", "Sub", "image", "link", 3, now, now.AddDays(1), false, "audience")
            .IsActive.Should().BeFalse();
        new CreateCourseCollectionDto("Collection", CollectionType.Career, "Description", "image")
            .Type.Should().Be(CollectionType.Career);
        new UpdateCourseCollectionDto("Collection", "Description", "image", true)
            .IsFeatured.Should().BeTrue();
        new RecordSearchDto("query", 5, "filters").Filters.Should().Be("filters");
        new RecordSearchClickDto(id, curatorId).ClickedCourseId.Should().Be(curatorId);
        new RecordSearchClickCommand(id, curatorId).SearchId.Should().Be(id);

        new GetActiveFeaturedContentQuery(tenantId, 1, 2).Take.Should().Be(2);
        new GetFeaturedContentByTypeQuery(FeaturedContentType.StaffPick, tenantId, 1, 2).Type.Should().Be(FeaturedContentType.StaffPick);
        new GetAllFeaturedContentQuery(tenantId, true, 1, 2).IncludeInactive.Should().BeTrue();
        new GetPublishedCollectionsQuery(tenantId, CollectionType.Career, 1, 2).Type.Should().Be(CollectionType.Career);
        new GetFeaturedCollectionsQuery(tenantId, 2).Take.Should().Be(2);
        new GetCollectionsByCuratorQuery(curatorId, true, 1, 2).IncludeUnpublished.Should().BeTrue();
        new GetAllCollectionsQuery(tenantId, false, 1, 2).IncludeUnpublished.Should().BeFalse();
        new GetUserSearchHistoryQuery(id, 2).UserId.Should().Be(id);
        new GetPopularSearchesQuery(7, 2).DaysBack.Should().Be(7);
        new PopularSearchResult("query", 2, 1, 0.5).ClickThroughRate.Should().Be(0.5);
    }

    [Fact]
    public void ModuleControllerServiceAndModelConfiguration_ConstructSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddDiscoveryModule().Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDiscoveryService));

        new DiscoveryController(Mock.Of<IDiscoveryService>(), Mock.Of<ISender>()).Should().NotBeNull();
        new DiscoveryService(Mock.Of<IMediator>()).Should().NotBeNull();

        var modelBuilder = new ModelBuilder();
        new DiscoveryModelConfiguration().Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();
        model.FindEntityType(typeof(FeaturedContent))!.GetTableName().Should().Be("learning_featured_content");
        model.FindEntityType(typeof(CourseCollection))!.GetTableName().Should().Be("learning_course_collections");
        model.FindEntityType(typeof(SearchHistory))!.GetTableName().Should().Be("learning_search_history");
    }

    [Fact]
    public void FeaturedContent_ActivityCoversInactiveFutureEndedAndActiveStates()
    {
        var inactive = FeaturedContent.Create(FeaturedContentType.NewRelease, "Inactive", 1);
        inactive.SetActive(false);
        var future = FeaturedContent.Create(FeaturedContentType.NewRelease, "Future", 1);
        future.Update(startsAt: DateTime.UtcNow.AddDays(1));
        var ended = FeaturedContent.Create(FeaturedContentType.NewRelease, "Ended", 1);
        ended.Update(endsAt: DateTime.UtcNow.AddDays(-1));
        var active = FeaturedContent.Create(FeaturedContentType.NewRelease, "Active", 1);

        inactive.IsCurrentlyActive().Should().BeFalse();
        future.IsCurrentlyActive().Should().BeFalse();
        ended.IsCurrentlyActive().Should().BeFalse();
        active.IsCurrentlyActive().Should().BeTrue();
    }
}
