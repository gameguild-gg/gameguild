using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingRequestServiceCoverageTests
{
    [Fact]
    public async Task CrudAndQueryMethods_DelegateToTenantScopedOperations()
    {
        var request = Request(TestingRequestStatus.Open);
        var projectVersionId = request.ProjectVersionId.GetValueOrDefault();
        var operations = new Mock<ITestingRequestOperations>(MockBehavior.Strict);
        var participants = new Mock<ITestingParticipantOperations>(MockBehavior.Strict);
        operations.Setup(service => service.GetAllTestingRequestsAsync()).ReturnsAsync([request]);
        operations.Setup(service => service.GetTestingRequestsAsync(2, 3, false)).ReturnsAsync([request]);
        operations.Setup(service => service.GetTestingRequestByIdAsync(request.Id)).ReturnsAsync(request);
        operations.Setup(service => service.GetTestingRequestByIdWithDetailsAsync(request.Id)).ReturnsAsync(request);
        operations.Setup(service => service.CreateTestingRequestAsync(request)).ReturnsAsync(request);
        operations.Setup(service => service.UpdateTestingRequestAsync(request)).ReturnsAsync(request);
        operations.Setup(service => service.DeleteTestingRequestAsync(request.Id)).ReturnsAsync(true);
        operations.Setup(service => service.RestoreTestingRequestAsync(request.Id)).ReturnsAsync(true);
        operations.Setup(service => service.GetTestingRequestsByProjectVersionAsync(projectVersionId)).ReturnsAsync([request]);
        operations.Setup(service => service.GetTestingRequestsByStatusAsync(TestingRequestStatus.Open)).ReturnsAsync([request]);
        operations.Setup(service => service.GetActiveTestingRequestsAsync()).ReturnsAsync([request]);
        operations.Setup(service => service.SearchTestingRequestsAsync("arena")).ReturnsAsync([request]);
        var service = new TestingRequestService(operations.Object, participants.Object);

        (await service.GetAllAsync()).Should().ContainSingle();
        (await service.GetWithPaginationAsync(2, 3)).Should().ContainSingle();
        (await service.GetByIdAsync(request.Id)).Should().BeSameAs(request);
        (await service.GetByIdWithDetailsAsync(request.Id)).Should().BeSameAs(request);
        (await service.CreateAsync(request)).Should().BeSameAs(request);
        (await service.UpdateAsync(request)).Should().BeSameAs(request);
        (await service.DeleteAsync(request.Id)).Should().BeTrue();
        (await service.RestoreAsync(request.Id)).Should().BeTrue();
        (await service.GetByProjectVersionAsync(projectVersionId)).Should().ContainSingle();
        (await service.GetByStatusAsync(TestingRequestStatus.Open)).Should().ContainSingle();
        (await service.GetActiveRequestsAsync()).Should().ContainSingle();
        (await service.SearchAsync("arena")).Should().ContainSingle();
        operations.VerifyAll();
    }

    [Fact]
    public async Task ClosureAndJoinRules_FilterExpiredRequestsAndProtectCapacityAndDuplicates()
    {
        var now = SystemClock.UtcNow;
        var expiredLater = Request(TestingRequestStatus.Open, now.AddHours(-1));
        var expiredEarlier = Request(TestingRequestStatus.InProgress, now.AddHours(-2));
        var notClosable = Request(TestingRequestStatus.Active, now.AddHours(-3));
        var future = Request(TestingRequestStatus.Open, now.AddHours(1));
        var operations = new Mock<ITestingRequestOperations>();
        var participants = new Mock<ITestingParticipantOperations>();
        operations.Setup(service => service.GetAllTestingRequestsAsync())
            .ReturnsAsync([expiredLater, future, notClosable, expiredEarlier]);
        var service = new TestingRequestService(operations.Object, participants.Object);

        (await service.GetRequestsNeedingClosureAsync()).Should().Equal(expiredEarlier, expiredLater);

        operations.Setup(service => service.GetTestingRequestByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TestingRequest?)null);
        (await service.CanUserJoinTestingAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
        operations.Setup(service => service.GetTestingRequestByIdAsync(notClosable.Id)).ReturnsAsync(notClosable);
        (await service.CanUserJoinTestingAsync(Guid.NewGuid(), notClosable.Id)).Should().BeFalse();

        var open = Request(TestingRequestStatus.Open);
        var userId = Guid.NewGuid();
        operations.Setup(service => service.GetTestingRequestByIdAsync(open.Id)).ReturnsAsync(open);
        participants.Setup(service => service.IsUserParticipantAsync(open.Id, userId)).ReturnsAsync(true);
        (await service.CanUserJoinTestingAsync(userId, open.Id)).Should().BeFalse();

        participants.Setup(service => service.IsUserParticipantAsync(open.Id, userId)).ReturnsAsync(false);
        open.MaxTesters = null;
        (await service.CanUserJoinTestingAsync(userId, open.Id)).Should().BeTrue();
        open.MaxTesters = 2;
        open.CurrentTesterCount = 1;
        (await service.CanUserJoinTestingAsync(userId, open.Id)).Should().BeTrue();
        open.CurrentTesterCount = 2;
        (await service.CanUserJoinTestingAsync(userId, open.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task JoinLeaveAndClose_ApplyStateOnlyAfterSuccessfulParticipantOperations()
    {
        var userId = Guid.NewGuid();
        var request = Request(TestingRequestStatus.Open);
        var operations = new Mock<ITestingRequestOperations>();
        var participants = new Mock<ITestingParticipantOperations>();
        var service = new TestingRequestService(operations.Object, participants.Object);

        operations.Setup(service => service.GetTestingRequestByIdAsync(request.Id)).ReturnsAsync(request);
        participants.Setup(service => service.IsUserParticipantAsync(request.Id, userId)).ReturnsAsync(false);
        participants.Setup(service => service.AddParticipantAsync(request.Id, userId)).ReturnsAsync(new TestingParticipant());
        operations.Setup(service => service.UpdateTestingRequestAsync(request)).ReturnsAsync(request);
        (await service.JoinTestingAsync(userId, request.Id)).CurrentTesterCount.Should().Be(1);

        participants.Setup(service => service.RemoveParticipantAsync(request.Id, userId)).ReturnsAsync(true);
        (await service.LeaveTestingAsync(userId, request.Id)).CurrentTesterCount.Should().Be(0);

        (await service.CloseTestingRequestAsync(request.Id)).Status.Should().Be(TestingRequestStatus.Completed);

        operations.Setup(service => service.GetTestingRequestByIdAsync(Guid.Empty)).ReturnsAsync((TestingRequest?)null);
        await FluentActions.Awaiting(() => service.LeaveTestingAsync(userId, Guid.Empty)).Should().ThrowAsync<KeyNotFoundException>();
        await FluentActions.Awaiting(() => service.CloseTestingRequestAsync(Guid.Empty)).Should().ThrowAsync<KeyNotFoundException>();

        var unavailable = Request(TestingRequestStatus.Draft);
        operations.Setup(service => service.GetTestingRequestByIdAsync(unavailable.Id)).ReturnsAsync(unavailable);
        await FluentActions.Awaiting(() => service.JoinTestingAsync(userId, unavailable.Id)).Should().ThrowAsync<InvalidOperationException>();

        var participating = Request(TestingRequestStatus.Open);
        participating.CurrentTesterCount = 0;
        operations.Setup(service => service.GetTestingRequestByIdAsync(participating.Id)).ReturnsAsync(participating);
        participants.Setup(service => service.RemoveParticipantAsync(participating.Id, userId)).ReturnsAsync(false);
        await FluentActions.Awaiting(() => service.LeaveTestingAsync(userId, participating.Id)).Should().ThrowAsync<InvalidOperationException>();
    }

    private static TestingRequest Request(TestingRequestStatus status, DateTime? end = null) => new()
    {
        Id = Guid.NewGuid(),
        ProjectVersionId = Guid.NewGuid(),
        Status = status,
        EndDate = end ?? SystemClock.UtcNow.AddDays(1),
        MaxTesters = 5
    };
}

public sealed class TestingSessionServiceCoverageTests
{
    [Fact]
    public async Task CrudAndQueryMethods_DelegateToTenantScopedOperations()
    {
        var session = Session(SessionStatus.Scheduled);
        var operations = new Mock<ITestingSessionOperations>(MockBehavior.Strict);
        var participants = new Mock<ITestingParticipantOperations>(MockBehavior.Strict);
        operations.Setup(service => service.GetAllTestingSessionsAsync()).ReturnsAsync([session]);
        operations.Setup(service => service.GetTestingSessionsAsync(2, 3)).ReturnsAsync([session]);
        operations.Setup(service => service.GetTestingSessionByIdAsync(session.Id)).ReturnsAsync(session);
        operations.Setup(service => service.GetTestingSessionByIdWithDetailsAsync(session.Id)).ReturnsAsync(session);
        operations.Setup(service => service.CreateTestingSessionAsync(session)).ReturnsAsync(session);
        operations.Setup(service => service.UpdateTestingSessionAsync(session)).ReturnsAsync(session);
        operations.Setup(service => service.DeleteTestingSessionAsync(session.Id)).ReturnsAsync(true);
        operations.Setup(service => service.RestoreTestingSessionAsync(session.Id)).ReturnsAsync(true);
        operations.Setup(service => service.GetTestingSessionsByRequestAsync(session.TestingRequestId)).ReturnsAsync([session]);
        operations.Setup(service => service.GetTestingSessionsByStatusAsync(SessionStatus.Scheduled)).ReturnsAsync([session]);
        operations.Setup(service => service.GetTestingSessionsByLocationAsync(session.LocationId)).ReturnsAsync([session]);
        var service = new TestingSessionService(operations.Object, participants.Object);

        (await service.GetAllAsync()).Should().ContainSingle();
        (await service.GetWithPaginationAsync(2, 3)).Should().ContainSingle();
        (await service.GetByIdAsync(session.Id)).Should().BeSameAs(session);
        (await service.GetByIdWithDetailsAsync(session.Id)).Should().BeSameAs(session);
        (await service.CreateAsync(session)).Should().BeSameAs(session);
        (await service.UpdateAsync(session)).Should().BeSameAs(session);
        (await service.DeleteAsync(session.Id)).Should().BeTrue();
        (await service.RestoreAsync(session.Id)).Should().BeTrue();
        (await service.GetByTestingRequestAsync(session.TestingRequestId)).Should().ContainSingle();
        (await service.GetByStatusAsync(SessionStatus.Scheduled)).Should().ContainSingle();
        (await service.GetByLocationAsync(session.LocationId)).Should().ContainSingle();
        operations.VerifyAll();
    }

    [Fact]
    public async Task TimeQueries_FilterAndSortSessions()
    {
        var now = SystemClock.UtcNow;
        var past = Session(SessionStatus.Scheduled, now.AddHours(-2), now.AddHours(-1));
        var later = Session(SessionStatus.Scheduled, now.AddHours(3), now.AddHours(4));
        var sooner = Session(SessionStatus.Scheduled, now.AddHours(1), now.AddHours(2));
        var active = Session(SessionStatus.Active, now.AddMinutes(-30), now.AddMinutes(30));
        var inactiveWindow = Session(SessionStatus.Active, now.AddHours(1), now.AddHours(2));
        var operations = new Mock<ITestingSessionOperations>();
        var participants = new Mock<ITestingParticipantOperations>();
        operations.Setup(service => service.GetTestingSessionsByStatusAsync(SessionStatus.Scheduled)).ReturnsAsync([past, later, sooner]);
        operations.Setup(service => service.GetTestingSessionsByStatusAsync(SessionStatus.Active)).ReturnsAsync([inactiveWindow, active]);
        operations.Setup(service => service.GetAllTestingSessionsAsync()).ReturnsAsync([later, past, sooner]);
        var service = new TestingSessionService(operations.Object, participants.Object);

        (await service.GetUpcomingSessionsAsync()).Should().Equal(sooner, later);
        (await service.GetActiveSessionsAsync()).Should().Equal(active);
        (await service.GetByDateRangeAsync(sooner.SessionDate, later.SessionDate)).Should().Equal(sooner, later);
    }

    [Fact]
    public async Task ParticipationAndTransitions_HandleSuccessAndFailurePaths()
    {
        var userId = Guid.NewGuid();
        var session = Session(SessionStatus.Scheduled);
        var operations = new Mock<ITestingSessionOperations>();
        var participants = new Mock<ITestingParticipantOperations>();
        var service = new TestingSessionService(operations.Object, participants.Object);

        operations.Setup(service => service.GetTestingSessionByIdAsync(Guid.Empty)).ReturnsAsync((TestingSession?)null);
        (await service.CanUserJoinSessionAsync(userId, Guid.Empty)).Should().BeFalse();

        session.RegisteredTesterCount = session.MaxTesters;
        operations.Setup(service => service.GetTestingSessionByIdAsync(session.Id)).ReturnsAsync(session);
        (await service.CanUserJoinSessionAsync(userId, session.Id)).Should().BeFalse();

        session.RegisteredTesterCount = 0;
        participants.Setup(service => service.GetSessionRegistrationsAsync(session.Id))
            .ReturnsAsync([new SessionRegistration { UserId = userId }]);
        (await service.CanUserJoinSessionAsync(userId, session.Id)).Should().BeFalse();
        participants.Setup(service => service.GetSessionRegistrationsAsync(session.Id)).ReturnsAsync([]);
        (await service.CanUserJoinSessionAsync(userId, session.Id)).Should().BeTrue();

        participants.Setup(service => service.RegisterForSessionAsync(session.Id, userId, RegistrationType.Tester, null))
            .ReturnsAsync(new SessionRegistration());
        (await service.JoinSessionAsync(userId, session.Id)).Should().BeSameAs(session);

        participants.Setup(service => service.UnregisterFromSessionAsync(session.Id, userId)).ReturnsAsync(false);
        await FluentActions.Awaiting(() => service.LeaveSessionAsync(userId, session.Id)).Should().ThrowAsync<InvalidOperationException>();
        participants.Setup(service => service.UnregisterFromSessionAsync(session.Id, userId)).ReturnsAsync(true);
        (await service.LeaveSessionAsync(userId, session.Id)).Should().BeSameAs(session);

        operations.Setup(service => service.UpdateTestingSessionAsync(It.IsAny<TestingSession>()))
            .ReturnsAsync((TestingSession candidate) => candidate);
        (await service.StartSessionAsync(session.Id)).Status.Should().Be(SessionStatus.Active);
        (await service.EndSessionAsync(session.Id)).Status.Should().Be(SessionStatus.Completed);

        var cancellable = Session(SessionStatus.Scheduled);
        operations.Setup(service => service.GetTestingSessionByIdAsync(cancellable.Id)).ReturnsAsync(cancellable);
        (await service.CancelSessionAsync(cancellable.Id)).Status.Should().Be(SessionStatus.Cancelled);

        await FluentActions.Awaiting(() => service.StartSessionAsync(Guid.Empty)).Should().ThrowAsync<KeyNotFoundException>();
        participants.Setup(service => service.RegisterForSessionAsync(Guid.Empty, userId, RegistrationType.Tester, null))
            .ReturnsAsync(new SessionRegistration());
        await FluentActions.Awaiting(() => service.JoinSessionAsync(userId, Guid.Empty)).Should().ThrowAsync<KeyNotFoundException>();
        participants.Setup(service => service.UnregisterFromSessionAsync(Guid.Empty, userId)).ReturnsAsync(true);
        await FluentActions.Awaiting(() => service.LeaveSessionAsync(userId, Guid.Empty)).Should().ThrowAsync<KeyNotFoundException>();
    }

    private static TestingSession Session(SessionStatus status, DateTime? start = null, DateTime? end = null)
    {
        var startsAt = start ?? SystemClock.UtcNow.AddHours(1);
        return new TestingSession
        {
            Id = Guid.NewGuid(),
            TestingRequestId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Status = status,
            StartTime = startsAt,
            EndTime = end ?? startsAt.AddHours(1),
            SessionDate = startsAt,
            MaxTesters = 2
        };
    }
}
