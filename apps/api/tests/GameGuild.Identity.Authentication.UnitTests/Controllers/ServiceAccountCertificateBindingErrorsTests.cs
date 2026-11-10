using System.Data.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class ServiceAccountCertificateBindingErrorsTests
{
    private const string Thumbprint = "AABBCCDDEEFF00112233445566778899AABBCCDD";

    [Theory]
    [InlineData("empty-thumb")]
    [InlineData("short-thumb")]
    [InlineData("non-hex-thumb")]
    [InlineData("short-spki")]
    public async Task Bind_InvalidPinReturns400BeforeLoadingOrUpdatingAccount(string invalid)
    {
        var fixture = CreateFixture();
        var request = new BindServiceAccountCertificateRequest
        {
            Thumbprint = invalid switch { "empty-thumb" => "", "short-thumb" => "ABCD", "non-hex-thumb" => new string('Z', 40), _ => Thumbprint },
            SpkiSha256 = invalid == "short-spki" ? "ABCD" : null
        };

        var result = await fixture.Controller.BindClientCertificate(fixture.Account.Id, request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
        fixture.Repository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Bind_UniqueConflictReturns409AndRestoresTrackedBinding()
    {
        var fixture = CreateFixture();
        fixture.Account.BindCertificate(new string('B', 40), new string('C', 64));
        var oldUpdatedAt = fixture.Account.UpdatedAt;
        fixture.Repository.Setup(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Binding rejected", new BindingDbException("23505")));

        var result = await fixture.Controller.BindClientCertificate(fixture.Account.Id,
            new BindServiceAccountCertificateRequest { Thumbprint = Thumbprint }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(409);
        fixture.Account.CertificateThumbprint.Should().Be(new string('B', 40));
        fixture.Account.CertificateSpkiSha256.Should().Be(new string('C', 64));
        fixture.Account.UpdatedAt.Should().Be(oldUpdatedAt);
    }

    [Theory]
    [InlineData("08006")]
    [InlineData("40001")]
    public async Task Bind_UnrelatedDatabaseFailureStillPropagates(string sqlState)
    {
        var fixture = CreateFixture();
        var failure = new DbUpdateException("Database unavailable", new BindingDbException(sqlState));
        fixture.Repository.Setup(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        var action = () => fixture.Controller.BindClientCertificate(fixture.Account.Id,
            new BindServiceAccountCertificateRequest { Thumbprint = Thumbprint }, CancellationToken.None);

        (await action.Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task Bind_MissingAccountStillReturns404()
    {
        var fixture = CreateFixture();
        fixture.Repository.Setup(repo => repo.GetByIdAsync(fixture.Account.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ServiceAccount?)null);

        var result = await fixture.Controller.BindClientCertificate(fixture.Account.Id,
            new BindServiceAccountCertificateRequest { Thumbprint = Thumbprint }, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Bind_ValidPinStillNormalizesAndPersists()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.BindClientCertificate(fixture.Account.Id,
            new BindServiceAccountCertificateRequest { Thumbprint = Thumbprint.ToLowerInvariant() }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        fixture.Account.CertificateThumbprint.Should().Be(Thumbprint);
        fixture.Repository.Verify(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Bind_OversizedHexInputReturns400WithoutLoadingAccount()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.BindClientCertificate(fixture.Account.Id,
            new BindServiceAccountCertificateRequest { Thumbprint = new string('A', 1_000_000) }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
        fixture.Repository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Fixture CreateFixture()
    {
        var account = new ServiceAccount { Id = Guid.NewGuid(), Name = "Certificate binding test", ClientId = "binding-test", IsActive = true };
        var repository = new Mock<IServiceAccountRepository>();
        repository.Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        repository.Setup(repo => repo.UpdateAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var service = new ServiceAccountService(repository.Object, Mock.Of<IRefreshTokenHasher>(), NullLogger<ServiceAccountService>.Instance);
        return new Fixture(account, repository, new ServiceAccountOperationsController(service, new CommandHandlerSender(service, Mock.Of<IJwtTokenService>())));
    }

    private sealed record Fixture(ServiceAccount Account, Mock<IServiceAccountRepository> Repository, ServiceAccountOperationsController Controller);

    private sealed class BindingDbException(string sqlState) : DbException("Synthetic provider failure")
    {
        public override string? SqlState => sqlState;
    }
}
