using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Fresh-verification gate (issue #1144): trusted-device mutations require a recent
///     authentication (auth_time within the window) and an MFA-verified token when the
///     user has MFA enabled — the same sensitive-action assurance used by teams/projects.
/// </summary>
public sealed class FreshVerificationGateTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private ActorContext Actor(
        DateTimeOffset? authenticatedAt = null,
        bool mfaVerified = false,
        bool isAuthenticated = true) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = _userId.ToString(),
        TenantId = Guid.NewGuid(),
        IsAuthenticated = isAuthenticated,
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>(),
        TypedAttributes = new ActorAttributes
        {
            AuthenticatedAt = authenticatedAt,
            MfaVerified = mfaVerified
        }
    };

    private FreshVerificationGate CreateGate(
        ActorContext actor,
        bool mfaEnabled = false,
        SessionOptions? options = null)
    {
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(a => a.ActorContext).Returns(actor);
        var mfaService = new Mock<IMfaService>();
        mfaService.Setup(s => s.IsMfaEnabledAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaEnabled);
        return new FreshVerificationGate(
            actorAccessor.Object,
            mfaService.Object,
            options,
            NullLogger<FreshVerificationGate>.Instance);
    }

    [Fact]
    public async Task RecentAuthenticationWithoutMfa_IsVerified()
    {
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-2)));
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task StaleAuthentication_IsDenied()
    {
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-30)));
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task MissingAuthTimeClaim_IsDenied()
    {
        var gate = CreateGate(Actor(authenticatedAt: null));
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task UnauthenticatedActor_IsDenied()
    {
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-1), isAuthenticated: false));
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task MfaEnabledWithoutVerifiedToken_IsDenied()
    {
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-2), mfaVerified: false), mfaEnabled: true);
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task MfaEnabledWithVerifiedToken_IsVerified()
    {
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-2), mfaVerified: true), mfaEnabled: true);
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task ConfiguredWindowExtendsTheFreshnessBound()
    {
        var options = new SessionOptions { DeviceTrustFreshAuthenticationWindowMinutes = 60 };
        var gate = CreateGate(Actor(SystemClock.UtcNow.AddMinutes(-30)), options: options);
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task DisabledGate_IsAlwaysVerified()
    {
        var options = new SessionOptions { RequireFreshAuthenticationForDeviceTrust = false };
        var gate = CreateGate(Actor(authenticatedAt: null), options: options);
        var result = await gate.VerifyRecentAuthenticationAsync();
        result.IsVerified.Should().BeTrue();
    }
}
