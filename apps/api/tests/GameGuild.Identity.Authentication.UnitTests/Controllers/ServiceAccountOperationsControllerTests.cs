using GameGuild.Identity.Context.Actors;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class ServiceAccountOperationsControllerTests
{
    [Fact]
    public async Task RotateSecret_ShouldReturnOk_WithNewSecret()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.RotateSecretAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-secret");

        var controller = CreateController(service, serviceAccountId);

        var result = await controller.RotateSecret(serviceAccountId, new RotateServiceAccountSecretRequest("verified-test-receipt"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<SecretRotationResponse>().Subject;
        payload.ClientSecret.Should().Be("new-secret");
    }

    [Fact]
    public async Task RotateSecret_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        var controller = CreateController(service, serviceAccountId, accountExists: false);

        var result = await controller.RotateSecret(serviceAccountId, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Unlock_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.UnlockAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.Unlock(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Unlock_ShouldReturnNotFound_WhenServiceThrowsInvalidOperation()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.UnlockAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(service);

        var result = await controller.Unlock(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Lock_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount?)null);

        var controller = CreateController(service);

        var result = await controller.Lock(serviceAccountId, new LockServiceAccountRequest { Reason = "manual" }, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Lock_ShouldCallServiceAndReturnNoContent_WhenAccountExists()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceAccount { Id = serviceAccountId, ClientId = "svc", Name = "Jobs" });

        service
            .Setup(x => x.LockAsync(serviceAccountId, "manual", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.Lock(serviceAccountId, new LockServiceAccountRequest { Reason = "manual" }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        service.Verify(x => x.LockAsync(serviceAccountId, "manual", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount?)null);

        var controller = CreateController(service);

        var result = await controller.GetAuditLog(serviceAccountId, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAuditLog_ShouldClampPagingAndReturnResponse()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();
        var entries = new[]
        {
            new ServiceAccountAuditEntry { Id = Guid.NewGuid(), Action = "Locked" }
        };

        service
            .Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceAccount { Id = serviceAccountId, ClientId = "svc", Name = "Jobs" });

        service
            .Setup(x => x.GetAuditLogAsync(serviceAccountId, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedAuditResult(entries, 1));

        var controller = CreateController(service);

        var result = await controller.GetAuditLog(serviceAccountId, page: 0, pageSize: 500, cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ServiceAccountAuditLogResponse>().Subject;
        payload.ServiceAccountId.Should().Be(serviceAccountId);
        payload.Page.Should().Be(1);
        payload.PageSize.Should().Be(100);
        payload.TotalCount.Should().Be(1);
        payload.Entries.Should().BeEquivalentTo(entries);
    }

    [Fact]
    public async Task Deactivate_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.DeactivateAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.Deactivate(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ShouldReturnNotFound_WhenServiceThrowsInvalidOperation()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.DeactivateAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(service);

        var result = await controller.Deactivate(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Reactivate_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.ReactivateAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.Reactivate(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Reactivate_ShouldReturnNotFound_WhenServiceThrowsInvalidOperation()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.ReactivateAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(service);

        var result = await controller.Reactivate(serviceAccountId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateScopes_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.UpdateScopesAsync(serviceAccountId, "read:all", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.UpdateScopes(serviceAccountId, new UpdateScopesRequest { Scopes = "read:all" }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateScopes_ShouldReturnNotFound_WhenServiceThrowsInvalidOperation()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();

        service
            .Setup(x => x.UpdateScopesAsync(serviceAccountId, "read:all", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(service);

        var result = await controller.UpdateScopes(serviceAccountId, new UpdateScopesRequest { Scopes = "read:all" }, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    private static ServiceAccountOperationsController CreateController(
        Mock<IServiceAccountService> service, Guid? accountId = null, bool accountExists = true)
    {
        var tenant = Guid.NewGuid();
        var actor = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenant)
            .WithAttribute("session_id", Guid.NewGuid().ToString()).Build() with
        { Roles = new HashSet<string> { "Admin" } };
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(actor);
        if (accountId.HasValue)
        {
            service.Setup(x => x.GetByIdAsync(accountId.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accountExists ? new ServiceAccount { Id = accountId.Value, TenantId = tenant } : null);
        }
        return new ServiceAccountOperationsController(service.Object,
            new CommandHandlerSender(service.Object, Mock.Of<IJwtTokenService>(), accessor, Mock.Of<IStepUpReceiptService>()),
            Mock.Of<IStepUpRequirementService>());
    }

    [Fact]
    public async Task BindClientCertificate_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();
        service
            .Setup(x => x.BindCertificateAsync(
                serviceAccountId,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service);

        var result = await controller.BindClientCertificate(
            serviceAccountId,
            new BindServiceAccountCertificateRequest
            {
                Thumbprint = "aa11bb22cc33dd44ee55ff66778899aabbccddee",
                SpkiSha256 = null
            },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        service.Verify(x => x.BindCertificateAsync(
            serviceAccountId,
            "aa11bb22cc33dd44ee55ff66778899aabbccddee",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BindClientCertificate_ShouldReturnNotFound_WhenServiceThrowsInvalidOperation()
    {
        var service = new Mock<IServiceAccountService>();
        var serviceAccountId = Guid.NewGuid();
        service
            .Setup(x => x.BindCertificateAsync(
                serviceAccountId,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(service);

        var result = await controller.BindClientCertificate(
            serviceAccountId,
            new BindServiceAccountCertificateRequest { Thumbprint = "aa11bb22cc33dd44ee55ff66778899aabbccddee" },
            CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
