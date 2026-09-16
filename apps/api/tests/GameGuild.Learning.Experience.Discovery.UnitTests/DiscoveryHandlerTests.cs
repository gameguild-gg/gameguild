using FluentAssertions;
using GameGuild.Learning.Experience.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Learning.Experience.Discovery.UnitTests;

public sealed class DiscoveryCommandHandlerTests
{
    [Fact]
    public async Task FeaturedContentCommands_PersistUpdatesToggleAndDelete()
    {
        await using var db = CreateContext();
        var handler = new DiscoveryCommandHandlers(db, NullLogger<DiscoveryCommandHandlers>.Instance);
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var startsAt = DateTime.UtcNow.AddHours(-1);
        var endsAt = DateTime.UtcNow.AddHours(1);

        var created = await handler.Handle(
            new CreateFeaturedContentCommand(
                FeaturedContentType.HeroBanner,
                "Launch",
                2,
                CourseId: courseId,
                TenantId: tenantId,
                Subtitle: "Subtitle",
                ImageUrl: "https://example.com/image.png",
                LinkUrl: "https://example.com",
                StartsAt: startsAt,
                EndsAt: endsAt,
                TargetAudience: "{\"level\":\"beginner\"}"),
            default);

        created.Subtitle.Should().Be("Subtitle");
        created.ImageUrl.Should().Be("https://example.com/image.png");
        created.LinkUrl.Should().Be("https://example.com");
        created.StartsAt.Should().Be(startsAt);
        created.EndsAt.Should().Be(endsAt);
        created.TargetAudience.Should().Contain("beginner");

        var updated = await handler.Handle(
            new UpdateFeaturedContentCommand(
                created.Id,
                "Updated",
                "New subtitle",
                "new-image",
                "new-link",
                7,
                startsAt.AddMinutes(1),
                endsAt.AddMinutes(1),
                false,
                "all"),
            default);
        updated.Should().BeSameAs(created);
        created.Title.Should().Be("Updated");
        created.DisplayOrder.Should().Be(7);
        created.IsActive.Should().BeFalse();

        (await handler.Handle(new ToggleFeaturedContentCommand(created.Id, true), default))
            .Should().BeSameAs(created);
        created.IsActive.Should().BeTrue();
        created.Version = 1;
        await db.SaveChangesAsync();
        (await handler.Handle(new DeleteFeaturedContentCommand(created.Id), default)).Should().BeTrue();
        created.DeletedAt.Should().NotBeNull();

        (await handler.Handle(new UpdateFeaturedContentCommand(Guid.NewGuid()), default)).Should().BeNull();
        (await handler.Handle(new ToggleFeaturedContentCommand(Guid.NewGuid(), true), default)).Should().BeNull();
        (await handler.Handle(new DeleteFeaturedContentCommand(Guid.NewGuid()), default)).Should().BeFalse();
    }

    [Fact]
    public async Task CourseCollectionCommands_PersistDetailsLifecycleAndUniqueSlug()
    {
        await using var db = CreateContext();
        var handler = new DiscoveryCommandHandlers(db, NullLogger<DiscoveryCommandHandlers>.Instance);
        var curatorId = Guid.NewGuid();

        var first = await handler.Handle(
            new CreateCourseCollectionCommand(
                curatorId,
                "C# Essentials!",
                CollectionType.Skill,
                Description: "Core skills",
                ImageUrl: "image"),
            default);
        var duplicate = await handler.Handle(
            new CreateCourseCollectionCommand(curatorId, "C# Essentials!"),
            default);

        first.Slug.Should().Be("c#-essentials");
        duplicate.Slug.Should().StartWith("c#-essentials-").And.NotBe(first.Slug);
        first.Description.Should().Be("Core skills");
        first.ImageUrl.Should().Be("image");

        var updated = await handler.Handle(
            new UpdateCourseCollectionCommand(first.Id, "Updated", "Description", "new-image", true),
            default);
        updated.Should().BeSameAs(first);
        first.Title.Should().Be("Updated");
        first.IsFeatured.Should().BeTrue();

        (await handler.Handle(new PublishCourseCollectionCommand(first.Id), default)).Should().BeSameAs(first);
        first.IsPublished.Should().BeTrue();
        (await handler.Handle(new UnpublishCourseCollectionCommand(first.Id), default)).Should().BeSameAs(first);
        first.IsPublished.Should().BeFalse();
        first.Version = 1;
        await db.SaveChangesAsync();
        (await handler.Handle(new DeleteCourseCollectionCommand(first.Id), default)).Should().BeTrue();

        (await handler.Handle(new UpdateCourseCollectionCommand(Guid.NewGuid()), default)).Should().BeNull();
        (await handler.Handle(new PublishCourseCollectionCommand(Guid.NewGuid()), default)).Should().BeNull();
        (await handler.Handle(new UnpublishCourseCollectionCommand(Guid.NewGuid()), default)).Should().BeNull();
        (await handler.Handle(new DeleteCourseCollectionCommand(Guid.NewGuid()), default)).Should().BeFalse();
    }

