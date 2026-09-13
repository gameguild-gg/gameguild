using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Authoring;

public sealed class ProgramContentAuthoringControllerTests
{
    [Fact]
    public void SerializeStreamEvent_UsesTheCamelCaseContractExpectedByTheWebClient()
    {
        var streamEvent = new AiStreamEvent(
            7,
            "delta",
            "Improved text",
            Guid.NewGuid(),
            "Running");

        var json = ProgramContentAuthoringController.SerializeStreamEvent(streamEvent);

        json.Should().Contain("\"sequence\":7");
        json.Should().Contain("\"delta\":\"Improved text\"");
        json.Should().NotContain("\"Sequence\"");
        json.Should().NotContain("\"Delta\"");
    }

    [Fact]
    public async Task SaveDraft_WhenAssetManifestIsInvalid_ReturnsUnprocessableEntity()
    {
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(
                It.IsAny<SaveProgramContentDraftCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("One or more assets are not available in this lesson."));
        var controller = CreateController(sender.Object);

        var result = await controller.SaveDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SaveAuthoringDraftRequest(1, CreatePayload()),
            CancellationToken.None);

        var unprocessable = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessable.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task Publish_WhenAssetSecurityReviewIsIncomplete_ReturnsUnprocessableEntity()
    {
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(
                It.IsAny<PublishProgramContentDraftCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Every lesson asset must finish security review before publishing."));
        var controller = CreateController(sender.Object);

        var result = await controller.Publish(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PublishAuthoringDraftRequest(1),
            CancellationToken.None);

        var unprocessable = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessable.StatusCode.Should().Be(422);
    }

    private static ProgramContentAuthoringController CreateController(ISender sender)
    {
        var actorId = Guid.NewGuid();
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(candidate => candidate.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
        });
        return new ProgramContentAuthoringController(
            Mock.Of<IProgramContentAuthoringService>(),
            actor.Object,
            sender);
    }

    private static AuthoringContentPayload CreatePayload() => new(
        "Lesson",
        "lesson",
        null,
        ProgramContentType.Lesson,
        "asset://11111111-1111-4111-8111-111111111111",
        null,
        LessonContentFormat.Markdown,
        null,
        true,
        5,
        EstimatedMinutesSource.Manual,
        Visibility.Public);
}
