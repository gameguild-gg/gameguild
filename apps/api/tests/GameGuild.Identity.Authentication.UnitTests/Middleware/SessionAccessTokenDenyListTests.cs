using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Middleware;

/// <summary>
///     Issue #1179: every revocation path must write the affected access-token JTI to the
///     per-token deny-list. These tests pin the session→JTI index (tracked at mint time by
///     JwtTokenService), the deny-list write on session termination, and the resulting 401
///     from TokenRevocationMiddleware for a token whose JTI was denied.
/// </summary>
public sealed class SessionAccessTokenDenyListTests
{
    private static InMemoryTokenRevocationService NewInMemoryStore() =>
        new(NullLogger<InMemoryTokenRevocationService>.Instance);

    private static DistributedCacheTokenRevocationService NewDistributedStore() =>
        new(new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheTokenRevocationService>.Instance);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SessionRevocation_WritesTrackedAccessTokenJtiToDenyList(bool distributed)
    {
        ITokenRevocationService store = distributed ? NewDistributedStore() : NewInMemoryStore();
        var sessionId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        await store.TrackSessionAccessTokenAsync(sessionId, jti, expiresAt);
        (await store.IsRevokedAsync(jti)).Should().BeFalse();

        await store.RevokeSessionAccessTokenAsync(sessionId, "SessionTerminated");

        (await store.IsRevokedAsync(jti)).Should().BeTrue();
    }

    [Fact]
    public async Task SessionRevocation_WithoutTrackedToken_IsNoOp()
    {
        var store = NewInMemoryStore();

        await store.RevokeSessionAccessTokenAsync(Guid.NewGuid(), "SessionTerminated");

        // No throw, no deny-list noise: an untracked session (pre-#1179 lineage) revokes nothing.
    }

    [Fact]
    public async Task SessionRevocation_IsIdempotentForAlreadyDeniedJti()
    {
        var store = NewInMemoryStore();
        var sessionId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString();

        await store.TrackSessionAccessTokenAsync(sessionId, jti, DateTime.UtcNow.AddMinutes(30));
        await store.RevokeSessionAccessTokenAsync(sessionId, "First");
        await store.RevokeSessionAccessTokenAsync(sessionId, "Second");

        (await store.IsRevokedAsync(jti)).Should().BeTrue();
    }

    [Fact]
    public async Task Tracking_NewMint_OverwritesPreviousJti()
    {
        var store = NewInMemoryStore();
        var sessionId = Guid.NewGuid();
        var firstJti = Guid.NewGuid().ToString();
        var secondJti = Guid.NewGuid().ToString();

        await store.TrackSessionAccessTokenAsync(sessionId, firstJti, DateTime.UtcNow.AddMinutes(30));
        await store.TrackSessionAccessTokenAsync(sessionId, secondJti, DateTime.UtcNow.AddMinutes(30));
        await store.RevokeSessionAccessTokenAsync(sessionId, "SessionTerminated");

        // A refresh minted a second access token for the session; revoking the session denies
        // the CURRENT token. The superseded first JTI is not denied by this path (it was replaced
        // by rotation; its refresh sibling is consumed by the rotation itself).
        (await store.IsRevokedAsync(secondJti)).Should().BeTrue();
    }

    [Fact]
    public async Task TerminateSessionAsync_WritesDenyListForTrackedAccessToken()
    {
        var store = NewInMemoryStore();
        var sessionId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString();
        await store.TrackSessionAccessTokenAsync(sessionId, jti, DateTime.UtcNow.AddMinutes(30));

        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSession { Id = sessionId, UserId = Guid.NewGuid(), IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1) });
        sessions.Setup(r => r.UpdateAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession)null!);
        var service = new SessionManagementService(
            NullLogger<SessionManagementService>.Instance,
            sessions.Object,
            Mock.Of<ITrustedDeviceRepository>(),
            Mock.Of<IRefreshTokenLineageRepository>(),
            tokenRevocationService: store);

        var terminated = await service.TerminateSessionAsync(sessionId, SessionTerminationReason.UserLogout);

        terminated.Should().BeTrue();
        (await store.IsRevokedAsync(jti)).Should().BeTrue();
    }

    [Fact]
    public async Task TerminateAllUserSessionsAsync_WritesDenyListForEachTrackedAccessToken()
    {
        var store = NewInMemoryStore();
        var userId = Guid.NewGuid();
        var first = (SessionId: Guid.NewGuid(), Jti: Guid.NewGuid().ToString());
        var second = (SessionId: Guid.NewGuid(), Jti: Guid.NewGuid().ToString());
        await store.TrackSessionAccessTokenAsync(first.SessionId, first.Jti, DateTime.UtcNow.AddMinutes(30));
        await store.TrackSessionAccessTokenAsync(second.SessionId, second.Jti, DateTime.UtcNow.AddMinutes(30));

        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSession>
            {
                new() { Id = first.SessionId, UserId = userId, IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1) },
                new() { Id = second.SessionId, UserId = userId, IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1) }
            });
        sessions.Setup(r => r.UpdateAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession)null!);
        var service = new SessionManagementService(
            NullLogger<SessionManagementService>.Instance,
            sessions.Object,
            Mock.Of<ITrustedDeviceRepository>(),
            Mock.Of<IRefreshTokenLineageRepository>(),
            tokenRevocationService: store);

        var terminated = await service.TerminateAllUserSessionsAsync(userId, SessionTerminationReason.SecurityViolation);

        terminated.Should().Be(2);
        (await store.IsRevokedAsync(first.Jti)).Should().BeTrue();
        (await store.IsRevokedAsync(second.Jti)).Should().BeTrue();
    }

    /// <summary>
    ///     End-to-end revocation contract (issue #1179): after a session terminates (or a user
    ///     signs out), a subsequent request carrying the denied JTI must receive 401 from
    ///     TokenRevocationMiddleware.
    /// </summary>
    [Fact]
    public async Task RequestWithDeniedJti_IsRejectedByTokenRevocationMiddleware()
    {
        var store = NewInMemoryStore();
        var sessionId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await store.TrackSessionAccessTokenAsync(sessionId, jti, DateTime.UtcNow.AddMinutes(30));

        // Simulate the sign-out/termination path writing the deny-list entry.
        await store.RevokeSessionAccessTokenAsync(sessionId, "User initiated sign-out");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("jti", jti)
            ], "Bearer"))
        };
        context.Response.Body = new MemoryStream();
        var called = false;
        var middleware = new TokenRevocationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<TokenRevocationMiddleware>.Instance);

        await middleware.InvokeAsync(context, store, Mock.Of<IUserRepository>(), Mock.Of<IUserSessionRepository>());

        called.Should().BeFalse();
        context.User.Identity?.IsAuthenticated.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ToString().Should().Be("Bearer");
    }
}
