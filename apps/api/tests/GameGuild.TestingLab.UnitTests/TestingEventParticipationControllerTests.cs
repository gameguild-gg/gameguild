using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventParticipationControllerTests
{
    [Fact]
    public async Task Register_ShouldForwardSlotNotesAndCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        using var cancellation = new CancellationTokenSource();
        var slotId = Guid.NewGuid();
        var projection = Registration(slotId);
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<RegisterTestingEventSlotCommand>(),
                cancellation.Token))
            .ReturnsAsync(Result.Success(projection));
        var controller = new TestingEventParticipationController(mediator.Object);
        var questionnaireResponse = new QuestionnaireResponse([]);

        var result = await controller.Register(
            slotId,
            new RegisterTestingEventSlotRequest("Morning session", questionnaireResponse, true),
            cancellation.Token);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(projection);
        mediator.Verify(candidate => candidate.Send(
            It.Is<RegisterTestingEventSlotCommand>(command =>
                command.SlotId == slotId &&
                command.Notes == "Morning session" &&
                command.RegistrationResponse == questionnaireResponse &&
                command.AcceptedRules),
            cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task AssignTestedProject_ShouldForwardRegistrationAndApplication()
    {
        var mediator = new Mock<IMediator>();
        var registrationId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var projection = new TestingFeedbackObligationProjection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            applicationId,
            Guid.NewGuid(),
            null,
            TestingFeedbackObligationStatus.Pending,
            null);
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<AssignTestingProjectToTesterCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(projection));
        var controller = new TestingEventParticipationController(mediator.Object);

        var result = await controller.AssignTestedProject(
            registrationId,
            new AssignTestingProjectToTesterRequest(applicationId));

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(projection);
        mediator.Verify(candidate => candidate.Send(
            It.Is<AssignTestingProjectToTesterCommand>(command =>
                command.RegistrationId == registrationId &&
                command.ApplicationId == applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitFeedback_ShouldForwardCompleteFeedbackPayload()
    {
        var mediator = new Mock<IMediator>();
        using var cancellation = new CancellationTokenSource();
        var obligationId = Guid.NewGuid();
        var request = new SubmitTestingEventFeedbackRequest(
            """{"playability":"clear"}""",
            9,
            true,
            "Strong session");
        var projection = new TestingEventFeedbackProjection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            request.FeedbackData!,
            request.OverallRating,
            request.WouldRecommend,
            request.AdditionalNotes,
            SystemClock.UtcNow);
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<SubmitTestingEventFeedbackCommand>(),
                cancellation.Token))
            .ReturnsAsync(Result.Success(projection));
        var controller = new TestingEventParticipationController(mediator.Object);

        var result = await controller.SubmitFeedback(obligationId, request, cancellation.Token);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(projection);
        mediator.Verify(candidate => candidate.Send(
            It.Is<SubmitTestingEventFeedbackCommand>(command =>
                command.ObligationId == obligationId &&
                command.FeedbackData == request.FeedbackData &&
                command.OverallRating == request.OverallRating &&
                command.WouldRecommend == request.WouldRecommend &&
                command.AdditionalNotes == request.AdditionalNotes),
            cancellation.Token), Times.Once);
    }

    private static TestingSlotRegistrationProjection Registration(Guid slotId) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        slotId,
        Guid.NewGuid(),
        TestingSlotRegistrationStatus.Registered,
        null,
        null,
        SystemClock.UtcNow,
        null,
        null,
        null,
        null,
        0);
}
