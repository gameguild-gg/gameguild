using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

/// <summary>
///     Controller-level enforcement of the fresh-verification prerequisite (issue #1144):
///     trusting or revoking a trusted device is rejected unless the acting session's
///     authentication is recent (optionally MFA-verified).
/// </summary>
public sealed class TrustedDevicesFreshVerificationTests
{
    private readonly Mock<ISessionManagementService> _sessionService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    private TrustedDevicesController CreateController(IFreshVerificationGate? gate)
    {
        var controller = new TrustedDevicesController(
            _sessionService.Object,
            new CommandHandlerSender(_sessionService.Object),
            gate)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                        new Claim("sub", _userId.ToString()),
                        new Claim("session_id", _sessionId.ToString())
                    }, "Test"))
                }
            }
        };

        _sessionService.Setup(service => service.GetSessionAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSession
            {
                Id = _sessionId,
                UserId = _userId,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                DeviceFingerprint = "session-fingerprint"
            });
        _sessionService.Setup(service => service.TrustDeviceAsync(
                _userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sessionService.Setup(service => service.RevokeTrustedDeviceAsync(
                _userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return controller;
    }

    [Fact]
    public async Task TrustCurrentDevice_WithFreshVerification_ReturnsOk()
    {
        var gate = new Mock<IFreshVerificationGate>();
        gate.Setup(g => g.VerifyRecentAuthenticationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshVerificationResult.Verified);

        var result = await CreateController(gate.Object).TrustCurrentDevice(
            new TrustDeviceRequest { DeviceName = "My Phone" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TrustCurrentDevice_WithoutFreshVerification_ReturnsForbid()
    {
        var gate = new Mock<IFreshVerificationGate>();
        gate.Setup(g => g.VerifyRecentAuthenticationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshVerificationResult.Failed("stale authentication"));

        var result = await CreateController(gate.Object).TrustCurrentDevice(
            new TrustDeviceRequest { DeviceName = "My Phone" }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _sessionService.Verify(
            s => s.TrustDeviceAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeTrustedDevice_WithoutFreshVerification_ReturnsForbid()
    {
        var gate = new Mock<IFreshVerificationGate>();
        gate.Setup(g => g.VerifyRecentAuthenticationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshVerificationResult.Failed("stale authentication"));

        var result = await CreateController(gate.Object).RevokeTrustedDevice(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _sessionService.Verify(
            s => s.RevokeTrustedDeviceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeTrustedDevice_WithFreshVerification_ReturnsOk()
    {
        var gate = new Mock<IFreshVerificationGate>();
        gate.Setup(g => g.VerifyRecentAuthenticationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshVerificationResult.Verified);

        var result = await CreateController(gate.Object).RevokeTrustedDevice(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
