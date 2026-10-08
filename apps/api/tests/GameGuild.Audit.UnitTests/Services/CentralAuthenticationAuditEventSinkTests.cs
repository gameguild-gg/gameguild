using FluentAssertions;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class CentralAuthenticationAuditEventSinkTests
{
    [Theory]
    [InlineData("Authentication.MfaSignInRequired")]
    [InlineData("Authentication.MfaSignInDenied")]
    [InlineData("Authentication.MfaSignInEnrollmentStarted")]
    [InlineData("Authentication.MfaSignInVerified")]
    public async Task RequiredMfaAuditPersistenceFailuresReachTheOwningCommand(string action)
    {
        var audit = new Mock<IAuditService>(MockBehavior.Strict);
        var failure = new InvalidOperationException("Synthetic required audit persistence failure.");
        audit.Setup(port => port.LogAsync(It.IsAny<CreateAuditLogRequest>())).ThrowsAsync(failure);
        var sink = new CentralAuthenticationAuditEventSink(audit.Object, NullLogger<CentralAuthenticationAuditEventSink>.Instance);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sink.RecordAsync(
            new AuthenticationAuditEvent(action, Guid.NewGuid(), true, "Totp"), CancellationToken.None));
        Assert.Same(failure, exception);
        audit.Verify(port => port.LogAsync(It.IsAny<CreateAuditLogRequest>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_MapsAuthenticationContextToCentralAuditLog()
    {
        var auditService = new Mock<IAuditService>();
        CreateAuditLogRequest? captured = null;
        auditService
            .Setup(service => service.LogAsync(It.IsAny<CreateAuditLogRequest>()))
            .Callback<CreateAuditLogRequest>(request => captured = request)
            .Returns(Task.CompletedTask);
        var sink = new CentralAuthenticationAuditEventSink(
            auditService.Object,
            NullLogger<CentralAuthenticationAuditEventSink>.Instance);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await sink.RecordAsync(new AuthenticationAuditEvent(
            "Authentication.Failed",
            userId,
            false,
            "Password",
            "192.0.2.20",
            "UnitTestAgent",
            sessionId,
            tenantId,
            "InvalidCredentials",
            new { Attempt = 2 }),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ActionType.Should().Be("Authentication.Failed");
        captured.Category.Should().Be(AuditCategory.Authentication);
        captured.UserId.Should().Be(userId);
        captured.SessionId.Should().Be(sessionId);
        captured.TenantId.Should().Be(tenantId);
        captured.IpAddress.Should().Be("192.0.2.20");
        captured.UserAgent.Should().Be("UnitTestAgent");
        captured.Success.Should().BeFalse();
        captured.ErrorMessage.Should().Be("InvalidCredentials");
        captured.Description.Should().Contain("Password");
    }

    [Fact]
    public async Task RecordAsync_MapsAssessedRiskLevelForSuccessfulThreatEvents()
    {
        var auditService = new Mock<IAuditService>();
        CreateAuditLogRequest? captured = null;
        auditService
            .Setup(service => service.LogAsync(It.IsAny<CreateAuditLogRequest>()))
            .Callback<CreateAuditLogRequest>(request => captured = request)
            .Returns(Task.CompletedTask);
        var sink = new CentralAuthenticationAuditEventSink(
            auditService.Object,
            NullLogger<CentralAuthenticationAuditEventSink>.Instance);

        await sink.RecordAsync(new AuthenticationAuditEvent(
            "Authentication.ThreatDetected",
            Guid.NewGuid(),
            true,
            "Password",
            Metadata: new { RiskScore = 35 },
            AssessedRiskLevel: RiskLevel.Medium),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Success.Should().BeTrue();
        captured.ActionType.Should().Be("Authentication.ThreatDetected");
        captured.RiskLevel.Should().Be(AuditRiskLevel.Medium);
    }
}
