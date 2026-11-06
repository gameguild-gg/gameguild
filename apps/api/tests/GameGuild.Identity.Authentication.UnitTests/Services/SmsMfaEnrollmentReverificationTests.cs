using System.Security.Cryptography;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SmsMfaEnrollmentReverificationTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("wrong")]
    public async Task MissingOrFailedPrimaryProofCannotReachEnrollment(string fault)
    {
        var user = Guid.NewGuid();
        var password = fault == "missing" ? string.Empty : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var provider = new Mock<ISmsMfaService>(MockBehavior.Strict);
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        proof.Setup(port => port.VerifyPasswordAsync(user, password, "sms-enrollment", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await new SmsPushMfaCommandHandler(provider.Object, Mock.Of<IPushMfaService>(), proof.Object)
            .Handle(new StartSmsMfaEnrollmentCommand(user, "+15555550123", password), CancellationToken.None);
        Assert.False(result.Success);
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingVerifierFailsClosedBeforeEnrollment()
    {
        var provider = new Mock<ISmsMfaService>(MockBehavior.Strict);
        var result = await new SmsPushMfaCommandHandler(provider.Object, Mock.Of<IPushMfaService>())
            .Handle(new StartSmsMfaEnrollmentCommand(Guid.NewGuid(), "+15555550123", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))), CancellationToken.None);
        Assert.False(result.Success);
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulPrimaryProofReachesTheOwnersEnrollmentExactlyOnce()
    {
        var user = Guid.NewGuid();
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var provider = new Mock<ISmsMfaService>(MockBehavior.Strict);
        provider.Setup(port => port.StartEnrollmentAsync(user, "+15555550123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsMfaChallengeResult { Success = true, PhoneNumberMasked = "***0123", ExpiresInSeconds = 600 });
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        proof.Setup(port => port.VerifyPasswordAsync(user, password, "sms-enrollment", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True((await new SmsPushMfaCommandHandler(provider.Object, Mock.Of<IPushMfaService>(), proof.Object)
            .Handle(new StartSmsMfaEnrollmentCommand(user, "+15555550123", password), CancellationToken.None)).Success);
        provider.Verify(port => port.StartEnrollmentAsync(user, "+15555550123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CommandDiagnosticsNeverPrintPasswordOrPhone()
    {
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var text = new StartSmsMfaEnrollmentCommand(Guid.NewGuid(), "+15555550123", password).ToString();
        Assert.DoesNotContain(password, text, StringComparison.Ordinal);
        Assert.DoesNotContain("+15555550123", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationBeforeVerificationCannotReachEitherPort()
    {
        var provider = new Mock<ISmsMfaService>(MockBehavior.Strict);
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SmsPushMfaCommandHandler(provider.Object, Mock.Of<IPushMfaService>(), proof.Object)
            .Handle(new StartSmsMfaEnrollmentCommand(Guid.NewGuid(), "+15555550123", string.Empty), cancellation.Token));
        provider.VerifyNoOtherCalls();
        proof.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifierInfrastructureFailureCannotStartEnrollment()
    {
        var provider = new Mock<ISmsMfaService>(MockBehavior.Strict);
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        proof.Setup(port => port.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), "sms-enrollment", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("controlled verifier outage"));
        await Assert.ThrowsAsync<IOException>(() => new SmsPushMfaCommandHandler(provider.Object, Mock.Of<IPushMfaService>(), proof.Object)
            .Handle(new StartSmsMfaEnrollmentCommand(Guid.NewGuid(), "+15555550123", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))), CancellationToken.None));
        provider.VerifyNoOtherCalls();
    }
}
