using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabTenantIsolationTests
{
    [Fact]
    public async Task RequestQueries_ShouldReturnOnlyCurrentTenantRows()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var actorRequest = NewRequest(actorTenantId);
        actorRequest.Title = "Shared searchable request";
        actorRequest.Status = TestingRequestStatus.Open;
        actorRequest.CreatedById = creatorId;
        var otherRequest = NewRequest(otherTenantId);
        otherRequest.Title = "Shared searchable request";
        otherRequest.Status = TestingRequestStatus.Open;
        otherRequest.CreatedById = creatorId;
        context.Set<User>().Add(new User
        {
            Id = creatorId,
            Email = "tenant-isolation@example.com",
            Name = "Tenant isolation actor",
            IsActive = true
        });
        context.Set<TestingRequest>().AddRange(actorRequest, otherRequest);
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, CreateActor(actorTenantId));

        AssertOnlyRequest(await service.GetAllTestingRequestsAsync(), actorRequest.Id);
        AssertOnlyRequest(await service.GetTestingRequestsAsync(), actorRequest.Id);
        (await service.GetTestingRequestByIdAsync(otherRequest.Id)).Should().BeNull();
        (await service.GetTestingRequestByIdWithDetailsAsync(otherRequest.Id)).Should().BeNull();
        AssertOnlyRequest(await service.GetTestingRequestsByCreatorAsync(creatorId), actorRequest.Id);
        AssertOnlyRequest(await service.GetTestingRequestsByStatusAsync(TestingRequestStatus.Open), actorRequest.Id);
        AssertOnlyRequest(await service.SearchTestingRequestsAsync("searchable"), actorRequest.Id);
        AssertOnlyRequest(await service.GetActiveTestingRequestsAsync(), actorRequest.Id);
    }

    [Fact]
    public async Task RequestDirectory_Should_Include_Archived_Rows_Only_When_Requested()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var activeRequest = NewRequest(actorTenantId);
        var archivedRequest = NewRequest(actorTenantId);
        archivedRequest.Version = 1;
        archivedRequest.SoftDelete();
        var otherTenantArchivedRequest = NewRequest(Guid.NewGuid());
        otherTenantArchivedRequest.Version = 1;
        otherTenantArchivedRequest.SoftDelete();
        context.Set<User>().AddRange(
            NewUser(activeRequest.CreatedById, "active"),
            NewUser(archivedRequest.CreatedById, "archived"),
            NewUser(otherTenantArchivedRequest.CreatedById, "other-archived"));
        context.Set<TestingRequest>().AddRange(activeRequest, archivedRequest, otherTenantArchivedRequest);
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, CreateActor(actorTenantId));

        AssertOnlyRequest(await service.GetTestingRequestsAsync(), activeRequest.Id);
        (await service.GetTestingRequestsAsync(includeArchived: true))
            .Select(request => request.Id)
            .Should().BeEquivalentTo([activeRequest.Id, archivedRequest.Id]);
    }

    [Fact]
    public async Task RequestMutations_ShouldNotChangeAnotherTenantRow()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var otherRequest = NewRequest(otherTenantId);
        otherRequest.Title = "Other tenant request";
        context.Set<TestingRequest>().Add(otherRequest);
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, CreateActor(actorTenantId));
        var attemptedUpdate = NewRequest(otherTenantId);
        attemptedUpdate.Id = otherRequest.Id;
        attemptedUpdate.Title = "Cross-tenant update";

        var update = () => service.UpdateTestingRequestAsync(attemptedUpdate);

        await update.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{otherRequest.Id}*not found*");
        (await service.DeleteTestingRequestAsync(otherRequest.Id)).Should().BeFalse();
        otherRequest.Title.Should().Be("Other tenant request");
        otherRequest.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task FeedbackQueriesAndModeration_ShouldStayInsideCurrentTenant()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentFeedback = NewFeedback(actorTenantId, userId);
        var foreignFeedback = NewFeedback(Guid.NewGuid(), userId);
        context.Set<User>().Add(NewUser(userId, "feedback-user"));
        context.Set<TestingFeedback>().AddRange(currentFeedback, foreignFeedback);
        await context.SaveChangesAsync();
        var service = new TestingFeedbackOperationsService(context, CreateActor(actorTenantId));

        var results = await service.GetFeedbackByUserAsync(userId);
        var reportForeign = () => service.ReportFeedbackAsync(
            foreignFeedback.Id,
            "Cross-tenant report",
            userId);

        results.Should().ContainSingle().Which.Id.Should().Be(currentFeedback.Id);
        await reportForeign.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
        foreignFeedback.IsReported.Should().BeFalse();
    }

    [Fact]
    public async Task RequestFeedback_ShouldRemainReadableAfterRequestIsArchived_WithinCurrentTenant()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var archivedRequest = NewRequest(tenantId);
        archivedRequest.Version = 1;
        archivedRequest.SoftDelete();
        var foreignArchivedRequest = NewRequest(Guid.NewGuid());
        foreignArchivedRequest.Version = 1;
        foreignArchivedRequest.SoftDelete();
        var requestFeedback = NewFeedback(tenantId, userId);
        requestFeedback.TestingRequestId = archivedRequest.Id;
        context.Set<User>().Add(NewUser(userId, "archived-feedback-user"));
        context.Set<TestingRequest>().AddRange(archivedRequest, foreignArchivedRequest);
        context.Set<TestingFeedback>().Add(requestFeedback);
        await context.SaveChangesAsync();
        var service = new TestingFeedbackOperationsService(context, CreateActor(tenantId));

        var feedback = await service.GetTestingRequestFeedbackAsync(archivedRequest.Id);
        var statistics = await service.GetTestingRequestStatisticsAsync(archivedRequest.Id);
        var readForeign = () => service.GetTestingRequestFeedbackAsync(foreignArchivedRequest.Id);
        var submitToArchived = () => service.AddFeedbackAsync(
            archivedRequest.Id,
            userId,
            Guid.NewGuid(),
            "Archived request feedback",
            TestingContext.Online);

        feedback.Should().ContainSingle().Which.Id.Should().Be(requestFeedback.Id);
        statistics.Should().NotBeNull();
        await readForeign.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
        await submitToArchived.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task FeedbackDirectory_ShouldUnifyRequestAndEventFeedback_WithTenantScopedFilters()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = NewUser(userId, "directory-user");
        var request = NewRequest(tenantId);
        request.Title = "Request source";
        var testingEvent = TestingEvent.Create(
            "Event source",
            TestingEventMode.Online,
            userId,
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            SystemClock.UtcNow.AddDays(4),
            true,
            TestingEventApprovalMode.ManagerOnly,
            tenantId);
        var requestFeedback = NewFeedback(tenantId, userId);
        requestFeedback.TestingRequestId = request.Id;
        requestFeedback.OverallRating = 8;
        var eventFeedback = NewFeedback(tenantId, userId);
        eventFeedback.EventId = testingEvent.Id;
        eventFeedback.IsReported = true;
        var foreignFeedback = NewFeedback(Guid.NewGuid(), userId);
        foreignFeedback.EventId = testingEvent.Id;
        context.Set<User>().Add(user);
        context.Set<TestingRequest>().Add(request);
        context.Set<TestingEvent>().Add(testingEvent);
        context.Set<TestingFeedback>().AddRange(requestFeedback, eventFeedback, foreignFeedback);
        await context.SaveChangesAsync();
        var service = new TestingFeedbackOperationsService(context, CreateActor(tenantId));

        var all = await service.GetFeedbackDirectoryAsync(new TestingFeedbackDirectoryQuery(Take: 20));
        var events = await service.GetFeedbackDirectoryAsync(new TestingFeedbackDirectoryQuery(
            Source: TestingFeedbackSource.Event,
            Reported: true,
            Take: 20));

        all.TotalCount.Should().Be(2);
        all.Items.Select(item => item.Source).Should().BeEquivalentTo([
            TestingFeedbackSource.Request,
            TestingFeedbackSource.Event]);
        events.Items.Should().ContainSingle(item =>
            item.Id == eventFeedback.Id &&
            item.EventName == "Event source" &&
            item.IsReported);

        var serializedItem = JsonSerializer.Serialize(
            events.Items.Single(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new JsonStringEnumConverter() }
            });
        using var document = JsonDocument.Parse(serializedItem);
        document.RootElement.TryGetProperty("qualityRating", out _).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreRequest_ShouldNotRestoreAnotherTenantRow()
    {
        await using var context = CreateContext();
        var actorTenantId = Guid.NewGuid();
        var otherRequest = NewRequest(Guid.NewGuid());
        otherRequest.Version = 1;
        otherRequest.SoftDelete();
        context.Set<TestingRequest>().Add(otherRequest);
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, CreateActor(actorTenantId));

        (await service.RestoreTestingRequestAsync(otherRequest.Id)).Should().BeFalse();
        otherRequest.DeletedAt.Should().NotBeNull();
    }

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

    private static TestingRequestOperationsService CreateRequestService(
        IApplicationDbContext context,
        IActorContextAccessor actor)
        => new(
            context,
            Mock.Of<IProjectChannelAvailabilityService>(),
            Mock.Of<IProjectAuthorizationService>(),
            actor);

    private static void AssertOnlyRequest(IEnumerable<TestingRequest> requests, Guid expectedRequestId)
        => requests.Should().ContainSingle().Which.Id.Should().Be(expectedRequestId);

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

    private static User NewUser(Guid userId, string emailPrefix)
        => new()
        {
            Id = userId,
            Email = $"{emailPrefix}@example.com",
            Name = emailPrefix,
            IsActive = true
        };

    private static TestingFeedback NewFeedback(Guid tenantId, Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            FeedbackData = "Tenant-scoped feedback",
            TestingContext = TestingContext.Online
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

        public DbSet<TestingFeedback> TestingFeedback => Set<TestingFeedback>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not required for tenant-isolation tests.");
    }
}
