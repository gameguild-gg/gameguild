using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabTenantIsolationTests
{
    [Fact]
    public async Task SessionsQuery_ShouldReturnOnlyCurrentTenantRows()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var actor = CreateActor(actorTenantId);
        var actorRequest = NewRequest(actorTenantId);
        var otherRequest = NewRequest(otherTenantId);
        var actorLocation = NewLocation(actorTenantId, "Current tenant location");
        var otherLocation = NewLocation(otherTenantId, "Other tenant location");
        context.Set<TestingRequest>().AddRange(actorRequest, otherRequest);
        context.Set<TestingLocation>().AddRange(actorLocation, otherLocation);
        context.Set<TestingSession>().AddRange(
            NewSession(actorTenantId, "Current tenant session", actorRequest.Id, actorLocation.Id),
            NewSession(otherTenantId, "Other tenant session", otherRequest.Id, otherLocation.Id));
        await context.SaveChangesAsync();

        var service = CreateService<TestingSessionOperationsService>(context, actor);

        var sessions = await service.GetTestingSessionsAsync();

        sessions.Should().ContainSingle(session => session.TenantId == actorTenantId);
        sessions.Should().NotContain(session => session.TenantId == otherTenantId);
    }

    [Fact]
    public async Task LocationsQuery_ShouldReturnOnlyCurrentTenantRows()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var actor = CreateActor(actorTenantId);
        context.Set<TestingLocation>().AddRange(
            NewLocation(actorTenantId, "Current tenant location"),
            NewLocation(otherTenantId, "Other tenant location"));
        await context.SaveChangesAsync();

        var service = CreateService<TestingLocationOperationsService>(context, actor);

        var locations = await service.GetTestingLocationsAsync();

        locations.Should().ContainSingle(location => location.TenantId == actorTenantId);
        locations.Should().NotContain(location => location.TenantId == otherTenantId);
    }

    [Fact]
    public async Task LocationsQuery_ShouldRequireAuthentication_WhenActorIsAnonymous()
    {
        await using var context = CreateContext();
        var anonymousActor = new ActorContextAccessor();
        anonymousActor.ClearActorContext();
        var service = CreateService<TestingLocationOperationsService>(context, anonymousActor);

        var act = () => service.GetTestingLocationsAsync();

        await act.Should().ThrowAsync<AuthenticationRequiredException>();
    }

    [Fact]
    public async Task LocationsQuery_ShouldForbidAuthenticatedActorWithoutTenant()
    {
        await using var context = CreateContext();
        var actor = new ActorContextAccessor();
        actor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).Build());
        var service = CreateService<TestingLocationOperationsService>(context, actor);

        var act = () => service.GetTestingLocationsAsync();

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task SessionsQuery_ShouldForbidAuthenticatedActorWithoutTenant()
    {
        await using var context = CreateContext();
        var actor = new ActorContextAccessor();
        actor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).Build());
        var service = CreateService<TestingSessionOperationsService>(context, actor);

        var act = () => service.GetTestingSessionsAsync();

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task CreateLocation_ShouldAssignCurrentTenant()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var service = CreateService<TestingLocationOperationsService>(context, CreateActor(actorTenantId));

        var location = await service.CreateTestingLocationAsync(new TestingLocation
        {
            Name = "Tenant lab",
            MaxTestersCapacity = 12,
            MaxProjectsCapacity = 3
        });

        location.TenantId.Should().Be(actorTenantId);
    }

    [Fact]
    public async Task CreateSession_ShouldAssignCurrentTenant_WhenRelationsBelongToTenant()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var request = NewRequest(actorTenantId);
        var location = NewLocation(actorTenantId, "Tenant session room");
        context.Set<TestingRequest>().Add(request);
        context.Set<TestingLocation>().Add(location);
        await context.SaveChangesAsync();
        var service = CreateService<TestingSessionOperationsService>(context, CreateActor(actorTenantId));

        var session = await service.CreateTestingSessionAsync(NewSession(null, "Tenant session", request.Id, location.Id));

        session.TenantId.Should().Be(actorTenantId);
    }

    [Fact]
    public async Task CreateSession_ShouldRejectCrossTenantRelations()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var request = NewRequest(actorTenantId);
        var otherLocation = NewLocation(otherTenantId, "Other tenant room");
        context.Set<TestingRequest>().Add(request);
        context.Set<TestingLocation>().Add(otherLocation);
        await context.SaveChangesAsync();
        var service = CreateService<TestingSessionOperationsService>(context, CreateActor(actorTenantId));

        var act = () => service.CreateTestingSessionAsync(NewSession(null, "Invalid session", request.Id, otherLocation.Id));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        context.Set<TestingSession>().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteLocation_ShouldNotDeleteAnotherTenantLocation()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherLocation = NewLocation(Guid.NewGuid(), "Other tenant location");
        otherLocation.Version = 1;
        context.Set<TestingLocation>().Add(otherLocation);
        await context.SaveChangesAsync();
        var service = CreateService<TestingLocationOperationsService>(context, CreateActor(actorTenantId));

        var deleted = await service.DeleteTestingLocationAsync(otherLocation.Id);

        deleted.Should().BeFalse();
        var persisted = await context.Set<TestingLocation>()
            .IgnoreQueryFilters()
            .SingleAsync(location => location.Id == otherLocation.Id);
        persisted.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task LocationsQuery_ShouldIncludeArchivedRowsOnlyWhenRequested()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var active = NewLocation(actorTenantId, "Active location");
        var archived = NewLocation(actorTenantId, "Archived location");
        archived.Version = 1;
        archived.SoftDelete();
        var otherArchived = NewLocation(otherTenantId, "Other archived location");
        otherArchived.Version = 1;
        otherArchived.SoftDelete();
        context.Set<TestingLocation>().AddRange(active, archived, otherArchived);
        await context.SaveChangesAsync();
        var service = CreateService<TestingLocationOperationsService>(context, CreateActor(actorTenantId));

        var activeOnly = await service.GetTestingLocationsAsync();
        var includingArchived = await service.GetTestingLocationsAsync(includeArchived: true);

        activeOnly.Should().ContainSingle(location => location.Id == active.Id);
        includingArchived.Should().HaveCount(2);
        includingArchived.Should().Contain(location => location.Id == archived.Id);
        includingArchived.Should().NotContain(location => location.TenantId == otherTenantId);
    }

    [Fact]
    public async Task DeleteLocation_ShouldRejectLocationWithUpcomingSession()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var request = NewRequest(tenantId);
        var location = NewLocation(tenantId, "Scheduled room");
        context.Set<TestingRequest>().Add(request);
        context.Set<TestingLocation>().Add(location);
        context.Set<TestingSession>().Add(NewSession(tenantId, "Upcoming session", request.Id, location.Id));
        await context.SaveChangesAsync();
        var service = CreateService<TestingLocationOperationsService>(context, CreateActor(tenantId));

        var action = () => service.DeleteTestingLocationAsync(location.Id);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*upcoming sessions*");
        location.DeletedAt.Should().BeNull();
    }

    private static TService CreateService<TService>(IApplicationDbContext context, IActorContextAccessor actor)
        where TService : notnull
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(actor);
        using var provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<TService>(provider);
    }

    private static IActorContextAccessor CreateActor(Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build());
        return accessor;
    }

    private static TestingSession NewSession(
        Guid? tenantId,
        string name,
        Guid? testingRequestId = null,
        Guid? locationId = null)
        => new()
        {
            TestingRequestId = testingRequestId ?? Guid.NewGuid(),
            LocationId = locationId ?? Guid.NewGuid(),
            SessionName = name,
            SessionDate = SystemClock.UtcNow.AddDays(1),
            StartTime = SystemClock.UtcNow.AddDays(1),
            EndTime = SystemClock.UtcNow.AddDays(1).AddHours(1),
            MaxTesters = 10,
            ManagerId = Guid.NewGuid(),
            ManagerUserId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid(),
            TenantId = tenantId
        };

    private static TestingLocation NewLocation(Guid tenantId, string name)
        => new()
        {
            Name = name,
            MaxTestersCapacity = 20,
            MaxProjectsCapacity = 4,
            TenantId = tenantId
        };

    private static TestingRequest NewRequest(Guid tenantId)
        => new()
        {
            Title = "Tenant request",
            StartDate = SystemClock.UtcNow,
            EndDate = SystemClock.UtcNow.AddDays(7),
            MaxTesters = 10,
            TenantId = tenantId,
            CreatedById = Guid.NewGuid()
        };

    private static TenantIsolationDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TenantIsolationDbContext>()
            .UseInMemoryDatabase($"testing-lab-tenant-isolation-{Guid.NewGuid():N}")
            .Options);

    private sealed class TenantIsolationDbContext(DbContextOptions<TenantIsolationDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();

        public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();

        public DbSet<TestingLocation> TestingLocations => Set<TestingLocation>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not required for tenant-isolation tests.");
    }
}
