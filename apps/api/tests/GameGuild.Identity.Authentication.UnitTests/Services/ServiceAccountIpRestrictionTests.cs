using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ServiceAccountIpRestrictionTests : IDisposable
{
    private readonly X509Certificate2 _authority = ClientCertificateTestFactory.CreateCertificateAuthority("Service IP test root");
    private readonly X509Certificate2 _certificate;

    public ServiceAccountIpRestrictionTests()
    {
        _certificate = ClientCertificateTestFactory.CreateClientCertificate("service-ip-test", _authority);
    }

    public void Dispose()
    {
        _certificate.Dispose();
        _authority.Dispose();
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    [InlineData(false, "203.0.113.250")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    [InlineData(true, "203.0.113.250")]
    public async Task RestrictedAccount_RejectsMissingOrDisallowedIpBeforeCredentialAcceptance(bool certificate, string? ip)
    {
        var fixture = CreateFixture("192.0.2.10, 203.0.113.10");

        var result = await Authenticate(fixture.Service, certificate, ip);

        result.Should().BeNull();
        fixture.Account.AuthenticationCount.Should().Be(0);
        fixture.Account.LastAuthenticatedAt.Should().BeNull();
        fixture.Account.LastAuthenticatedFromIp.Should().BeNull();
        fixture.Account.FailedAuthenticationAttempts.Should().Be(0);
        fixture.Account.IsLocked.Should().BeFalse();
        fixture.Repository.Verify(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>()), Times.Never);
        fixture.Hasher.Verify(hasher => hasher.VerifyToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false, "192.0.2.10", "192.0.2.10, 203.0.113.10")]
    [InlineData(false, "203.0.113.10", "192.0.2.10, 203.0.113.10")]
    [InlineData(false, null, null)]
    [InlineData(false, null, "")]
    [InlineData(true, "192.0.2.10", "192.0.2.10, 203.0.113.10")]
    [InlineData(true, "203.0.113.10", "192.0.2.10, 203.0.113.10")]
    [InlineData(true, null, null)]
    [InlineData(true, null, "")]
    public async Task ValidCredentials_PreserveAllowedIpAndUnrestrictedAccountBehavior(bool certificate, string? ip, string? allowed)
    {
        var fixture = CreateFixture(allowed);
        fixture.Account.FailedAuthenticationAttempts = 2;

        var result = await Authenticate(fixture.Service, certificate, ip);

        result.Should().BeSameAs(fixture.Account);
        fixture.Account.AuthenticationCount.Should().Be(1);
        fixture.Account.LastAuthenticatedAt.Should().NotBeNull();
        fixture.Account.LastAuthenticatedFromIp.Should().Be(ip);
        fixture.Account.FailedAuthenticationAttempts.Should().Be(0);
        fixture.Repository.Verify(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Task<ServiceAccount?> Authenticate(ServiceAccountService service, bool certificate, string? ip) =>
        certificate
            ? service.AuthenticateWithCertificateAsync("service-ip-test", _certificate, ip)
            : service.AuthenticateAsync("service-ip-test", "test-credential", ip);

    private Fixture CreateFixture(string? allowed)
    {
        var account = new ServiceAccount
        {
            Id = Guid.NewGuid(), ClientId = "service-ip-test", ClientSecretHash = "test-digest",
            Name = "Service IP test", IsActive = true, AllowedIpAddresses = allowed
        };
        account.BindCertificate(ClientCertificateAuthenticationUtilities.GetNormalizedThumbprint(_certificate));
        var repository = new Mock<IServiceAccountRepository>();
        repository.Setup(repo => repo.GetByClientIdAsync(account.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        repository.Setup(repo => repo.UpdateAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var hasher = new Mock<IRefreshTokenHasher>();
        hasher.Setup(value => value.VerifyToken("test-credential", account.ClientSecretHash)).Returns(true);
        return new Fixture(account, repository, hasher,
            new ServiceAccountService(repository.Object, hasher.Object, NullLogger<ServiceAccountService>.Instance));
    }

    private sealed record Fixture(ServiceAccount Account, Mock<IServiceAccountRepository> Repository,
        Mock<IRefreshTokenHasher> Hasher, ServiceAccountService Service);
}
