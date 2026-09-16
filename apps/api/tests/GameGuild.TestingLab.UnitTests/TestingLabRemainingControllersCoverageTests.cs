using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabRemainingControllersCoverageTests
{
    [Fact]
    public async Task TemplateEndpoints_RejectAnInactiveActorIncludingArchiveWrappers()
    {
        var actors = new Mock<IActorContextAccessor>();
        actors.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var controller = new TestingEventTemplatesController(
            Mock.Of<IApplicationDbContext>(), actors.Object, Mock.Of<ISender>());
        var request = new UpsertTestingEventTemplateRequest(
            "Template", null, "Rules", "Candidates", "Testers",
            new QuestionnaireSchema("Applications", []),
            new QuestionnaireSchema("Registrations", []),
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true);

        (await controller.GetTemplates()).Result.Should().BeOfType<UnauthorizedResult>();
        (await controller.GetRevision(Guid.NewGuid(), Guid.NewGuid())).Result
            .Should().BeOfType<UnauthorizedResult>();
        (await controller.CreateTemplate(request)).Result.Should().BeOfType<UnauthorizedResult>();
        (await controller.CreateRevision(Guid.NewGuid(), request)).Result
            .Should().BeOfType<UnauthorizedResult>();
        (await controller.ArchiveTemplate(Guid.NewGuid())).Result.Should().BeOfType<UnauthorizedResult>();
        (await controller.RestoreTemplate(Guid.NewGuid())).Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void RemainingControllers_CanBeComposedWithTheirContracts()
    {
        var actor = Mock.Of<IActorContextAccessor>();

        new TestingFeedbackController(
                Mock.Of<ITestingFeedbackOperations>(), Mock.Of<ISender>(), actor)
            .Should().NotBeNull();
        new TestingLocationsController(Mock.Of<ITestingLocationOperations>(), Mock.Of<ISender>())
            .Should().NotBeNull();
        new TestingAnalyticsController(Mock.Of<IMediator>()).Should().NotBeNull();
        new TestingSessionsController(
                Mock.Of<ITestingSessionOperations>(), Mock.Of<IMediator>(), actor,
                NullLogger<TestingSessionsController>.Instance)
            .Should().NotBeNull();
    }
}
