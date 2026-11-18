using System.Security.Cryptography;
using System.Text;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

public sealed class TrustedDeviceSessionCommandTests
{
    private static (Mock<ISessionManagementService> Service, Mock<IStepUpReceiptService> Receipts) CreateStrictMocks()
        => (new Mock<ISessionManagementService>(MockBehavior.Strict), new Mock<IStepUpReceiptService>(MockBehavior.Strict));

    private static Task<bool> Handle(
        SessionMutationCommandHandler handler,
        TrustDeviceCommand command,
        CancellationToken cancellationToken = default) =>
        handler.Handle(command, cancellationToken);

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign-owner")]
    [InlineData("wrong-session")]
    [InlineData("inactive")]
    [InlineData("expired")]
    [InlineData("termination-marker")]
    [InlineData("empty-fingerprint")]
    [InlineData("whitespace-fingerprint")]
    public async Task TrustRejectsAnUnavailableOrUnownedSessionWithoutCallingTheTrustMutation(string boundary)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        UserSession? session = new()
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device"
        };
        switch (boundary)
        {
            case "missing": session = null; break;
            case "foreign-owner": session.UserId = Guid.NewGuid(); break;
            case "wrong-session": session.Id = Guid.NewGuid(); break;
            case "inactive": session.IsActive = false; break;
            case "expired": session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); break;
            case "termination-marker": session.TerminatedAt = DateTime.UtcNow; break;
            case "empty-fingerprint": session.DeviceFingerprint = string.Empty; break;
            case "whitespace-fingerprint": session.DeviceFingerprint = " "; break;
            default: throw new ArgumentOutOfRangeException(nameof(boundary), boundary, "Unknown session trust boundary.");
        }
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, CancellationToken.None)).ReturnsAsync(session);
        var handler = new SessionMutationCommandHandler(service.Object, receipts.Object);
        var result = await Handle(handler, new TrustDeviceCommand(userId, sessionId, "Browser", "step-up-receipt"));
        Assert.False(result);
        service.Verify(s => s.GetSessionAsync(sessionId, CancellationToken.None), Times.Once);
        service.VerifyNoOtherCalls();
        receipts.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyPrincipalIdentifiersAreRejectedBeforeAnySessionRead(bool emptyUser)
    {
        var (service, receipts) = CreateStrictMocks();
        var handler = new SessionMutationCommandHandler(service.Object, receipts.Object);
        var result = await Handle(handler, new TrustDeviceCommand(
            emptyUser ? Guid.Empty : Guid.NewGuid(),
            emptyUser ? Guid.NewGuid() : Guid.Empty, "Browser", "step-up-receipt"));
        Assert.False(result);
        service.VerifyNoOtherCalls();
        receipts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TrustWithoutAStepUpReceiptRaisesTheStructuredStepUpTrigger()
    {
        // Fresh verification gate (issue #1144): an otherwise valid session must not be able to
        // grant device trust without a just-verified MFA receipt. Issue #1191: the refusal is a
        // StepUpRequiredException carrying the exact binding, so the controller can answer 423
        // with the challenge instructions instead of an ambiguous false.
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, CancellationToken.None)).ReturnsAsync(new UserSession
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device"
        });

        var requirement = await Assert.ThrowsAsync<StepUpRequiredException>(() =>
            Handle(new SessionMutationCommandHandler(service.Object, receipts.Object),
                new TrustDeviceCommand(userId, sessionId, "Browser", null)));

        Assert.Equal("trust-device", requirement.OperationType);
        Assert.Equal("current-session", requirement.TargetReference);
        Assert.Equal(SessionMutationCommandHandler.TrustDeviceStepUpBinding("Browser").PayloadHash, requirement.PayloadHash);
        service.Verify(s => s.GetSessionAsync(sessionId, CancellationToken.None), Times.Once);
        service.VerifyNoOtherCalls();
        receipts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TrustWithAnInvalidStepUpReceiptRaisesTheStructuredStepUpTrigger()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, CancellationToken.None)).ReturnsAsync(new UserSession
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device"
        });
        receipts.Setup(r => r.ConsumeAsync(
                It.Is<StepUpOperationBinding>(binding => binding.OperationType == "trust-device"),
                "forged-receipt",
                CancellationToken.None))
            .ThrowsAsync(new StepUpReceiptInvalidException("receipt is invalid"));

        var requirement = await Assert.ThrowsAsync<StepUpRequiredException>(() =>
            Handle(new SessionMutationCommandHandler(service.Object, receipts.Object),
                new TrustDeviceCommand(userId, sessionId, "Browser", "forged-receipt")));

        Assert.Equal("trust-device", requirement.OperationType);
        Assert.Equal("current-session", requirement.TargetReference);
        Assert.NotNull(requirement.InnerException);
        Assert.IsType<StepUpReceiptInvalidException>(requirement.InnerException);
        service.Verify(s => s.GetSessionAsync(sessionId, CancellationToken.None), Times.Once);
        service.Verify(s => s.TrustDeviceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrustConsumesTheReceiptBoundToTheDeviceName()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, CancellationToken.None)).ReturnsAsync(new UserSession
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device"
        });
        receipts.Setup(r => r.ConsumeAsync(
                SessionMutationCommandHandler.TrustDeviceStepUpBinding("Browser"),
                "step-up-receipt",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.TrustDeviceAsync(userId, "persisted-device", "Browser", null, null, CancellationToken.None))
            .ReturnsAsync(true);

        var result = await Handle(new SessionMutationCommandHandler(service.Object, receipts.Object),
            new TrustDeviceCommand(userId, sessionId, "Browser", "step-up-receipt"));

        Assert.True(result);
        service.VerifyAll();
        receipts.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidBindingUsesOnlyPersistedFingerprintAndPropagatesCancellationAndMutationOutcome(bool outcome)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, cancellation.Token)).ReturnsAsync(new UserSession
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device"
        });
        receipts.Setup(r => r.ConsumeAsync(
                It.IsAny<StepUpOperationBinding>(), "step-up-receipt", cancellation.Token))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.TrustDeviceAsync(userId, "persisted-device", "Browser", null, null, cancellation.Token)).ReturnsAsync(outcome);
        var result = await Handle(new SessionMutationCommandHandler(service.Object, receipts.Object),
            new TrustDeviceCommand(userId, sessionId, "Browser", "step-up-receipt"), cancellation.Token);
        Assert.Equal(outcome, result);
        service.VerifyAll();
        service.VerifyNoOtherCalls();
        receipts.VerifyAll();
    }

    [Fact]
    public async Task TrustCarriesTheTrustedSessionDeviceMetadataAndIpIntoTheTrustMutation()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const string deviceInfo = """{"browser":"Firefox","operatingSystem":"Windows"}""";
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.GetSessionAsync(sessionId, CancellationToken.None)).ReturnsAsync(new UserSession
        {
            Id = sessionId, UserId = userId, IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1), DeviceFingerprint = "persisted-device",
            DeviceInfo = deviceInfo,
            IpAddress = "203.0.113.7"
        });
        receipts.Setup(r => r.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), "step-up-receipt", CancellationToken.None))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.TrustDeviceAsync(userId, "persisted-device", "Browser", deviceInfo, "203.0.113.7", CancellationToken.None))
            .ReturnsAsync(true);
        var result = await Handle(new SessionMutationCommandHandler(service.Object, receipts.Object),
            new TrustDeviceCommand(userId, sessionId, "Browser", "step-up-receipt"));
        Assert.True(result);
        service.VerifyAll();
        service.VerifyNoOtherCalls();
        receipts.VerifyAll();
    }

    [Fact]
    public void TrustDeviceStepUpBinding_HashesTheTrimmedDeviceName()
    {
        var binding = SessionMutationCommandHandler.TrustDeviceStepUpBinding("  Browser  ");

        Assert.Equal("trust-device", binding.OperationType);
        Assert.Equal("current-session", binding.TargetReference);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Browser"))).ToLowerInvariant(),
            binding.PayloadHash);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RenameTrustedDeviceDelegatesToTheServiceWithTheActingPrincipal(bool outcome)
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var (service, receipts) = CreateStrictMocks();
        service.Setup(s => s.RenameTrustedDeviceAsync(userId, deviceId, "Laptop", CancellationToken.None))
            .ReturnsAsync(outcome);
        var result = await new SessionMutationCommandHandler(service.Object, receipts.Object)
            .Handle(new RenameTrustedDeviceCommand(userId, deviceId, "Laptop"), CancellationToken.None);
        Assert.Equal(outcome, result);
        service.VerifyAll();
        service.VerifyNoOtherCalls();
    }
}
