using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabLegacyInfrastructureCoverageTests
{
    [Fact]
    public async Task CreateSessionHandler_ValidatesRequestAndLocationThenPublishesCreation()
    {
        var requestId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var requests = new Mock<ITestingRequestService>();
        var locations = new Mock<ITestingLocationRepository>();
        var sessions = new Mock<ITestingSessionService>();
        var mediator = new Mock<IMediator>();
        var command = Command(requestId, locationId);
        var handler = new CreateTestingSessionCommandHandler(
            sessions.Object, requests.Object, locations.Object, mediator.Object);

        await InvokingAsync(() => handler.Handle(command, default)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Testing request*");

        requests.Setup(value => value.GetByIdAsync(requestId)).ReturnsAsync(new TestingRequest { Id = requestId });
        await InvokingAsync(() => handler.Handle(command, default)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Location*");

        locations.Setup(value => value.ExistsAsync(locationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sessions.Setup(value => value.CreateAsync(It.IsAny<TestingSession>()))
            .ReturnsAsync((TestingSession session) => session);
        mediator.Setup(value => value.Publish(
                It.IsAny<TestingSessionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var created = await handler.Handle(command, default);
        created.TestingRequestId.Should().Be(requestId);
        created.LocationId.Should().Be(locationId);
        created.SessionName.Should().Be(command.Title);
        created.EndTime.Should().Be(command.ScheduledDate.Add(command.Duration));
        created.Status.Should().Be(SessionStatus.Scheduled);

        var withoutLocation = await handler.Handle(Command(requestId, null), default);
        withoutLocation.LocationId.Should().Be(Guid.Empty);
        mediator.Verify(value => value.Publish(
            It.IsAny<TestingSessionCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RequestQueryHandlers_ForwardPaginationDetailAndOptionalFilters()
    {
        var projectVersionId = Guid.NewGuid();
        var expected = new TestingRequest
        {
            Id = Guid.NewGuid(),
            ProjectVersionId = projectVersionId,
            Status = TestingRequestStatus.Open
        };
        var excluded = new TestingRequest
        {
            Id = Guid.NewGuid(),
            ProjectVersionId = Guid.NewGuid(),
            Status = TestingRequestStatus.Completed
        };
        var service = new Mock<ITestingRequestService>();
        service.Setup(value => value.GetWithPaginationAsync(5, 10)).ReturnsAsync([expected, excluded]);
        service.Setup(value => value.GetByIdWithDetailsAsync(expected.Id)).ReturnsAsync(expected);

        var listHandler = new GetTestingRequestsQueryHandler(service.Object);
        (await listHandler.Handle(new GetTestingRequestsQuery(5, 10), default))
            .Should().BeEquivalentTo([expected, excluded]);
        (await listHandler.Handle(new GetTestingRequestsQuery(
            5, 10, projectVersionId, TestingRequestStatus.Open), default))
            .Should().ContainSingle().Which.Should().BeSameAs(expected);

        var detail = await new GetTestingRequestQueryHandler(service.Object)
            .Handle(new GetTestingRequestQuery(expected.Id), default);
        detail.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task LegacyEventsAndRepositories_AreConstructibleAndForwardNotifications()
    {
        var context = Mock.Of<IApplicationDbContext>();
        _ = new TestingRequestRepository(context);
        _ = new TestingLocationRepository(context);

        var userId = Guid.NewGuid();
        var userCreated = new UserCreatedEvent(userId, "Member");
        userCreated.UserId.Should().Be(userId);
        userCreated.Name.Should().Be("Member");

        var requestEvent = new TestingRequestCreatedEvent(
            Guid.NewGuid(), null, "Request", userId, SystemClock.UtcNow);
        var handler = new TestingRequestCreatedEventHandler(
            NullLogger<TestingRequestCreatedEventHandler>.Instance);
        await handler.Handle(requestEvent, default);
    }

    [Fact]
    public void UserCreatedPermissionHandler_IsConstructibleWithItsRuntimeDependencies()
    {
        var handlerType = typeof(TestingRequest).Assembly.GetType(
            "GameGuild.TestingLab.UserCreatedTestingLabPermissionHandler", throwOnError: true)!;
        var loggerType = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>).MakeGenericType(handlerType);
        var logger = Activator.CreateInstance(loggerType);

        var handler = Activator.CreateInstance(
            handlerType,
            logger,
            Mock.Of<IApplicationDbContext>(),
            new ConfigurationBuilder().Build());

        handler.Should().NotBeNull();
    }

    [Fact]
    public void ModuleAndResourcePermissionAdapters_RegisterAndMapEndpoints()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var module = new TestingLabModule();

        module.Name.Should().Be("TestingLab");
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ITestingRequestRepository));

        var endpoints = Mock.Of<IEndpointRouteBuilder>();
        module.MapEndpoints(endpoints).Should().BeSameAs(endpoints);
        endpoints.UseTestingLabModule().Should().BeSameAs(endpoints);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        new TestingParticipantPermission(userId, tenantId, resourceId, PermissionType.Read)
            .HasPermission(PermissionType.Read).Should().BeTrue();
        new TestingLocationPermission(userId, tenantId, resourceId, PermissionType.Edit)
            .HasPermission(PermissionType.Edit).Should().BeTrue();
        new SessionWaitlistPermission(userId, tenantId, resourceId, PermissionType.Create)
            .HasPermission(PermissionType.Create).Should().BeTrue();
    }

    private static CreateTestingSessionCommand Command(Guid requestId, Guid? locationId) => new(
        requestId,
        "Session",
        "Description",
        SystemClock.UtcNow.AddDays(1),
        TimeSpan.FromHours(2),
        TestingMode.Online,
        locationId,
        10,
        RegistrationType.Tester);

    private static Func<Task> InvokingAsync(Func<Task> action) => action;
}
