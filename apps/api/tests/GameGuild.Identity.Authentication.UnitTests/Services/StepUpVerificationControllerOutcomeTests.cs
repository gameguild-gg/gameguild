using GameGuild;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpVerificationControllerOutcomeTests
{
    [Fact]
    public async Task InvalidFactorReturns400OnlyAfterTheRecordedDenialWasReturnedByTheCommand()
    {
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StepUpVerificationFailedException("sensitive provider detail"));
        var sender = new ActualHandlerSender(service.Object);
        var result = await Controller(sender).VerifyChallenge(Guid.NewGuid(), new VerifyStepUpChallengeRequest(MfaMethod.Totp, "invalid"), CancellationToken.None);
        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal("MFA evidence is invalid.", problem.Detail);
        Assert.NotNull(sender.ReturnedOutcome);
        Assert.False(sender.ReturnedOutcome.Success);
        Assert.Null(sender.ReturnedOutcome.Receipt);
        Assert.False(CommandOutcome.ShouldRollback(sender.ReturnedOutcome));
    }

    [Fact]
    public async Task SuccessfulCommandStillReturnsTheExistingReceiptDtoAs200()
    {
        var receipt = new StepUpReceiptResponse("test-receipt", DateTimeOffset.UtcNow.AddMinutes(5));
        var service = new Mock<IStepUpReceiptService>();
        var challengeId = Guid.NewGuid();
        service.Setup(value => value.VerifyAsync(challengeId, It.Is<StepUpVerification>(proof =>
                proof.Method == MfaMethod.BackupCode && proof.Evidence == "proof"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        var sender = new ActualHandlerSender(service.Object);
        var response = Assert.IsType<OkObjectResult>(await Controller(sender).VerifyChallenge(challengeId,
            new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, "proof"), CancellationToken.None));
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Same(receipt, response.Value);
        Assert.NotSame(sender.ReturnedOutcome, response.Value);
    }

    [Fact]
    public async Task ReceiptCasFailureRemainsA409WithoutACommittedDenialOutcome()
    {
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StepUpChallengeUnavailableException("challenge changed"));
        var sender = new ActualHandlerSender(service.Object);
        var result = Assert.IsAssignableFrom<ObjectResult>(await Controller(sender).VerifyChallenge(Guid.NewGuid(),
            new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, "proof"), CancellationToken.None));
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.IsType<ProblemDetails>(result.Value);
        Assert.Null(sender.ReturnedOutcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedVoidCommandsFailWithoutRenderingTheirPayload(bool cancelled)
    {
        var service = new Mock<IStepUpReceiptService>(MockBehavior.Strict);
        var sender = new ActualHandlerSender(service.Object);
        var payload = Guid.NewGuid().ToString("N");
        var error = await Assert.ThrowsAsync<NotSupportedException>(() => ((ISender)sender).Send(
            new UnsupportedVoidCommand(payload), new CancellationToken(cancelled)));
        Assert.Contains(nameof(UnsupportedVoidCommand), error.Message);
        Assert.DoesNotContain(payload, error.Message);
        Assert.Null(sender.ReturnedOutcome);
        service.VerifyNoOtherCalls();
    }

    private sealed record UnsupportedVoidCommand(string SensitiveValue) : IRequest;

    private static StepUpController Controller(ISender sender) => new(sender)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private sealed class ActualHandlerSender(IStepUpReceiptService service) : ISender
    {
        public StepUpVerificationOutcome? ReturnedOutcome { get; private set; }

        Task ISender.Send<TRequest>(TRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException(
                $"Void command {(request is null ? "<null>" : request.GetType().Name)} is outside this test " +
                $"(cancellation requested: {cancellationToken.IsCancellationRequested}).");

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request is VerifyStepUpChallengeCommand command)
            {
                return await Send<StepUpVerificationOutcome>(command, cancellationToken);
            }
            throw new NotSupportedException("Unexpected command type.");
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is not VerifyStepUpChallengeCommand command)
            {
                throw new InvalidOperationException("Unexpected test command.");
            }
            ReturnedOutcome = await new StepUpCommandHandler(service).Handle(command, cancellationToken);
            return (TResponse)(object)ReturnedOutcome;
        }
    }
}
