using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabEndpointAdapterCoverageTests
{
    [Fact]
    public async Task FeedbackHandler_ForwardsEveryCommandContract()
    {
        var service = new Mock<ITestingFeedbackOperations>();
        var feedback = new TestingFeedback { Id = Guid.NewGuid() };
        service.Setup(value => value.AddFeedbackAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<TestingContext>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .ReturnsAsync(feedback);
        service.Setup(value => value.SubmitFeedbackAsync(It.IsAny<SubmitFeedbackDto>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        service.Setup(value => value.ReportFeedbackAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        service.Setup(value => value.RateFeedbackQualityAsync(
                It.IsAny<Guid>(), It.IsAny<FeedbackQuality>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        var handler = new TestingFeedbackEndpointCommandHandler(service.Object);
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        (await handler.Handle(new AddTestingFeedbackEndpointCommand(
            requestId, userId, Guid.NewGuid(), "data", TestingContext.Online, null, "notes"), default))
            .Should().BeSameAs(feedback);
        (await handler.Handle(new SubmitTestingFeedbackEndpointCommand(new SubmitFeedbackDto(), userId), default))
            .Should().Be(Unit.Value);
        (await handler.Handle(new ReportTestingFeedbackEndpointCommand(feedbackId, "reason", userId), default))
            .Should().Be(Unit.Value);
        (await handler.Handle(new RateTestingFeedbackQualityEndpointCommand(
            feedbackId, FeedbackQuality.High, userId), default)).Should().Be(Unit.Value);
    }

    [Fact]
    public async Task LocationHandler_MapsCreatesUpdatesAndLifecycleCommands()
    {
        var service = new Mock<ITestingLocationOperations>();
        var created = new TestingLocation { Id = Guid.NewGuid(), Name = "Created" };
        var existing = new TestingLocation { Id = Guid.NewGuid(), Name = "Before" };
        service.Setup(value => value.CreateTestingLocationAsync(It.IsAny<TestingLocation>()))
            .ReturnsAsync(created);
        service.Setup(value => value.GetTestingLocationByIdAsync(existing.Id)).ReturnsAsync(existing);
        service.Setup(value => value.GetTestingLocationByIdAsync(Guid.Empty))
            .ReturnsAsync((TestingLocation?)null);
        service.Setup(value => value.UpdateTestingLocationAsync(existing)).ReturnsAsync(existing);
        service.Setup(value => value.DeleteTestingLocationAsync(existing.Id)).ReturnsAsync(true);
        service.Setup(value => value.RestoreTestingLocationAsync(existing.Id)).ReturnsAsync(true);
        var handler = new TestingLocationEndpointCommandHandler(service.Object);
        var create = new CreateTestingLocationDto
        {
            Name = "Mapped", Description = "Description", Address = "Address", City = "City",
            State = "State", PostalCode = "00000", Country = "BR", MaxTestersCapacity = 12,
            MaxProjectsCapacity = 3, EquipmentAvailable = "PC", IsVirtual = true,
            VirtualUrl = "https://example.com", ContactEmail = "lab@example.com",
            ContactPhone = "123", Status = LocationStatus.Active
        };
        var update = new UpdateTestingLocationDto
        {
            Name = "After", Description = "Updated", Address = "New address", City = "New city",
            State = "New state", PostalCode = "11111", Country = "CA", MaxTestersCapacity = 20,
            MaxProjectsCapacity = 5, EquipmentAvailable = "Console", IsVirtual = false,
            VirtualUrl = "https://updated.example.com", ContactEmail = "updated@example.com",
            ContactPhone = "456", Status = LocationStatus.Maintenance
        };

        (await handler.Handle(new CreateTestingLocationEndpointCommand(create), default)).Should().BeSameAs(created);
        (await handler.Handle(new UpdateTestingLocationEndpointCommand(existing.Id, update), default))
            .Should().BeSameAs(existing);
        existing.Name.Should().Be("After");
        existing.Status.Should().Be(LocationStatus.Maintenance);
        (await handler.Handle(new UpdateTestingLocationEndpointCommand(Guid.Empty, update), default))
            .Should().BeNull();
        (await handler.Handle(new DeleteTestingLocationEndpointCommand(existing.Id), default)).Should().BeTrue();
        (await handler.Handle(new RestoreTestingLocationEndpointCommand(existing.Id), default)).Should().BeTrue();
    }

    [Fact]
    public async Task ParticipantHandler_ForwardsRegistrationAndWaitlistCommands()
    {
        var service = new Mock<ITestingParticipantOperations>();
        var participant = new TestingParticipant();
        var registration = new SessionRegistration();
        var waitlist = new SessionWaitlist();
        service.Setup(value => value.AddParticipantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(participant);
        service.Setup(value => value.RemoveParticipantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        service.Setup(value => value.RegisterForSessionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RegistrationType>(), It.IsAny<string?>()))
            .ReturnsAsync(registration);
        service.Setup(value => value.UnregisterFromSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        service.Setup(value => value.AddToWaitlistAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RegistrationType>(), It.IsAny<string?>()))
            .ReturnsAsync(waitlist);
        service.Setup(value => value.RemoveFromWaitlistAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        var handler = new TestingParticipantEndpointCommandHandler(service.Object);
        var requestId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        (await handler.Handle(new AddTestingParticipantEndpointCommand(requestId, userId), default))
            .Should().BeSameAs(participant);
        (await handler.Handle(new RemoveTestingParticipantEndpointCommand(requestId, userId), default))
            .Should().BeTrue();
        (await handler.Handle(new RegisterTestingSessionEndpointCommand(
            sessionId, userId, RegistrationType.ProjectMember, "notes"), default))
            .Should().BeSameAs(registration);
        (await handler.Handle(new UnregisterTestingSessionEndpointCommand(sessionId, userId), default))
            .Should().BeTrue();
        (await handler.Handle(new AddTestingSessionWaitlistEndpointCommand(
            sessionId, userId, RegistrationType.Tester, "wait"), default))
            .Should().BeSameAs(waitlist);
        (await handler.Handle(new RemoveTestingSessionWaitlistEndpointCommand(sessionId, userId), default))
            .Should().BeTrue();
    }
}
