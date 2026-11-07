using GameGuild.Identity.Authentication;
using Moq;
using Xunit;
using static GameGuild.Identity.Authentication.UnitTests.Services.StepUpReceiptServiceTestHarness;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpVerificationAttemptBudgetTests
{
    [Fact]
    public async Task VerifyAsync_RejectsSixthAttemptBeforeInvokingFactor()
    {
        var actorId = Guid.NewGuid();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(value => value.VerifyMfaForStepUpAsync(actorId, "invalid", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failure("invalid"));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actorId, Guid.NewGuid(), store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        for (var attempt = 0; attempt < 6; attempt++)
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => service.VerifyAsync(
                challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "invalid")));
        }
        mfa.Verify(value => value.VerifyMfaForStepUpAsync(actorId, "invalid", MfaMethod.Totp,
            It.IsAny<CancellationToken>()), Times.Exactly(5));
        Assert.Null(store.Challenges.Single().ReceiptHash);
    }

    [Fact]
    public async Task VerifyAsync_FifthAttemptCanStillProduceBoundReceipt()
    {
        var actorId = Guid.NewGuid();
        var mfa = new Mock<IMfaService>();
        mfa.SetupSequence(value => value.VerifyMfaForStepUpAsync(actorId, "candidate", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failure("invalid"))
            .ReturnsAsync(MfaVerificationResult.Failure("invalid"))
            .ReturnsAsync(MfaVerificationResult.Failure("invalid"))
            .ReturnsAsync(MfaVerificationResult.Failure("invalid"))
            .ReturnsAsync(MfaVerificationResult.Successful("verified"));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actorId, Guid.NewGuid(), store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => service.VerifyAsync(
                challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "candidate")));
        }
        var receipt = await service.VerifyAsync(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "candidate"));
        await service.ConsumeAsync(Binding, receipt.Receipt);
        Assert.Equal(Now, store.Challenges.Single().ConsumedAt);
        await Assert.ThrowsAsync<StepUpReceiptInvalidException>(() => service.ConsumeAsync(Binding, receipt.Receipt));
        mfa.Verify(value => value.VerifyMfaForStepUpAsync(actorId, "candidate", MfaMethod.Totp,
            It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task VerifyAsync_ConcurrentRequestsCannotAllInvokeFactor()
    {
        var actorId = Guid.NewGuid();
        var pending = new TaskCompletionSource<MfaVerificationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mfa = new Mock<IMfaService>();
        mfa.Setup(value => value.VerifyMfaForStepUpAsync(actorId, "invalid", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actorId, Guid.NewGuid(), store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        var attempts = Enumerable.Range(0, 32).Select(_ => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "invalid"))).ToArray();
        pending.SetResult(MfaVerificationResult.Failure("invalid"));
        foreach (var attempt in attempts)
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => attempt);
        }
        mfa.Verify(value => value.VerifyMfaForStepUpAsync(actorId, "invalid", MfaMethod.Totp,
            It.IsAny<CancellationToken>()), Times.Exactly(5));
        Assert.Null(store.Challenges.Single().ReceiptHash);
    }
}
