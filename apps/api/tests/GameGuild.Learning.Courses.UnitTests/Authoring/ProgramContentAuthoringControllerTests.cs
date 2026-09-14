using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public async Task GetDraft_WhenDraftExists_ReturnsPayloadAndEtag()
    {
        var programId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var draft = new AuthoringDraftDto(
            Guid.NewGuid(),
            programId,
            contentId,
            CreatePayload(),
            1,
            2,
            "\"draft-2\"",
            actorId,
            DateTimeOffset.UtcNow);
        var authoring = new Mock<IProgramContentAuthoringService>();
        authoring.Setup(candidate => candidate.GetOrCreateDraft(
                programId,
                contentId,
                actorId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var controller = CreateController(
            Mock.Of<ISender>(),
            authoring.Object,
            AuthenticatedActor(actorId));

        var result = await controller.GetDraft(programId, contentId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(draft);
        controller.Response.Headers.ETag.ToString().Should().Be("\"draft-2\"");
    }

    [Fact]
    public async Task SaveDraft_WhenRevisionIsStale_ReturnsStructuredConflict()
    {
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(
                It.IsAny<SaveProgramContentDraftCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthoringRevisionConflictException(1, 3));
        var controller = CreateController(sender.Object);

        var result = await controller.SaveDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SaveAuthoringDraftRequest(1, CreatePayload()),
            CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problem = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Extensions["code"].Should().Be("AUTHORING_REVISION_CONFLICT");
        problem.Extensions["currentRevision"].Should().Be(3);
    }

    [Theory]
    [MemberData(nameof(InvalidUserActors))]
    public async Task GetDraft_WhenActorIsNotAnAuthenticatedUser_RejectsRequest(ActorContext actor)
    {
        var controller = CreateController(Mock.Of<ISender>(), actorContext: actor);

        var action = () => controller.GetDraft(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("An authenticated user actor is required.");
    }

    [Fact]
    public async Task GetAiConversations_ForTenantUser_ForwardsAuthenticatedActor()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var ai = new Mock<IAuthoringAiService>();
        ai.Setup(candidate => candidate.GetConversations(
                tenantId,
                actorId,
                programId,
                contentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AiAuthoringConversationDto>());
        var controller = CreateController(
            Mock.Of<ISender>(),
            actorContext: AuthenticatedActor(actorId, tenantId));

        var result = await controller.GetAiConversations(
            programId,
            contentId,
            ai.Object,
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ai.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(InvalidTenantUserActors))]
    public async Task GetAiConversations_WhenTenantUserIdentityIsIncomplete_RejectsRequest(ActorContext actor)
    {
        var controller = CreateController(Mock.Of<ISender>(), actorContext: actor);

        var action = () => controller.GetAiConversations(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Mock.Of<IAuthoringAiService>(),
            CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("An authenticated tenant user actor is required.");
    }

    public static IEnumerable<object[]> InvalidUserActors =>
    [
        [Actor(isAuthenticated: false, ActorKind.User, Guid.NewGuid(), Guid.NewGuid().ToString())],
        [Actor(isAuthenticated: true, (ActorKind)int.MaxValue, Guid.NewGuid(), Guid.NewGuid().ToString())],
        [Actor(isAuthenticated: true, ActorKind.User, Guid.NewGuid(), "not-a-guid")],
    ];

    public static IEnumerable<object[]> InvalidTenantUserActors =>
    [
        [Actor(isAuthenticated: false, ActorKind.User, Guid.NewGuid(), Guid.NewGuid().ToString())],
        [Actor(isAuthenticated: true, (ActorKind)int.MaxValue, Guid.NewGuid(), Guid.NewGuid().ToString())],
        [Actor(isAuthenticated: true, ActorKind.User, null, Guid.NewGuid().ToString())],
        [Actor(isAuthenticated: true, ActorKind.User, Guid.NewGuid(), "not-a-guid")],
    ];

    private static ProgramContentAuthoringController CreateController(
        ISender sender,
        IProgramContentAuthoringService? authoring = null,
        ActorContext? actorContext = null)
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(candidate => candidate.ActorContext)
            .Returns(actorContext ?? AuthenticatedActor(Guid.NewGuid()));
        var controller = new ProgramContentAuthoringController(
            authoring ?? Mock.Of<IProgramContentAuthoringService>(),
            actor.Object,
            sender);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ActorContext AuthenticatedActor(Guid actorId, Guid? tenantId = null) =>
        Actor(isAuthenticated: true, ActorKind.User, tenantId ?? Guid.NewGuid(), actorId.ToString());

    private static ActorContext Actor(bool isAuthenticated, ActorKind kind, Guid? tenantId, string subjectId) => new()
    {
        ActorKind = kind,
        SubjectId = subjectId,
        TenantId = tenantId,
        IsAuthenticated = isAuthenticated,
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>(),
    };

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
