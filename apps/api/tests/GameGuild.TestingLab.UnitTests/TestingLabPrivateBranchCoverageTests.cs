using System.Reflection;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Projects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabPrivateBranchCoverageTests
{
    [Fact]
    public async Task OptionalPermissionServices_DelegateWhenConfigured()
    {
        var permissionService = new Mock<ITestingLabPermissionService>();
        permissionService.Setup(service => service.HasPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);
        var context = Mock.Of<IApplicationDbContext>();
        var actors = Mock.Of<IActorContextAccessor>();
        var projectAuthorization = Mock.Of<IProjectAuthorizationService>();
        var lifecycleLock = Mock.Of<IProjectLifecycleLock>();

        var applicationHandlers = new TestingApplicationHandlers(
            context,
            actors,
            projectAuthorization,
            NullLogger<TestingApplicationHandlers>.Instance,
            lifecycleLock,
            permissionService.Object);
        (await InvokePermissionAsync(
            applicationHandlers,
            "HasApplicationPermissionAsync",
            TestingLabResourceTypes.Application)).Should().BeTrue();

        var eventHandlers = new TestingEventHandlers(context, actors, permissionService.Object);
        (await InvokePermissionAsync(
            eventHandlers,
            "HasTestingLabPermissionAsync",
            TestingLabResourceTypes.Event)).Should().BeTrue();

        var participationHandlers = new TestingParticipationHandlers(
            context,
            actors,
            NullLogger<TestingParticipationHandlers>.Instance,
            lifecycleLock,
            testingLabPermissionService: permissionService.Object);
        (await InvokePermissionAsync(
            participationHandlers,
            "HasTestingLabPermissionAsync",
            TestingLabResourceTypes.Participant)).Should().BeTrue();

        permissionService.Verify(service => service.HasPermissionAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()),
            Times.Exactly(3));
    }

    [Fact]
    public void ApplicationWindow_RejectsBeforeOpeningAndAfterClosing()
    {
        var method = typeof(TestingApplicationHandlers).GetMethod(
            "AcceptsApplicationChanges",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var beforeOpening = OpenEvent(
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            SystemClock.UtcNow.AddDays(4));
        var afterClosing = OpenEvent(
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(-1),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2));

        method.Invoke(null, [beforeOpening]).Should().Be(false);
        method.Invoke(null, [afterClosing]).Should().Be(false);
    }

    [Fact]
    public void EventInterval_RejectsEitherBoundaryOutsideTheEvent()
    {
        var method = typeof(TestingEventHandlers).GetMethod(
            "IsWithinEvent",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var start = SystemClock.UtcNow.AddDays(1);
        var end = start.AddHours(4);
        var testingEvent = OpenEvent(start.AddDays(-2), start.AddDays(-1), start, end);

        method.Invoke(null, [testingEvent, start.AddMinutes(-1), end.AddMinutes(-1)]).Should().Be(false);
        method.Invoke(null, [testingEvent, start.AddMinutes(1), end.AddMinutes(1)]).Should().Be(false);
        method.Invoke(null, [testingEvent, start.AddMinutes(1), end.AddMinutes(-1)]).Should().Be(true);
    }

    private static async Task<bool> InvokePermissionAsync(
        object handler,
        string methodName,
        string resourceType)
    {
        var handlerType = handler.GetType();
        var actorType = handlerType.GetNestedType("ActorScope", BindingFlags.NonPublic)!;
        var actor = Activator.CreateInstance(
            actorType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [Guid.NewGuid(), Guid.NewGuid(), null],
            culture: null)!;
        var method = handlerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments = methodName == "HasApplicationPermissionAsync"
            ? [actor, TestingLabActions.Read, null, CancellationToken.None]
            : [actor, TestingLabActions.Read, resourceType, null];

        return await (Task<bool>)method.Invoke(handler, arguments)!;
    }

    private static TestingEvent OpenEvent(
        DateTime applicationsOpenAt,
        DateTime applicationsCloseAt,
        DateTime startsAt,
        DateTime endsAt)
    {
        var schema = new QuestionnaireSchema("Basic", []);
        var testingEvent = TestingEvent.Create(
            "Event",
            TestingEventMode.Online,
            Guid.NewGuid(),
            applicationsOpenAt,
            applicationsCloseAt,
            startsAt,
            endsAt,
            true,
            TestingEventApprovalMode.ManagerOnly,
            Guid.NewGuid());
        testingEvent.Configure("Rules", "Candidates", "Testers", schema, schema);
        testingEvent.OpenApplications();
        return testingEvent;
    }
}
