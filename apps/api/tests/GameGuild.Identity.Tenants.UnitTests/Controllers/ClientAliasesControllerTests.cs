using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Commerce.Subscriptions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class ClientAliasesControllerTests
{
    [Fact]
    public async Task ClientCrudAliases_Should_Dispatch_TenantCommands()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();

        sender.Setup(s => s.Send<Guid>(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        sender.Setup(s => s.Send<PagedResult<Tenant>>(It.IsAny<GetTenantsPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Tenant>([], 0, 1, 20));
        sender.Setup(s => s.Send<Tenant?>(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Client", Slug = "client" });
        sender.Setup(s => s.Send<UpdateTenantCommand>(It.IsAny<UpdateTenantCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<ArchiveTenantResponse>(It.IsAny<ArchiveTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchiveTenantResponse { Success = true });
        sender.Setup(s => s.Send<UpdateTenantMetadataCommand>(It.IsAny<UpdateTenantMetadataCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ClientsController(sender.Object);

        (await controller.CreateClient(new CreateClientRequest("Client", "client", "admin@example.com", Cnpj: "04.252.011/0001-10"), CancellationToken.None))
            .Should().BeOfType<CreatedAtActionResult>();
        (await controller.GetClients(page: 0, pageSize: 600, status: "active", searchTerm: "cli", CancellationToken.None))
            .Should().BeOfType<OkObjectResult>();
        (await controller.GetClientById(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateClientById(tenantId, new UpdateTenantRequest("Updated", "Desc"), CancellationToken.None))
            .Should().BeOfType<NoContentResult>();
        (await controller.DeleteClientById(tenantId, new ArchiveRequest("contract closeout"), CancellationToken.None))
            .Should().BeOfType<NoContentResult>();

        sender.Verify(
            s => s.Send<PagedResult<Tenant>>(
                It.Is<GetTenantsPageQuery>(query => query.Page == 1 && query.PageSize == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sender.Verify(
            s => s.Send<UpdateTenantMetadataCommand>(
                It.Is<UpdateTenantMetadataCommand>(command =>
                    command.TenantId == tenantId
                    && command.Request.CustomFields != null
                    && command.Request.CustomFields.ContainsKey("fiscal")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateClient_Should_Reject_Invalid_Cnpj_Before_Creating_Tenant()
    {
        var sender = new Mock<ISender>();
        var controller = new ClientsController(sender.Object);

        var result = await controller.CreateClient(
            new CreateClientRequest("Client", "client", "admin@example.com", Cnpj: "11.111.111/1111-11"),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        sender.Verify(
            s => s.Send<Guid>(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ClientModulesAliases_Should_Dispatch_Subscriptions_And_FeatureFlags()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var subscriptions = PagedResult<Subscription>.FromPage(
            [
                new Subscription(
                    tenantId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    BillingCycle.Monthly,
                    new Money(49m, "USD"),
                    DateTime.UtcNow)
            ],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 20);

        sender.Setup(s => s.Send<PagedResult<Subscription>>(It.IsAny<GetPagedSubscriptionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);
        sender.Setup(s => s.Send<Dictionary<string, bool>?>(It.IsAny<GetTenantFeatureFlagsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool> { ["brokerage"] = true });
        sender.Setup(s => s.Send<UpdateTenantFeatureFlagsCommand>(It.IsAny<UpdateTenantFeatureFlagsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ClientsController(sender.Object);

        var modulesResult = await controller.GetClientModules(tenantId, page: 0, pageSize: 600, status: null, CancellationToken.None);
        var updateResult = await controller.UpdateClientModules(tenantId, new UpdateTenantFeatureFlagsRequest(new Dictionary<string, bool>
        {
            ["brokerage"] = true,
            ["maintenance"] = false
        }), CancellationToken.None);

        modulesResult.Should().BeOfType<OkObjectResult>();
        updateResult.Should().BeOfType<NoContentResult>();
        sender.Verify(
            s => s.Send<PagedResult<Subscription>>(
                It.Is<GetPagedSubscriptionsQuery>(query => query.TenantId == tenantId && query.Page == 1 && query.PageSize == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sender.Verify(
            s => s.Send<UpdateTenantFeatureFlagsCommand>(
                It.Is<UpdateTenantFeatureFlagsCommand>(command => command.TenantId == tenantId && command.Request.FeatureFlags["maintenance"] == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
