using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class AuthenticationAuditForwardingTests
{
    [Fact]
    public async Task MfaAttempt_IsForwardedToCentralAuditWithRequestContext()
    {
        var repository = new Mock<IMfaAttemptRepository>();
        var auditEventSink = new Mock<IAuthenticationAuditEventSink>();
        repository
            .Setup(x => x.CreateAsync(It.IsAny<MfaAttempt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaAttempt attempt, CancellationToken _) => attempt);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");
        httpContext.Request.Headers.UserAgent = "MfaUnitTestAgent";
        var service = new MfaAttemptTrackingService(
            NullLogger<MfaAttemptTrackingService>.Instance,
            Mock.Of<IUserMfaConfigurationRepository>(),
            repository.Object,
            new HttpContextAccessor { HttpContext = httpContext },
            auditEventSink: auditEventSink.Object);
        var userId = Guid.NewGuid();

        await service.RecordMfaAttemptAsync(userId, MfaMethod.Totp, false, "InvalidCode", "device-hash", CancellationToken.None);

        auditEventSink.Verify(x => x.RecordAsync(
            It.Is<AuthenticationAuditEvent>(auditEvent =>
                auditEvent.ActionType == "Authentication.MfaFailed" &&
                auditEvent.UserId == userId &&
                auditEvent.Method == nameof(MfaMethod.Totp) &&
                auditEvent.IpAddress == "198.51.100.25" &&
                auditEvent.UserAgent == "MfaUnitTestAgent" &&
                auditEvent.ErrorMessage == "InvalidCode"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