    [Fact]
    public async Task SearchCommands_PersistSearchAndClick()
    {
        await using var db = CreateContext();
        var handler = new DiscoveryCommandHandlers(db, NullLogger<DiscoveryCommandHandlers>.Instance);
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var search = await handler.Handle(
            new RecordSearchCommand("unity", 5, userId, "{\"level\":1}"), default);
        (await handler.Handle(new RecordSearchClickCommand(search.Id, courseId), default)).Should().BeTrue();

        search.ClickedCourseId.Should().Be(courseId);
        search.ClickedPosition.Should().Be(0);
        (await handler.Handle(new RecordSearchClickCommand(Guid.NewGuid(), courseId), default)).Should().BeFalse();
    }

    private static DiscoveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<DiscoveryDbContext>()
            .UseInMemoryDatabase($"DiscoveryCommands_{Guid.NewGuid()}")
            .Options);

    private sealed class DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new DiscoveryModelConfiguration().Configure(modelBuilder);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

public sealed class DiscoveryQueryHandlerTests
{
    [Fact]
    public async Task FeaturedContentQueries_ApplyLifecycleTypeTenantAndAdminFilters()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var global = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Global", 3);
        var tenant = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Tenant", 1, tenantId: tenantId);
        var other = FeaturedContent.Create(FeaturedContentType.NewRelease, "Other", 2, tenantId: otherTenantId);
        var inactive = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Inactive", 4, tenantId: tenantId);
        inactive.SetActive(false);
        var future = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Future", 5, tenantId: tenantId);
        future.Update(startsAt: DateTime.UtcNow.AddDays(1));
        var ended = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Ended", 6, tenantId: tenantId);
        ended.Update(endsAt: DateTime.UtcNow.AddDays(-1));
        db.AddRange(global, tenant, other, inactive, future, ended);
        await db.SaveChangesAsync();
        var handler = new DiscoveryQueryHandlers(db, NullLogger<DiscoveryQueryHandlers>.Instance);

