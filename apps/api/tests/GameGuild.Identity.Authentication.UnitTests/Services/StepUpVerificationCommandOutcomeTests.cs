using GameGuild;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpVerificationCommandOutcomeTests
{
    [Fact]
    public async Task InvalidFactorCommitsARecordedDenialWithoutAReceipt()
    {
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StepUpVerificationFailedException("invalid factor"));
        var result = await new StepUpCommandHandler(service.Object).Handle(Command(), CancellationToken.None);
        Assert.False(result.Success);
        Assert.Null(result.Receipt);
        Assert.True(CommandOutcome.IsFailure(result));
        Assert.False(CommandOutcome.ShouldRollback(result));
        Assert.IsAssignableFrom<ICommitOnFailureOutcome>(result);
    }

    [Fact]
    public async Task ReceiptCasLossStillThrowsSoConsumedFactorCanRollBack()
    {
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StepUpChallengeUnavailableException("receipt compare and swap lost"));
        await Assert.ThrowsAsync<StepUpChallengeUnavailableException>(() =>
            new StepUpCommandHandler(service.Object).Handle(Command(), CancellationToken.None));
    }

    [Fact]
    public async Task InfrastructureFailuresCannotReturnACommitOnFailureOutcome()
    {
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StepUpCommandHandler(service.Object).Handle(Command(), CancellationToken.None));
    }

    [Fact]
    public async Task CancellationCannotReturnACommitOnFailureOutcome()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var failure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new StepUpCommandHandler(service.Object).Handle(Command(), cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    [Fact]
    public async Task SuccessfulVerificationReturnsOnlyTheActualReceipt()
    {
        var receipt = new StepUpReceiptResponse("test-receipt", DateTimeOffset.UtcNow.AddMinutes(5));
        var service = new Mock<IStepUpReceiptService>();
        service.Setup(value => value.VerifyAsync(It.IsAny<Guid>(), It.IsAny<StepUpVerification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        var result = await new StepUpCommandHandler(service.Object).Handle(Command(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Same(receipt, result.Receipt);
        Assert.False(CommandOutcome.ShouldRollback(result));
        Assert.False(result is ICommitOnFailureOutcome);
    }

    private static VerifyStepUpChallengeCommand Command() => new(Guid.NewGuid(), new StepUpVerification(MfaMethod.Totp, "test-proof"));
}
