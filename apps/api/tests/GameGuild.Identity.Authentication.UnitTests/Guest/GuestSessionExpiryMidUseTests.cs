using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Guest;

/// <summary>
///     Guest session expiry during active use (issue #1128): a guest crossing the session
///     boundary mid-journey must not lose their data silently. Within the store's retention
///     grace past the token's expiry, sign-in binds the still-retained snapshot to the
///     authenticated user (one-time continuity migration — the credential stays dead), so
///     cart/progress survive via <see cref="SignInResponse.UpgradedFromGuestSessionId" />.
///     When continuity is impossible (token unreadable, or past the grace with the record
///     gone), the sign-in still succeeds but the response carries the explicit
///     guest-session-expired signal instead of a silently missing field.
/// </summary>
public sealed class GuestSessionExpiryMidUseTests
{
    private static readonly JwtOptions TestJwtOptions = new()
    {
        Issuer = "GameGuild-Test",
        Audience = "GameGuild.Users",
        SecretKey = "test-secret-key-at-least-32-characters-long!!"
    };

    private sealed class Fixture
    {
        public Fixture(int retentionSlackMinutes = 5)
        {
            Settings = new GuestSessionOptions { IsEnabled = true, AccessTokenLifetimeMinutes = 30, StoreEvictionSlackMinutes = retentionSlackMinutes };
            TokenService = new GuestTokenService(Options.Create(TestJwtOptions), NullLogger<GuestTokenService>.Instance);
            Cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            Store = new DistributedCacheGuestSessionStore(Cache, Options.Create(Settings), NullLogger<DistributedCacheGuestSessionStore>.Instance);
            AuditSink = new Mock<IAuthenticationAuditEventSink>();
            Service = new GuestSessionService(
                Store,
                TokenService,
                Options.Create(Settings),
                AuditSink.Object,
                NullLogger<GuestSessionService>.Instance);
        }

        public GuestSessionOptions Settings { get; }
        public GuestTokenService TokenService { get; }
        public MemoryDistributedCache Cache { get; }
        public DistributedCacheGuestSessionStore Store { get; }
        public Mock<IAuthenticationAuditEventSink> AuditSink { get; }
        public GuestSessionService Service { get; }
    }

