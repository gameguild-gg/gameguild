using System.Security.Claims;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Authorization;

/// <summary>
///     Trusted-device gate for sensitive operations (issue #278,
/// <c>Session:RequireTrustedDeviceForSensitiveOps</c>): inert while disabled, and once enabled
/// it fails closed for untrusted/unknown sessions while letting non-session schemes through.
/// </summary>
public sealed class TrustedDeviceAuthorizationHandlerTests
{
    private static ClaimsPrincipal Principal(Guid userId, Guid? sessionId)
    {
        var claims = new List<Claim> { new("sub", userId.ToString()) };
        if (sessionId.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.SessionId, sessionId.Value.ToString()));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static AuthorizationHandlerContext Context(ClaimsPrincipal user) =>
        new(new[] { new TrustedDeviceRequirement() }, user, resource: null);

    private static async Task<AuthorizationHandlerContext> Evaluate(
        ClaimsPrincipal user,
        ISessionManagementService service,
        SessionOptions options)
    {
        var context = Context(user);
        await new TrustedDeviceAuthorizationHandler(service, options).HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task DisabledOptionSucceedsForEveryoneWithoutTouchingTheRegistries()
    {
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        var context = await Evaluate(
            Principal(Guid.NewGuid(), Guid.NewGuid()),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = false });
        Assert.True(context.HasSucceeded);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnabledOptionPassesRequestsWithoutAnInteractiveSession()
    {
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        var context = await Evaluate(
            Principal(Guid.NewGuid(), sessionId: null),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = true });
        Assert.True(context.HasSucceeded);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnabledOptionFailsWhenTheSessionCannotBeResolved()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        service.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync((UserSession?)null);
        var context = await Evaluate(
            Principal(userId, sessionId),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = true });
        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("terminated")]
    [InlineData("foreign-owner")]
    [InlineData("no-fingerprint")]
    public async Task EnabledOptionFailsForUnusableSessions(string scenario)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = userId,
            IsActive = true,
            DeviceFingerprint = "device-1",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        switch (scenario)
        {
            case "inactive": session.IsActive = false; break;
            case "terminated": session.TerminatedAt = DateTime.UtcNow; break;
            case "foreign-owner": session.UserId = Guid.NewGuid(); break;
            case "no-fingerprint": session.DeviceFingerprint = string.Empty; break;
        }
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        service.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var context = await Evaluate(
            Principal(userId, sessionId),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = true });
        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnabledOptionDecidesByTheTrustedDeviceRegistry(bool trusted)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        service.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserSession
        {
            Id = sessionId,
            UserId = userId,
            IsActive = true,
            DeviceFingerprint = "device-1",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        service.Setup(s => s.IsDeviceTrustedAsync(userId, "device-1", It.IsAny<CancellationToken>())).ReturnsAsync(trusted);
        var context = await Evaluate(
            Principal(userId, sessionId),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = true });
        Assert.Equal(trusted, context.HasSucceeded);
    }

    [Fact]
    public async Task RegistryOutagesFailClosed()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new Mock<ISessionManagementService>(MockBehavior.Strict);
        service.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("registry unavailable"));
        var context = await Evaluate(
            Principal(userId, sessionId),
            service.Object,
            new SessionOptions { RequireTrustedDeviceForSensitiveOps = true });
        Assert.False(context.HasSucceeded);
    }
}
