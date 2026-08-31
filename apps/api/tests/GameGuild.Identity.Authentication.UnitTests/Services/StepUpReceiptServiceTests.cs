using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;
using static GameGuild.Identity.Authentication.UnitTests.Services.StepUpReceiptServiceTestHarness;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpReceiptServiceTests
{
    [Fact]
    public async Task CreateChallengeAsync_DerivesSecurityScopeFromActorContext()
    {
        // Given an authenticated actor with tenant and session scope.
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var service = CreateService(tenantId, actorId, sessionId, store);

        // When the actor creates a challenge for a protected operation.
        var result = await service.CreateChallengeAsync(Binding);

        // Then the durable challenge is scoped by server-derived identity data.
        store.Challenges.Should().ContainSingle();
        var challenge = store.Challenges.Single();
        challenge.TenantId.Should().Be(tenantId);
        challenge.ActorId.Should().Be(actorId);
        challenge.SessionId.Should().Be(sessionId);
        challenge.OperationType.Should().Be(Binding.OperationType);
        challenge.TargetReference.Should().Be(Binding.TargetReference);
        challenge.PayloadHash.Should().Be(Binding.PayloadHash);
        challenge.ExpiresAt.Should().Be(Now.AddMinutes(5));
        result.ChallengeId.Should().Be(challenge.Id);
    }

    [Fact]
    public async Task CreateChallengeAsync_RejectsActorWithoutTenantOrSession()
    {
        // Given an actor whose request is not tenant and session scoped.
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).Build());
        var service = CreateService(accessor, new RecordingStepUpChallengeStore());

        // When challenge creation is attempted, then it fails closed.
        var act = () => service.CreateChallengeAsync(Binding);
        await act.Should().ThrowAsync<StepUpContextUnavailableException>();
    }

    [Fact]
    public async Task VerifyAsync_IssuesOpaqueReceiptAfterValidTotpWithoutPersistingSecret()
    {
        // Given an active challenge and a successful TOTP verification.
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(service => service.VerifyMfaAsync(actorId, "123456", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Successful("verified"));
        var service = CreateService(tenantId, actorId, sessionId, store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);

        // When the actor verifies the challenge.
        var verification = await service.VerifyAsync(
            challenge.ChallengeId,
            new StepUpVerification(MfaMethod.Totp, "123456"));

        // Then only a hash of the opaque receipt is durable.
        verification.Receipt.Should().NotBeNullOrWhiteSpace();
        store.Challenges.Single().ReceiptHash.Should().Be(Sha256(verification.Receipt));
        store.Challenges.Single().ReceiptHash.Should().NotContain(verification.Receipt);
        store.Challenges.Single().VerifiedAt.Should().Be(Now);
        store.Challenges.Single().VerificationMethod.Should().Be(MfaMethod.Totp);
    }

    [Fact]
    public async Task VerifyAsync_RejectsWebAuthnCredentialOwnedByAnotherActor()
    {
        // Given a challenge and a valid WebAuthn assertion for a different actor.
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var webAuthn = new Mock<IWebAuthnAuthenticationService>();
        webAuthn.Setup(service => service.CompleteAuthenticationAsync(
                "assertion", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnAuthenticationResult { Success = true, UserId = Guid.NewGuid() });
        var service = CreateService(tenantId, actorId, sessionId, store, webAuthn: webAuthn.Object);
        var challenge = await service.CreateChallengeAsync(Binding);

        // When the assertion is submitted, then no receipt is issued.
        var act = () => service.VerifyAsync(
            challenge.ChallengeId,
            new StepUpVerification(MfaMethod.WebAuthn, "assertion"));
        await act.Should().ThrowAsync<StepUpVerificationFailedException>();
        store.Challenges.Single().VerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task ConsumeAsync_AllowsExactlyOneMatchingOperation()
    {
        // Given a verified receipt bound to one operation and security scope.
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(service => service.VerifyMfaAsync(actorId, "123456", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Successful("verified"));
        var service = CreateService(tenantId, actorId, sessionId, store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        var verification = await service.VerifyAsync(
            challenge.ChallengeId,
            new StepUpVerification(MfaMethod.Totp, "123456"));

        // When it is consumed once, then replay of the same receipt fails.
        await service.ConsumeAsync(Binding, verification.Receipt);
        var replay = () => service.ConsumeAsync(Binding, verification.Receipt);
        await replay.Should().ThrowAsync<StepUpReceiptInvalidException>();
        store.Challenges.Single().ConsumedAt.Should().Be(Now);
    }

    [Fact]
    public async Task ConsumeAsync_RejectsReceiptForDifferentPayload()
    {
        // Given a verified receipt for one canonical payload.
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new RecordingStepUpChallengeStore();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(service => service.VerifyMfaAsync(actorId, "123456", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Successful("verified"));
        var service = CreateService(tenantId, actorId, sessionId, store, mfa.Object);
        var challenge = await service.CreateChallengeAsync(Binding);
        var verification = await service.VerifyAsync(
            challenge.ChallengeId,
            new StepUpVerification(MfaMethod.Totp, "123456"));

        // When another payload attempts consumption, then it fails closed.
        var otherBinding = new StepUpOperationBinding(
            Binding.OperationType,
            Binding.TargetReference,
            new string('b', 64));
        var act = () => service.ConsumeAsync(otherBinding, verification.Receipt);
        await act.Should().ThrowAsync<StepUpReceiptInvalidException>();
        store.Challenges.Single().ConsumedAt.Should().BeNull();
    }

}