    /// <summary>
    ///     Crafts a guest token exactly like <see cref="GuestTokenService.Issue" /> but with
    ///     the given (possibly past) expiry — the credential a browser still holds after the
    ///     session boundary passed.
    /// </summary>
    private static string ExpiredGuestToken(Guid guestSessionId, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtOptions.SecretKey)) { KeyId = "GameGuild-jwt-key" };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            TestJwtOptions.Issuer,
            GuestAccessDefaults.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(expiresAt.AddMinutes(-30)).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new Claim(ClaimNames.GuestId, guestSessionId.ToString()),
                new Claim(ClaimNames.Role, GuestIdentity.RoleName)
            ],
            expiresAt.AddMinutes(-30),
            expiresAt,
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task SaveSnapshot(DistributedCacheGuestSessionStore store, Guid guestSessionId, DateTime issuedAt, DateTime expiresAt)
    {
        await store.SaveAsync(new GuestSessionSnapshot
        {
            GuestSessionId = guestSessionId,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            IpAddress = "203.0.113.7",
            UserAgent = "ExpiryTest/1.0"
        });
    }

    // ── Continuity inside the retention grace ────────────────────────────────────────

    [Fact]
    public async Task ExpiredMidUse_WithinRetentionGrace_UpgradeCompletesWithDataPreserved()
    {
        var fixture = new Fixture(retentionSlackMinutes: 5);
        var guestSessionId = Guid.NewGuid();
        var now = SystemClock.UtcNow;

        // The guest session expired two minutes ago; the snapshot is still retained by the
        // store's eviction slack and has never been upgraded. This is the "data" (cart /
        // progress is keyed to the guest session id in the domain modules).
        await SaveSnapshot(fixture.Store, guestSessionId, now.AddMinutes(-32), now.AddMinutes(-2));
        var expiredToken = ExpiredGuestToken(guestSessionId, now.AddMinutes(-2));

        // The expired credential no longer validates — the door is closed for guests…
        fixture.TokenService.Validate(expiredToken).Should().BeNull("the guest credential is dead after expiry");

        // …but the sign-in-side read still identifies the session it named.
        fixture.TokenService.TryReadExpired(expiredToken, out var readSessionId, out var readExpiresAt)
            .Should().BeTrue();
        readSessionId.Should().Be(guestSessionId);
        readExpiresAt.Should().BeAfter(now.AddMinutes(-3));

        // The user signs in right after the boundary: continuity completes with data —
        // the upgrade records the migration and hands back the ORIGINAL guest session id.
        var userId = Guid.NewGuid();
        var upgrade = await fixture.Service.UpgradeAsync(expiredToken, userId, Guid.NewGuid());

        upgrade.Success.Should().BeTrue("an expired-mid-use session still upgrades inside the retention grace");
        upgrade.GuestSessionId.Should().Be(guestSessionId, "the response names the session whose data was preserved");

        // The upgrade is visible in the store — the domain migration can find it — and is
        // one-shot: a second attempt is refused.
        fixture.AuditSink.Verify(sink => sink.RecordAsync(
            It.Is<AuthenticationAuditEvent>(e =>
                e.ActionType == "Authentication.GuestSessionUpgraded"),
            It.IsAny<CancellationToken>()), Times.Once);
        var second = await fixture.Service.UpgradeAsync(expiredToken, userId, Guid.NewGuid());
        second.Success.Should().BeFalse("the continuity migration is one-time");
    }

    // ── Continuity impossible: past the grace, the data is gone and the client is told ─

    [Fact]
    public async Task ExpiredMidUse_PastRetentionGrace_SignInSucceedsWithExpiredSignal()
    {
        var fixture = new Fixture(retentionSlackMinutes: 5);
        var guestSessionId = Guid.NewGuid();
        var now = SystemClock.UtcNow;

        // The token expired ten minutes ago — beyond the retention grace. No snapshot can
        // survive that long (the store's TTL is expiry + slack), so nothing to migrate.
        var longExpiredToken = ExpiredGuestToken(guestSessionId, now.AddMinutes(-10));

        var upgrade = await fixture.Service.UpgradeAsync(longExpiredToken, Guid.NewGuid(), Guid.NewGuid());

        upgrade.Success.Should().BeFalse("no continuity is possible past the retention grace");
        upgrade.FailureReason.Should().Be(GuestSessionFailureReason.GuestSessionExpired,
            "the refusal says WHY: the session expired, it is not an invalid credential");

        // The expired-signals surface: the integration stamps the response so clients are
        // told instead of silently observing a missing continuity field.
        var response = new SignInResponse { Success = true, UserId = Guid.NewGuid(), SessionId = Guid.NewGuid() };
        var stamped = await GuestSessionUpgradeIntegration.ApplyGuestUpgradeAsync(response, fixture.Service, longExpiredToken, CancellationToken.None);

        stamped.Should().BeSameAs(response);
        stamped.Success.Should().BeTrue("the sign-in itself is never blocked by guest expiry");
        stamped.UpgradedFromGuestSessionId.Should().BeNull();
        stamped.GuestSessionExpired.Should().BeTrue("the response carries the session-expired signal");
    }

    // ── The full mid-use sequence: expire, re-issue, sign in — data follows the user ──

    [Fact]
    public async Task ExpiredMidUse_ThenReIssueAndSignIn_ReissuedSessionIsIndependent_ExpiredDataStillMigrates()
    {
        var fixture = new Fixture(retentionSlackMinutes: 5);
        var now = SystemClock.UtcNow;

        // The original journey: a guest session that expired one minute ago, data retained.
        var originalSessionId = Guid.NewGuid();
        await SaveSnapshot(fixture.Store, originalSessionId, now.AddMinutes(-31), now.AddMinutes(-1));
        var expiredToken = ExpiredGuestToken(originalSessionId, now.AddMinutes(-1));

        // The browser hits the issue endpoint again: a BRAND-NEW session id (the documented
        // re-issue behavior — no refresh, no silent continuation).
        var reissued = await fixture.Service.IssueAsync(new GuestSessionRequest { IpAddress = "203.0.113.7" });
        reissued.Success.Should().BeTrue();
        reissued.GuestSessionId.Should().NotBe(originalSessionId,
            "re-issue mints a fresh session; the expired one is never revived");

        // The user then signs in presenting the EXPIRED credential from the old journey:
        // within the grace, that old session (and its data) is what migrates.
        var upgrade = await fixture.Service.UpgradeAsync(expiredToken, Guid.NewGuid(), Guid.NewGuid());
        upgrade.Success.Should().BeTrue("the upgrade flow preserves the expired session's data on re-issue");
        upgrade.GuestSessionId.Should().Be(originalSessionId, "continuity points at the original data-bearing session");
    }

    // ── Garbage credentials never reach the continuity path ──────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("gg_live_forged")]
    public async Task UnreadableCredential_IsInvalidGuestToken_NotAnExpirySignal(string? token)
    {
        var fixture = new Fixture();

        var upgrade = await fixture.Service.UpgradeAsync(token, Guid.NewGuid(), Guid.NewGuid());

        upgrade.Success.Should().BeFalse();
        upgrade.FailureReason.Should().Be(GuestSessionFailureReason.InvalidGuestToken,
            "an unreadable credential is an invalid token, not an expired session");
    }

    [Fact]
    public async Task ValidToken_EvictedSnapshot_ReportsSessionUnavailable()
    {
        var fixture = new Fixture(retentionSlackMinutes: 5);
        var guestSessionId = Guid.NewGuid();

        // A structurally valid, NOT-yet-expired token whose snapshot record is gone (evicted
        // or never persisted): the door reads the credential fine but there is nothing to
        // migrate — the unavailable reason, distinct from both invalid and expired.
        var token = ExpiredGuestToken(guestSessionId, SystemClock.UtcNow.AddMinutes(5));

        var upgrade = await fixture.Service.UpgradeAsync(token, Guid.NewGuid(), Guid.NewGuid());

        upgrade.Success.Should().BeFalse();
        upgrade.FailureReason.Should().Be(GuestSessionFailureReason.GuestSessionUnavailable);
    }
}
