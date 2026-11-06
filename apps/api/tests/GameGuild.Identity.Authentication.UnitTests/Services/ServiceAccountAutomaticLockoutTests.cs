using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ServiceAccountAutomaticLockoutTests : IDisposable
{
    private readonly X509Certificate2 _authority = ClientCertificateTestFactory.CreateCertificateAuthority("Lockout test root");
    private readonly X509Certificate2 _certificate;
    private readonly X509Certificate2 _wrongCertificate;

    public ServiceAccountAutomaticLockoutTests()
    {
        _certificate = ClientCertificateTestFactory.CreateClientCertificate("allowed-client", _authority);
        _wrongCertificate = ClientCertificateTestFactory.CreateClientCertificate("other-client", _authority);
    }

    public void Dispose()
    {
        _wrongCertificate.Dispose();
        _certificate.Dispose();
        _authority.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutomaticLockout_ExpiresAndAcceptsValidCredentials(bool certificateFlow)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture();
        await LockAutomatically(fixture, certificateFlow);

        clock.Advance(TimeSpan.FromMinutes(31));
        var result = await Authenticate(fixture, certificateFlow, valid: true);

        result.Should().BeSameAs(fixture.Account);
        fixture.Account.IsLocked.Should().BeFalse();
        fixture.Account.FailedAuthenticationAttempts.Should().Be(0);
        fixture.Account.AuthenticationCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutomaticLockout_ExpiredWindowStartsNewFailureCounter(bool certificateFlow)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture();
        await LockAutomatically(fixture, certificateFlow);

        clock.Advance(TimeSpan.FromMinutes(31));
        (await Authenticate(fixture, certificateFlow, valid: false)).Should().BeNull();

        fixture.Account.IsLocked.Should().BeFalse();
        fixture.Account.FailedAuthenticationAttempts.Should().Be(1);
        fixture.Account.AuthenticationCount.Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManualLockout_OverridesAutomaticExpiry(bool certificateFlow)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture();
        await LockAutomatically(fixture, certificateFlow);
        fixture.Account.Lock("Administrative hold");

        clock.Advance(TimeSpan.FromDays(2));

        (await Authenticate(fixture, certificateFlow, valid: true)).Should().BeNull();
        fixture.Account.IsLocked.Should().BeTrue();
        fixture.Account.AuthenticationCount.Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyLockout_WithUnknownOriginRemainsLocked(bool certificateFlow)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture();
        fixture.Account.IsLocked = true;
        fixture.Account.LockedAt = SystemClock.UtcNow.AddDays(-2);
        fixture.Account.FailedAuthenticationAttempts = 10;

        (await Authenticate(fixture, certificateFlow, valid: true)).Should().BeNull();
        fixture.Account.IsLocked.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisallowedSource_CannotConsumeCredentialLockoutBudget(bool certificateFlow)
    {
        var fixture = CreateFixture();
        fixture.Account.AllowedIpAddresses = "192.0.2.10";

        for (var attempt = 0; attempt < 12; attempt++)
        {
            (await Authenticate(fixture, certificateFlow, valid: true, ip: "192.0.2.20")).Should().BeNull();
        }

        fixture.Account.IsLocked.Should().BeFalse();
        fixture.Account.FailedAuthenticationAttempts.Should().Be(0);
        fixture.Account.AuthenticationCount.Should().Be(0);
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        (await Authenticate(fixture, certificateFlow, valid: true, ip: "192.0.2.10")).Should().BeSameAs(fixture.Account);
    }

    private async Task LockAutomatically(Fixture fixture, bool certificateFlow)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            (await Authenticate(fixture, certificateFlow, valid: false)).Should().BeNull();
        }
        fixture.Account.IsLocked.Should().BeTrue();
    }

    private Task<ServiceAccount?> Authenticate(Fixture fixture, bool certificateFlow, bool valid, string? ip = null) =>
        certificateFlow
            ? fixture.Service.AuthenticateWithCertificateAsync(fixture.Account.ClientId, valid ? _certificate : _wrongCertificate, ip)
            : fixture.Service.AuthenticateAsync(fixture.Account.ClientId, valid ? "valid" : "invalid", ip);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutomaticLockout_RespectsConfiguredDeadlineExactly(bool certificateFlow)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture(durationMinutes: 7);
        await LockAutomatically(fixture, certificateFlow);
        fixture.Account.AutomaticLockoutEndsAt.Should().Be(SystemClock.UtcNow.AddMinutes(7));
        clock.Advance(TimeSpan.FromMinutes(7) - TimeSpan.FromTicks(1));
        (await Authenticate(fixture, certificateFlow, valid: true)).Should().BeNull();
        clock.Advance(TimeSpan.FromTicks(1));

        (await Authenticate(fixture, certificateFlow, valid: true)).Should().BeSameAs(fixture.Account);
        fixture.Account.AutomaticLockoutEndsAt.Should().BeNull();
        fixture.Account.LockedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExpiredAutomaticLockout_DoesNotReactivateInactiveOrExpiredAccount(bool certificateFlow, bool expired)
    {
        using var clock = new ClockScope();
        var fixture = CreateFixture();
        await LockAutomatically(fixture, certificateFlow);
        if (expired) fixture.Account.ExpiresAt = SystemClock.UtcNow.AddMinutes(1);
        else fixture.Account.IsActive = false;
        clock.Advance(TimeSpan.FromHours(1));

        (await Authenticate(fixture, certificateFlow, valid: true)).Should().BeNull();
        fixture.Account.AuthenticationCount.Should().Be(0);
    }

    private Fixture CreateFixture(int durationMinutes = 30)
    {
        var account = new ServiceAccount { Id = Guid.NewGuid(), ClientId = "lockout-test", Name = "Lockout test", IsActive = true };
        account.BindCertificate(ClientCertificateAuthenticationUtilities.GetNormalizedThumbprint(_certificate));
        var repository = new Mock<IServiceAccountRepository>();
        repository.Setup(x => x.GetByClientIdAsync(account.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        repository.Setup(x => x.UpdateAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var hasher = new Mock<IRefreshTokenHasher>();
        hasher.Setup(x => x.VerifyToken("valid", It.IsAny<string>())).Returns(true);
        return new Fixture(account, repository,
            new ServiceAccountService(repository.Object, hasher.Object, NullLogger<ServiceAccountService>.Instance,
                new AuthenticationSecurityOptions { AccountLockoutDurationMinutes = durationMinutes }));
    }

    private sealed record Fixture(ServiceAccount Account, Mock<IServiceAccountRepository> Repository, ServiceAccountService Service);

    private sealed class ClockScope : TimeProvider, IDisposable
    {
        private DateTimeOffset _now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        public ClockScope() => SystemClock.SetProvider(this);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
        public void Dispose() => SystemClock.Reset();
    }
}
