using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class ServiceAccountCertificateUnbindingTests
{
    [Theory]
    [InlineData("Admin", false, false, false, 204)]
    [InlineData("Owner", false, false, false, 204)]
    [InlineData("TenantAdmin", false, false, false, 204)]
    [InlineData("Member", false, false, false, 403)]
    [InlineData("Admin", true, false, false, 403)]
    [InlineData("Admin", false, true, false, 403)]
    [InlineData("Admin", false, false, true, 403)]
    [InlineData("SystemAdmin", true, false, false, 204)]
    [InlineData("SystemAdmin", false, true, false, 204)]
    public async Task Unbind_OnlyAuthenticatedAdministratorsInTheAccountTenantCanRemovePins(
        string role, bool otherTenant, bool global, bool anonymous, int expectedStatus)
    {
        var tenant = Guid.NewGuid();
        var actor = ActorContext.Anonymous with
        {
            ActorKind = anonymous ? ActorKind.Anonymous : ActorKind.User,
            IsAuthenticated = !anonymous,
            SubjectId = anonymous ? null : Guid.NewGuid().ToString(),
            TenantId = otherTenant ? Guid.NewGuid() : tenant,
            Roles = new HashSet<string> { role }
        };
        var fixture = CreateFixture(actor, global ? null : tenant);

        var result = await InvokeEndpoint(fixture.Controller, fixture.Account.Id);

        if (expectedStatus == 204)
        {
            result.Should().BeOfType<NoContentResult>();
            fixture.Account.CertificateThumbprint.Should().BeNull();
            fixture.Account.CertificateSpkiSha256.Should().BeNull();
        }
        else
        {
            result.Should().BeOfType<ForbidResult>();
            fixture.Account.CertificateThumbprint.Should().Be(new string('A', 40));
            fixture.Account.CertificateSpkiSha256.Should().Be(new string('B', 64));
        }
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()),
            expectedStatus == 204 ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task Unbind_MissingAccountReturns404()
    {
        var fixture = CreateFixture(Admin(), Guid.NewGuid());
        fixture.Repository.Setup(repo => repo.GetByIdAsync(fixture.Account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount?)null);

        (await InvokeEndpoint(fixture.Controller, fixture.Account.Id)).Should().BeOfType<NotFoundResult>();
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unbind_AlreadyUnboundAccountRemainsUsableWithItsSecret()
    {
        var fixture = CreateFixture(Admin(), Guid.NewGuid());
        fixture.Account.UnbindCertificate();

        (await InvokeEndpoint(fixture.Controller, fixture.Account.Id)).Should().BeOfType<NoContentResult>();
        (await fixture.Service.AuthenticateAsync(fixture.Account.ClientId, "valid-secret", null)).Should().BeSameAs(fixture.Account);
        fixture.Account.IsActive.Should().BeTrue();
        fixture.Account.IsLocked.Should().BeFalse();
    }

    [Theory]
    [InlineData("service")]
    [InlineData("invalid-subject")]
    [InlineData("empty-subject")]
    public async Task Unbind_RejectsInvalidActorBeforeLoadingAccount(string invalidActor)
    {
        var actor = Admin() with
        {
            ActorKind = invalidActor == "service" ? ActorKind.Service : ActorKind.User,
            SubjectId = invalidActor == "invalid-subject" ? "invalid" : invalidActor == "empty-subject" ? Guid.Empty.ToString() : Guid.NewGuid().ToString()
        };
        var fixture = CreateFixture(actor, Guid.NewGuid());

        (await InvokeEndpoint(fixture.Controller, fixture.Account.Id)).Should().BeOfType<ForbidResult>();

        fixture.Repository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(repo => repo.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unbind_FormerlyMatchingCertificateCanNoLongerAuthenticateButSecretStillWorks()
    {
        var fixture = CreateFixture(Admin(), Guid.NewGuid());
        using var authority = ClientCertificateTestFactory.CreateCertificateAuthority("Unbinding test root");
        using var certificate = ClientCertificateTestFactory.CreateClientCertificate("unbind-client", authority);
        fixture.Account.BindCertificate(ClientCertificateAuthenticationUtilities.GetNormalizedThumbprint(certificate),
            ClientCertificateAuthenticationUtilities.ComputeSpkiSha256Hex(certificate));
        (await fixture.Service.AuthenticateWithCertificateAsync(fixture.Account.ClientId, certificate, null)).Should().BeSameAs(fixture.Account);

        (await InvokeEndpoint(fixture.Controller, fixture.Account.Id)).Should().BeOfType<NoContentResult>();

        (await fixture.Service.AuthenticateWithCertificateAsync(fixture.Account.ClientId, certificate, null)).Should().BeNull();
        (await fixture.Service.AuthenticateAsync(fixture.Account.ClientId, "valid-secret", null)).Should().BeSameAs(fixture.Account);
    }

    [Fact]
    public async Task Unbind_FailedPersistencePropagatesAndRestoresTrackedPins()
    {
        var fixture = CreateFixture(Admin(), Guid.NewGuid());
        var updatedAt = fixture.Account.UpdatedAt;
        var failure = new IOException("Persistence unavailable");
        fixture.Repository.Setup(repo => repo.UpdateAsync(fixture.Account, It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        var action = () => InvokeEndpoint(fixture.Controller, fixture.Account.Id);

        (await action.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);
        fixture.Account.CertificateThumbprint.Should().Be(new string('A', 40));
        fixture.Account.CertificateSpkiSha256.Should().Be(new string('B', 64));
        fixture.Account.UpdatedAt.Should().Be(updatedAt);
    }

    private static Task<IActionResult> InvokeEndpoint(ServiceAccountOperationsController controller, Guid id) =>
        controller.UnbindClientCertificate(id, CancellationToken.None);

    private static ActorContext Admin() => ActorContext.Anonymous with
    {
        ActorKind = ActorKind.User, IsAuthenticated = true, SubjectId = Guid.NewGuid().ToString(),
        Roles = new HashSet<string> { "SystemAdmin" }
    };

    private static Fixture CreateFixture(ActorContext actor, Guid? tenant)
    {
        var account = new ServiceAccount { Id = Guid.NewGuid(), TenantId = tenant, Name = "Unbinding test", ClientId = "unbind", IsActive = true };
        account.BindCertificate(new string('A', 40), new string('B', 64));
        var repository = new Mock<IServiceAccountRepository>();
        repository.Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        repository.Setup(repo => repo.GetByClientIdAsync(account.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        repository.Setup(repo => repo.UpdateAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var hasher = new Mock<IRefreshTokenHasher>();
        hasher.Setup(x => x.VerifyToken("valid-secret", It.IsAny<string>())).Returns(true);
        var service = new ServiceAccountService(repository.Object, hasher.Object, NullLogger<ServiceAccountService>.Instance);
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(x => x.ActorContext).Returns(actor);
        var sender = new CommandHandlerSender(service, Mock.Of<IJwtTokenService>(), accessor.Object);
        return new Fixture(account, repository, service, new ServiceAccountOperationsController(service, sender));
    }

    private sealed record Fixture(ServiceAccount Account, Mock<IServiceAccountRepository> Repository,
        ServiceAccountService Service, ServiceAccountOperationsController Controller);
}