        (await handler.Handle(new GetActiveFeaturedContentQuery(tenantId), default))
            .Select(item => item.Title).Should().Equal("Tenant", "Global");
        (await handler.Handle(new GetActiveFeaturedContentQuery(), default)).Should().Contain(other);
        (await handler.Handle(new GetFeaturedContentByTypeQuery(FeaturedContentType.HeroBanner, tenantId), default))
            .Should().Contain([tenant, global]).And.NotContain(other);
        (await handler.Handle(new GetFeaturedContentByTypeQuery(FeaturedContentType.NewRelease), default))
            .Should().Contain(other);
        (await handler.Handle(new GetFeaturedContentByIdQuery(global.Id), default)).Should().BeSameAs(global);
        (await handler.Handle(new GetFeaturedContentByIdQuery(Guid.NewGuid()), default)).Should().BeNull();
        (await handler.Handle(new GetAllFeaturedContentQuery(tenantId, IncludeInactive: false), default))
            .Should().Contain(tenant).And.NotContain(inactive);
        (await handler.Handle(new GetAllFeaturedContentQuery(null, IncludeInactive: true), default))
            .Should().Contain(inactive).And.Contain(other);
    }

    [Fact]
    public async Task CollectionQueries_ApplyPublicationFeatureCuratorTypeAndTenantFilters()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var curatorId = Guid.NewGuid();
        var global = CourseCollection.Create(curatorId, "Global", "global", CollectionType.Curated);
        global.Publish();
        global.Update(isFeatured: true);
        var tenant = CourseCollection.Create(curatorId, "Tenant", "tenant", CollectionType.Skill, tenantId);
        tenant.Publish();
        var draft = CourseCollection.Create(curatorId, "Draft", "draft", CollectionType.Skill, tenantId);
        var other = CourseCollection.Create(Guid.NewGuid(), "Other", "other", CollectionType.Career, otherTenant);
        other.Publish();
        db.AddRange(global, tenant, draft, other);
        await db.SaveChangesAsync();
        var handler = new DiscoveryQueryHandlers(db, NullLogger<DiscoveryQueryHandlers>.Instance);

        (await handler.Handle(new GetPublishedCollectionsQuery(tenantId, CollectionType.Skill), default))
            .Should().ContainSingle().Which.Should().BeSameAs(tenant);
        (await handler.Handle(new GetPublishedCollectionsQuery(), default)).Should().Contain([global, tenant, other]);
        (await handler.Handle(new GetCollectionBySlugQuery("global", tenantId), default)).Should().BeSameAs(global);
        (await handler.Handle(new GetCollectionBySlugQuery("other"), default)).Should().BeSameAs(other);
        (await handler.Handle(new GetCollectionByIdQuery(tenant.Id), default)).Should().BeSameAs(tenant);
        (await handler.Handle(new GetFeaturedCollectionsQuery(tenantId), default))
            .Should().ContainSingle().Which.Should().BeSameAs(global);
        (await handler.Handle(new GetFeaturedCollectionsQuery(), default)).Should().Contain(global);
        (await handler.Handle(new GetCollectionsByCuratorQuery(curatorId, IncludeUnpublished: false), default))
            .Should().Contain([global, tenant]).And.NotContain(draft);
        (await handler.Handle(new GetCollectionsByCuratorQuery(curatorId, IncludeUnpublished: true), default))
            .Should().Contain(draft);
        (await handler.Handle(new GetAllCollectionsQuery(tenantId, IncludeUnpublished: false), default))
            .Should().Contain(tenant).And.NotContain(draft);
        (await handler.Handle(new GetAllCollectionsQuery(null, IncludeUnpublished: true), default))
            .Should().Contain([global, tenant, draft, other]);
    }

    [Fact]
    public async Task SearchQueries_ReturnHistoryAndAggregateClicks()
    {
        await using var db = CreateContext();
        var userId = Guid.NewGuid();
        var first = SearchHistory.Create("Unity", 5, userId);
        first.RecordClick(Guid.NewGuid(), 1);
        var second = SearchHistory.Create("unity", 2, userId);
        var other = SearchHistory.Create("godot", 3, Guid.NewGuid());
        db.AddRange(first, second, other);
        await db.SaveChangesAsync();
        var handler = new DiscoveryQueryHandlers(db, NullLogger<DiscoveryQueryHandlers>.Instance);

        (await handler.Handle(new GetUserSearchHistoryQuery(userId, 1), default)).Should().HaveCount(1);
        var popular = (await handler.Handle(new GetPopularSearchesQuery(30, 10), default)).ToList();
        var unity = popular.Single(item => item.Query == "unity");
        unity.SearchCount.Should().Be(2);
        unity.TotalClicks.Should().Be(1);
        unity.ClickThroughRate.Should().Be(0.5);
    }

    private static DiscoveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<DiscoveryDbContext>()
            .UseInMemoryDatabase($"DiscoveryQueries_{Guid.NewGuid()}")
            .Options);

    private sealed class DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new DiscoveryModelConfiguration().Configure(modelBuilder);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
