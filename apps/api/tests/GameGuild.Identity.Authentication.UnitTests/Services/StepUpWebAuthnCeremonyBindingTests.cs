using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Moq;
using Xunit;
using static GameGuild.Identity.Authentication.UnitTests.Services.StepUpReceiptServiceTestHarness;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpWebAuthnCeremonyBindingTests
{
    [Fact]
    public async Task VerificationWithoutStepUpOptionsCannotUseAnOtherwiseSuccessfulAssertion()
    {
        var actor = Guid.NewGuid();
        var provider = Provider(actor, RandomNumberGenerator.GetBytes(32));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, Assertion(RandomNumberGenerator.GetBytes(32)))));

        Assert.Null(store.Challenges.Single().ReceiptHash);
        VerifyNoCryptographicVerification(provider);
    }

    [Fact]
    public async Task CeremonyForOneChallengeCannotAuthorizeAnotherOperation()
    {
        var actor = Guid.NewGuid();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var provider = Provider(actor, nonce);
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var first = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(first.ChallengeId);
        var other = await service.CreateChallengeAsync(new StepUpOperationBinding(
            "identity.mfa.disable", "current-account", new string('b', 64)));

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            other.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, Assertion(nonce))));

        Assert.All(store.Challenges, challenge => Assert.Null(challenge.ReceiptHash));
        VerifyNoCryptographicVerification(provider);
    }

    [Fact]
    public async Task BoundCeremonyCanIssueOneReceiptForItsExactOperation()
    {
        var actor = Guid.NewGuid();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var provider = Provider(actor, nonce);
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);

        var receipt = await service.VerifyAsync(challenge.ChallengeId,
            new StepUpVerification(MfaMethod.WebAuthn, Assertion(nonce)));
        await service.ConsumeAsync(Binding, receipt.Receipt);

        await Assert.ThrowsAsync<StepUpReceiptInvalidException>(() => service.ConsumeAsync(Binding, receipt.Receipt));
        provider.Verify(value => value.CompleteAuthenticationAsync(
            It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReissuedOptionsInvalidateTheEarlierCeremony()
    {
        var actor = Guid.NewGuid();
        var oldNonce = RandomNumberGenerator.GetBytes(32);
        var provider = Provider(actor, oldNonce);
        provider.SetupSequence(value => value.BeginAuthenticationAsync(null, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Options(oldNonce))
            .ReturnsAsync(Options(RandomNumberGenerator.GetBytes(32)));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, Assertion(oldNonce))));

        Assert.Null(store.Challenges.Single().ReceiptHash);
        VerifyNoCryptographicVerification(provider);
    }

    [Fact]
    public async Task SuccessfulProviderResponseWithoutOptionsCannotBeginAStepUpCeremony()
    {
        var actor = Guid.NewGuid();
        var provider = Provider(actor, RandomNumberGenerator.GetBytes(32));
        provider.Setup(value => value.BeginAuthenticationAsync(null, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnAuthenticationOptionsResult { Success = true });
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.BeginWebAuthnAsync(challenge.ChallengeId));
        Assert.Null(store.Challenges.Single().ReceiptHash);
    }

    [Fact]
    public async Task MalformedClientDataCannotReachCryptographicVerification()
    {
        var actor = Guid.NewGuid();
        var provider = Provider(actor, RandomNumberGenerator.GetBytes(32));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, "{}")));
        VerifyNoCryptographicVerification(provider);
    }

    [Fact]
    public async Task BoundNonceDoesNotReplaceTheCredentialOwnershipCheck()
    {
        var actor = Guid.NewGuid();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var provider = Provider(actor, nonce);
        provider.Setup(value => value.CompleteAuthenticationAsync(
                It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnAuthenticationResult { Success = true, UserId = Guid.NewGuid() });
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);

        await Assert.ThrowsAsync<StepUpVerificationFailedException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, Assertion(nonce))));
        Assert.Null(store.Challenges.Single().ReceiptHash);
    }

    [Fact]
    public async Task ReissuingOptionsDuringVerificationPreventsAStaleReceiptCommit()
    {
        var actor = Guid.NewGuid();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var provider = Provider(actor, nonce);
        provider.SetupSequence(value => value.BeginAuthenticationAsync(null, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Options(nonce))
            .ReturnsAsync(Options(RandomNumberGenerator.GetBytes(32)));
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), actor, Guid.NewGuid(), store, webAuthn: provider.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        await service.BeginWebAuthnAsync(challenge.ChallengeId);
        provider.Setup(value => value.CompleteAuthenticationAsync(
                It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await service.BeginWebAuthnAsync(challenge.ChallengeId);
                return new WebAuthnAuthenticationResult { Success = true, UserId = actor };
            });

        await Assert.ThrowsAsync<StepUpChallengeUnavailableException>(() => service.VerifyAsync(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.WebAuthn, Assertion(nonce))));
        Assert.Null(store.Challenges.Single().ReceiptHash);
    }

    [Fact]
    public async Task LegacyWebAuthnReceiptWithoutABoundCeremonyFailsClosed()
    {
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), store);
        await service.CreateChallengeAsync(Binding);
        var receipt = Base64Url(RandomNumberGenerator.GetBytes(32));
        var row = store.Challenges.Single();
        row.VerifiedAt = Now;
        row.VerificationMethod = MfaMethod.WebAuthn;
        row.ReceiptHash = Sha256(receipt);

        await Assert.ThrowsAsync<StepUpReceiptInvalidException>(() => service.ConsumeAsync(Binding, receipt));
        Assert.Null(row.ConsumedAt);
    }

    private static Mock<IWebAuthnAuthenticationService> Provider(Guid actor, byte[] nonce)
    {
        var provider = new Mock<IWebAuthnAuthenticationService>();
        provider.Setup(value => value.BeginAuthenticationAsync(null, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Options(nonce));
        provider.Setup(value => value.CompleteAuthenticationAsync(
                It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnAuthenticationResult { Success = true, UserId = actor });
        return provider;
    }

    private static WebAuthnAuthenticationOptionsResult Options(byte[] nonce) => new()
    {
        Success = true,
        Options = AssertionOptions.FromJson(JsonSerializer.Serialize(new
        {
            challenge = Base64Url(nonce), timeout = 30000, allowCredentials = Array.Empty<object>(), userVerification = "preferred"
        }))
    };

    private static string Assertion(byte[] nonce) => JsonSerializer.Serialize(new
    {
        response = new
        {
            clientDataJSON = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                type = "webauthn.get", challenge = Base64Url(nonce), origin = "https://auth.example.test"
            })))
        }
    });

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void VerifyNoCryptographicVerification(Mock<IWebAuthnAuthenticationService> provider) =>
        provider.Verify(value => value.CompleteAuthenticationAsync(
            It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()), Times.Never);
}
