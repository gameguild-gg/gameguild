using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabEndpointWorkflowAdapterCoverageTests
{
    [Fact]
    public async Task SessionHandler_ForwardsCrudAndAttendanceCommands()
    {
        var service = new Mock<ITestingSessionOperations>();
        var created = new TestingSession { Id = Guid.NewGuid() };
        var updated = new TestingSession { Id = Guid.NewGuid() };
        service.Setup(value => value.CreateTestingSessionAsync(It.IsAny<TestingSession>())).ReturnsAsync(created);
        service.Setup(value => value.UpdateTestingSessionAsync(updated)).ReturnsAsync(updated);
        service.Setup(value => value.DeleteTestingSessionAsync(updated.Id)).ReturnsAsync(true);
        service.Setup(value => value.RestoreTestingSessionAsync(updated.Id)).ReturnsAsync(true);
        service.Setup(value => value.UpdateSessionAttendanceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AttendanceStatus>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        var handler = new TestingSessionEndpointCommandHandler(service.Object);
        var userId = Guid.NewGuid();

        (await handler.Handle(new CreateTestingSessionEndpointCommand(
            new CreateTestingSessionDto { SessionName = "Session" }, userId), default))
            .Should().BeSameAs(created);
        (await handler.Handle(new UpdateTestingSessionEndpointCommand(updated.Id, updated), default))
            .Should().BeSameAs(updated);
        (await handler.Handle(new DeleteTestingSessionEndpointCommand(updated.Id), default)).Should().BeTrue();
        (await handler.Handle(new RestoreTestingSessionEndpointCommand(updated.Id), default)).Should().BeTrue();
        (await handler.Handle(new UpdateTestingSessionAttendanceEndpointCommand(
            updated.Id, userId, AttendanceStatus.Present, Guid.NewGuid()), default)).Should().Be(Unit.Value);
    }

    [Fact]
    public async Task RequestHandler_MapsCreateUpdateMissingAndLifecycleCommands()
    {
        var service = new Mock<ITestingRequestOperations>();
        var created = new TestingRequest { Id = Guid.NewGuid() };
        var existing = new TestingRequest { Id = Guid.NewGuid(), Title = "Before" };
        var simple = new TestingRequest { Id = Guid.NewGuid() };
        service.Setup(value => value.CreateTestingRequestAsync(It.IsAny<TestingRequest>())).ReturnsAsync(created);
        service.Setup(value => value.GetTestingRequestByIdAsync(existing.Id)).ReturnsAsync(existing);
        service.Setup(value => value.GetTestingRequestByIdAsync(Guid.Empty)).ReturnsAsync((TestingRequest?)null);
        service.Setup(value => value.UpdateTestingRequestAsync(existing)).ReturnsAsync(existing);
        service.Setup(value => value.DeleteTestingRequestAsync(existing.Id)).ReturnsAsync(true);
        service.Setup(value => value.RestoreTestingRequestAsync(existing.Id)).ReturnsAsync(true);
        service.Setup(value => value.CreateSimpleTestingRequestAsync(
                It.IsAny<CreateSimpleTestingRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(simple);
        var handler = new TestingRequestEndpointCommandHandler(service.Object);
        var userId = Guid.NewGuid();
        var update = new UpdateTestingRequestDto { Title = "After" };

        (await handler.Handle(new CreateTestingRequestEndpointCommand(
            new CreateTestingRequestDto { Title = "New" }, userId), default)).Should().BeSameAs(created);
        (await handler.Handle(new UpdateTestingRequestEndpointCommand(existing.Id, update), default))
            .Should().BeSameAs(existing);
        existing.Title.Should().Be("After");
        (await handler.Handle(new UpdateTestingRequestEndpointCommand(Guid.Empty, update), default))
            .Should().BeNull();
        (await handler.Handle(new DeleteTestingRequestEndpointCommand(existing.Id), default)).Should().BeTrue();
        (await handler.Handle(new RestoreTestingRequestEndpointCommand(existing.Id), default)).Should().BeTrue();
        (await handler.Handle(new CreateSimpleTestingRequestEndpointCommand(
            new CreateSimpleTestingRequestDto(), userId), default)).Should().BeSameAs(simple);
    }

    [Fact]
    public async Task SettingsHandler_MutatesThenReturnsTheCanonicalDto()
    {
        var service = new Mock<ITestingLabSettingsService>();
        var tenantId = Guid.NewGuid();
        var dto = new TestingLabSettingsDto { Id = Guid.NewGuid(), TenantId = tenantId };
        service.Setup(value => value.CreateOrUpdateTestingLabSettingsAsync(
                tenantId, It.IsAny<CreateTestingLabSettingsDto>()))
            .ReturnsAsync(new TestingLabSettings());
        service.Setup(value => value.UpdateTestingLabSettingsAsync(
                tenantId, It.IsAny<UpdateTestingLabSettingsDto>()))
            .ReturnsAsync(new TestingLabSettings());
        service.Setup(value => value.ResetTestingLabSettingsAsync(tenantId))
            .ReturnsAsync(new TestingLabSettings());
        service.Setup(value => value.GetTestingLabSettingsDtoAsync(tenantId)).ReturnsAsync(dto);
        var handler = new TestingLabSettingsEndpointCommandHandler(service.Object);

        (await handler.Handle(new CreateOrUpdateTestingLabSettingsEndpointCommand(
            tenantId, new CreateTestingLabSettingsDto()), default)).Should().BeSameAs(dto);
        (await handler.Handle(new UpdateTestingLabSettingsEndpointCommand(
            tenantId, new UpdateTestingLabSettingsDto()), default)).Should().BeSameAs(dto);
        (await handler.Handle(new ResetTestingLabSettingsEndpointCommand(tenantId), default))
            .Should().BeSameAs(dto);

        service.Verify(value => value.GetTestingLabSettingsDtoAsync(tenantId), Times.Exactly(3));
    }
}
