using Moq;
using Xunit;
using static GameGuild.Identity.Authentication.UnitTests.Services.StepUpReceiptServiceTestHarness;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpVerifiedFactorPersistenceTests
{
    [Fact]
    public async Task FailedTotpCannotIssueReceiptEvenWhenWebAuthnIsBound()
    {
        await AssertFailedMfaCannotIssueReceipt(MfaMethod.Totp);
    }

    [Fact]
    public async Task FailedBackupCodeCannotIssueReceiptEvenWhenWebAuthnIsBound()
    {
        await AssertFailedMfaCannotIssueReceipt(MfaMethod.BackupCode);
    }

    [Fact]
    public async Task SuccessfulTotpPersistsOnlyItsVerifiedFactor()
    {
        await AssertSuccessfulMfaUsesItsFactor(MfaMethod.Totp);
    }

    [Fact]
    public async Task SuccessfulBackupCodePersistsOnlyItsVerifiedFactor()
    {
        await AssertSuccessfulMfaUsesItsFactor(MfaMethod.BackupCode);
    }

    [Fact]
    public async Task UnsupportedMethodsCannotCallAnyVerifierOrIssueReceipt()
    {
        var actor = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>(MockBehavior.Strict);
        var webAuthn = new Mock<IWebAuthnAuthenticationService>(MockBehavior.Strict);
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, mfa.Object, webAuthn.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        var unsupported = Enum.GetValues<MfaMethod>()
            .Where(method => method is not (MfaMethod.Totp or MfaMethod.BackupCode or MfaMethod.WebAuthn))
            .Append((MfaMethod)int.MaxValue);

        foreach (var method in unsupported)
        {
            await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
                challenge.ChallengeId, new StepUpVerification(method, "untrusted-proof")));
        }

        Assert.Null(store.Challenges.Single().VerifiedAt);
        Assert.Null(store.Challenges.Single().ReceiptHash);
        mfa.VerifyNoOtherCalls();
        webAuthn.VerifyNoOtherCalls();
    }

    private static async Task AssertFailedMfaCannotIssueReceipt(MfaMethod method)
    {
        var actor = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(value => value.VerifyMfaAsync(actor, "invalid-proof", method, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failure("Invalid proof"));
        var webAuthn = new Mock<IWebAuthnAuthenticationService>(MockBehavior.Strict);
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, mfa.Object, webAuthn.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        store.Challenges.Single().WebAuthnChallengeHash = new string('a', 64);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(method, "invalid-proof")));

        Assert.Null(store.Challenges.Single().VerifiedAt);
        Assert.Null(store.Challenges.Single().ReceiptHash);
        mfa.Verify(value => value.VerifyMfaAsync(actor, "invalid-proof", method, It.IsAny<CancellationToken>()), Times.Once);
        webAuthn.VerifyNoOtherCalls();
    }

    private static async Task AssertSuccessfulMfaUsesItsFactor(MfaMethod method)
    {
        var actor = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(value => value.VerifyMfaAsync(actor, "valid-proof", method, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Successful("Verified"));
        var webAuthn = new Mock<IWebAuthnAuthenticationService>(MockBehavior.Strict);
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, mfa.Object, webAuthn.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        store.Challenges.Single().WebAuthnChallengeHash = new string('a', 64);

        var receipt = await service.VerifyAsync(challenge.ChallengeId, new StepUpVerification(method, "valid-proof"));

        Assert.Equal(method, store.Challenges.Single().VerificationMethod);
        Assert.Equal(Sha256(receipt.Receipt), store.Challenges.Single().ReceiptHash);
        await service.ConsumeAsync(Binding, receipt.Receipt);
        webAuthn.VerifyNoOtherCalls();
    }
}
