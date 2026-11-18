using System.Security.Claims;
using GameGuild.CQRS;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpRequirementServiceTests
{
    [Fact]
    public async Task MissingReceiptProducesStructuredStepupAndNeverGrantsTrust()
    {
        var sender = new Mock<ISender>();
        var challenge = new StepUpChallengeResponse(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(5));
        sender.Setup(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        var controller = Controller(sender);
        var action = await controller.TrustCurrentDevice(new TrustDeviceRequest { DeviceName = "  Work device  " }, default);
        var result = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        var response = Assert.IsType<StepUpAuthenticationRequiredResponse>(result.Value);
        Assert.True(response.StepUpRequired);
        Assert.Equal(StatusCodes.Status403Forbidden, response.Status);
        Assert.Equal(challenge.ChallengeId, response.ChallengeId);
        Assert.Equal(SessionMutationCommandHandler.TrustDeviceStepUpBinding("  Work device  "), response.Operation);
        Assert.Equal(RiskLevel.High, response.RiskLevel);
        Assert.Equal(challenge.ChallengeId.ToString("D"), response.StepUpToken);
        Assert.Equal(challenge.ExpiresAt.UtcDateTime, response.ExpiresAt);
        sender.Verify(x => x.Send(It.Is<CreateStepUpChallengeCommand>(c =>
            c.Binding == SessionMutationCommandHandler.TrustDeviceStepUpBinding("  Work device  ")), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(x => x.Send(It.IsAny<TrustDeviceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvidedReceiptPreservesExistingGuardedCommandPath()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<TrustDeviceCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = Controller(sender);
        Assert.IsType<OkObjectResult>(await controller.TrustCurrentDevice(new TrustDeviceRequest { DeviceName = "Work device", StepUpReceipt = "valid-test-receipt" }, default));
        sender.Verify(x => x.Send(It.Is<TrustDeviceCommand>(c => c.StepUpReceipt == "valid-test-receipt" && c.DeviceName == "Work device"), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingSessionDoesNotCreateChallengeOrMutateTrust()
    {
        var sender = new Mock<ISender>();
        var controller = Controller(sender, includeSession: false);
        Assert.IsType<BadRequestObjectResult>(await controller.TrustCurrentDevice(new TrustDeviceRequest { DeviceName = "Work device" }, default));
        sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        sender.Verify(x => x.Send(It.IsAny<TrustDeviceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequirementBindsExactOperationExpiryAndEnrolledSupportedMethods()
    {
        var f = new RequirementFixture();
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration
        { IsEnabled = true, IsSetupComplete = true, TotpSecretKey = "synthetic-encrypted-marker", BackupCodes = "hash-one,hash-two", SmsPhoneNumber = "+12025550123", EmailMfaAddress = "synthetic@example.test" });
        f.WebAuthn.Setup(x => x.HasActiveCredentialsAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var response = await f.Service.CreateAsync(f.Operation);
        Assert.Equal(["Totp", "BackupCode", "WebAuthn"], response.AvailableMethods);
        Assert.Equal(f.Operation, response.Operation); Assert.Equal(f.Challenge.ChallengeId, response.ChallengeId);
        Assert.Equal(f.Challenge.ExpiresAt.UtcDateTime, response.ExpiresAt); Assert.True(response.StepUpRequired);
        f.Sender.Verify(x => x.Send(It.Is<CreateStepUpChallengeCommand>(c => c.Binding == f.Operation), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisabledMfaAndRevokedPasskeysAreNeverAdvertised()
    {
        var f = new RequirementFixture();
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration
        { IsEnabled = false, IsSetupComplete = true, TotpSecretKey = "synthetic-encrypted-marker", BackupCodes = "hash-one,hash-two" });
        var response = await f.Service.CreateAsync(f.Operation);
        Assert.Empty(response.AvailableMethods); Assert.True(response.StepUpRequired);
    }

    [Fact]
    public async Task ExhaustedBackupAndUnsupportedEmailSmsNeverAdvertised()
    {
        var f = new RequirementFixture();
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration
        { IsEnabled = true, IsSetupComplete = true, BackupCodes = "set-v1$2$", SmsPhoneNumber = "+12025550123", EmailMfaAddress = "synthetic@example.test" });
        Assert.Empty((await f.Service.CreateAsync(f.Operation)).AvailableMethods);
    }

    [Theory]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    public async Task AvailableFactorsRequireConfirmedEnrollmentAndRespectGlobalMfaConfiguration(
        bool enabled, bool complete, bool globallyEnabled, bool activePasskey)
    {
        var f = new RequirementFixture();
        f.Options.Enabled = globallyEnabled;
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration
        {
            IsEnabled = enabled, IsSetupComplete = complete,
            TotpSecretKey = "synthetic-encrypted-marker", BackupCodes = "synthetic-legacy-hash"
        });
        f.WebAuthn.Setup(x => x.HasActiveCredentialsAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(activePasskey);
        var expected = new List<string>();
        if (globallyEnabled && enabled && complete)
        {
            expected.Add("Totp");
            expected.Add("BackupCode");
        }
        if (activePasskey)
        {
            expected.Add("WebAuthn");
        }
        Assert.Equal(expected, (await f.Service.CreateAsync(f.Operation)).AvailableMethods);
        f.Mfa.Verify(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>()), globallyEnabled ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task MissingMfaConfigurationDoesNotAdvertiseAnUnenrolledFactor()
    {
        var f = new RequirementFixture();
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync((UserMfaConfiguration?)null);
        Assert.Empty((await f.Service.CreateAsync(f.Operation)).AvailableMethods);
    }

    [Fact]
    public async Task CorruptBackupMetadataDoesNotCreateAChallengeOrHideTheStorageFailure()
    {
        var f = new RequirementFixture();
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration
        { IsEnabled = true, IsSetupComplete = true, BackupCodes = "set-v1$invalid$" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateAsync(f.Operation));
        f.Sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MfaStoreFaultPropagatesWithoutCreatingChallenge()
    {
        var f = new RequirementFixture(); var fault = new InvalidOperationException("configuration store unavailable");
        f.Mfa.Setup(x => x.GetByUserIdAsync(f.ActorId, It.IsAny<CancellationToken>())).ThrowsAsync(fault);
        Assert.Same(fault, await Record.ExceptionAsync(() => f.Service.CreateAsync(f.Operation)));
        f.Sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasskeyStoreFaultPropagatesWithoutCreatingChallenge()
    {
        var f = new RequirementFixture(); var fault = new InvalidOperationException("credential store unavailable");
        f.WebAuthn.Setup(x => x.HasActiveCredentialsAsync(f.ActorId, It.IsAny<CancellationToken>())).ThrowsAsync(fault);
        Assert.Same(fault, await Record.ExceptionAsync(() => f.Service.CreateAsync(f.Operation)));
        f.Sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChallengeWriteFaultIsNotReplacedWithFabricatedToken()
    {
        var f = new RequirementFixture(); var fault = new InvalidOperationException("challenge transaction unavailable");
        f.Sender.Setup(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(fault);
        Assert.Same(fault, await Record.ExceptionAsync(() => f.Service.CreateAsync(f.Operation)));
    }

    [Fact]
    public async Task CancelledRequestDoesNotReadFactorsOrCreateChallenge()
    {
        var f = new RequirementFixture(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Service.CreateAsync(f.Operation, cts.Token));
        f.Mfa.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        f.WebAuthn.Verify(x => x.HasActiveCredentialsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        f.Sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("no-tenant")]
    [InlineData("no-session")]
    [InlineData("empty-session")]
    public async Task InvalidActorContextDoesNotIssueChallenge(string context)
    {
        var f = new RequirementFixture();
        var builder = ActorContextBuilder.ForUser(f.ActorId);
        if (context != "no-tenant")
        {
            builder.WithTenantId(Guid.NewGuid());
        }
        if (context != "no-session")
        {
            builder.WithAttribute("session_id", context == "empty-session" ? Guid.Empty.ToString() : Guid.NewGuid().ToString());
        }
        f.Actor.SetupGet(x => x.ActorContext).Returns(context == "anonymous" ? ActorContext.Anonymous : builder.Build());
        await Assert.ThrowsAsync<StepUpContextUnavailableException>(() => f.Service.CreateAsync(f.Operation));
        f.Mfa.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        f.Sender.Verify(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TrustedDevicesController Controller(Mock<ISender> sender) => Controller(sender, true);

    private static TrustedDevicesController Controller(Mock<ISender> sender, bool includeSession)
    {
        var actorId = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, actorId.ToString()) };
        if (includeSession)
        {
            claims.Add(new Claim("session_id", Guid.NewGuid().ToString()));
        }
        return new TrustedDevicesController(Mock.Of<ISessionManagementService>(), sender.Object, new RequirementFixture(sender, actorId).Service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "probe")) } }
        };
    }

    private sealed class RequirementFixture
    {
        public Guid ActorId { get; }
        public Mock<ISender> Sender { get; }
        public Mock<IActorContextAccessor> Actor { get; } = new();
        public Mock<IUserMfaConfigurationRepository> Mfa { get; } = new();
        public MfaOptions Options { get; } = new() { Enabled = true };
        public Mock<IWebAuthnCredentialRepository> WebAuthn { get; } = new();
        public StepUpOperationBinding Operation { get; } = SessionMutationCommandHandler.TrustDeviceStepUpBinding("Work device");
        public StepUpChallengeResponse Challenge { get; } = new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(5));
        public StepUpRequirementService Service { get; }
        public RequirementFixture() : this(null, null) { }

        public RequirementFixture(Mock<ISender>? sender, Guid? actorId)
        {
            ActorId = actorId ?? Guid.NewGuid(); Sender = sender ?? new Mock<ISender>();
            Actor.SetupGet(x => x.ActorContext).Returns(ActorContextBuilder.ForUser(ActorId).WithTenantId(Guid.NewGuid()).WithAttribute("session_id", Guid.NewGuid().ToString()).Build());
            Mfa.Setup(x => x.GetByUserIdAsync(ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserMfaConfiguration { IsEnabled = true, IsSetupComplete = true, TotpSecretKey = "synthetic-encrypted-marker" });
            WebAuthn.Setup(x => x.HasActiveCredentialsAsync(ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            if (sender is null)
            {
                Sender.Setup(x => x.Send(It.IsAny<CreateStepUpChallengeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Challenge);
            }
            Service = new StepUpRequirementService(Sender.Object, Actor.Object, Mfa.Object, WebAuthn.Object, Options);
        }
    }
}
