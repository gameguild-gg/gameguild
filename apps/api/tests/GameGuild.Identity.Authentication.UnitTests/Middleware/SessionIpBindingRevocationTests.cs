using System.Security.Claims;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Middleware;

/// <summary>
///     Session IP binding (issue #278): with <see cref="SessionOptions.IpAddressBinding"/> set to
///     Strict, a session-bound request must originate from the session's recorded address; every
///     other configuration keeps the pre-existing behaviour (no address comparison).
/// </summary>
public sealed class SessionIpBindingRevocationTests
{
    private static (DefaultHttpContext Context, Mock<ITokenRevocationService> Revocation, Mock<IUserRepository> Users, Mock<IUserSessionRepository> Sessions, Func<Task> Next)
        CreateHarness(Guid userId, Guid sessionId, string? sessionIpAddress, System.Net.IPAddress? remoteIpAddress)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim(JwtClaimTypes.SessionId, sessionId.ToString())
            ], "Bearer"))
        };
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = remoteIpAddress;
        var revocation = new Mock<ITokenRevocationService>(MockBehavior.Strict);
        revocation
            .Setup(service => service.IsUserTokenRevokedAsync(userId, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var sessions = new Mock<IUserSessionRepository>(MockBehavior.Strict);
        sessions
            .Setup(repository => repository.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSession
            {
                Id = sessionId,
                UserId = userId,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IpAddress = sessionIpAddress ?? string.Empty
            });
        var invoked = false;
        Task Next()
        {
            invoked = true;
            return Task.CompletedTask;
        }
        return (context, revocation, users, sessions, () => { Assert.True(invoked); return Task.CompletedTask; });
    }

    private static TokenRevocationMiddleware CreateMiddleware(Func<Task> assertNextInvoked) =>
        new(_ =>
        {
            assertNextInvoked();
            return Task.CompletedTask;
        }, NullLogger<TokenRevocationMiddleware>.Instance);

    [Fact]
    public async Task StrictBindingAcceptsRequestFromTheRecordedAddress()
    {
        var (context, revocation, users, sessions, assertNext) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), "203.0.113.7", System.Net.IPAddress.Parse("203.0.113.7"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new SessionOptions { IpAddressBinding = SessionIpAddressBinding.Strict, EnableLocationTracking = true })
            .BuildServiceProvider();

        await CreateMiddleware(assertNext).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task StrictBindingNormalizesIpv4MappedIpv6BeforeComparing()
    {
        var (context, revocation, users, sessions, assertNext) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), "203.0.113.7", System.Net.IPAddress.Parse("::ffff:203.0.113.7"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new SessionOptions { IpAddressBinding = SessionIpAddressBinding.Strict, EnableLocationTracking = true })
            .BuildServiceProvider();

        await CreateMiddleware(assertNext).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("198.51.100.25")]     // different IPv4 address
    [InlineData("unknown")]           // session recorded no address (location tracking off)
    [InlineData("not-an-address")]    // unparseable session address
    [InlineData("")]                  // empty session address
    public async Task StrictBindingRejectsEverythingElse(string sessionIpAddress)
    {
        var (context, revocation, users, sessions, _) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), sessionIpAddress, System.Net.IPAddress.Parse("203.0.113.7"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new SessionOptions { IpAddressBinding = SessionIpAddressBinding.Strict, EnableLocationTracking = true })
            .BuildServiceProvider();

        await CreateMiddleware(() => Task.CompletedTask).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task StrictBindingFailsClosedWithoutARemoteAddress()
    {
        var (context, revocation, users, sessions, _) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), "203.0.113.7", remoteIpAddress: null);
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new SessionOptions { IpAddressBinding = SessionIpAddressBinding.Strict, EnableLocationTracking = true })
            .BuildServiceProvider();

        await CreateMiddleware(() => Task.CompletedTask).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultOffBindingAllowsAMismatchedAddress()
    {
        var (context, revocation, users, sessions, assertNext) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), "198.51.100.25", System.Net.IPAddress.Parse("203.0.113.7"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new SessionOptions { EnableLocationTracking = true })
            .BuildServiceProvider();

        await CreateMiddleware(assertNext).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task AbsentOptionsLeaveBindingOff()
    {
        var (context, revocation, users, sessions, assertNext) = CreateHarness(
            Guid.NewGuid(), Guid.NewGuid(), "198.51.100.25", System.Net.IPAddress.Parse("203.0.113.7"));
        context.RequestServices = new ServiceCollection().BuildServiceProvider();

        await CreateMiddleware(assertNext).InvokeAsync(context, revocation.Object, users.Object, sessions.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}

/// <summary>
///     Configuration coherence for the new session options (issue #278): strict IP binding
///     requires location tracking, and the shipped defaults stay valid.
/// </summary>
public sealed class SessionOptionsIpBindingValidationTests
{
    [Fact]
    public void DefaultOptionsRemainValid()
    {
        var validation = new SessionOptions().Validate();
        Assert.True(validation.IsValid);
        Assert.Equal(SessionIpAddressBinding.Off, new SessionOptions().IpAddressBinding);
    }

    [Fact]
    public void StrictBindingWithoutLocationTrackingIsRejected()
    {
        var validation = new SessionOptions
        {
            IpAddressBinding = SessionIpAddressBinding.Strict,
            EnableLocationTracking = false
        }.Validate();
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("IpAddressBinding", StringComparison.Ordinal));
    }

    [Fact]
    public void StrictBindingWithLocationTrackingIsValid()
    {
        var validation = new SessionOptions
        {
            IpAddressBinding = SessionIpAddressBinding.Strict,
            EnableLocationTracking = true
        }.Validate();
        Assert.True(validation.IsValid);
    }
}
