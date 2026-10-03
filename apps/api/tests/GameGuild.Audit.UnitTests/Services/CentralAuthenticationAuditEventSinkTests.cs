using FluentAssertions;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class CentralAuthenticationAuditEventSinkTests
{
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
}
