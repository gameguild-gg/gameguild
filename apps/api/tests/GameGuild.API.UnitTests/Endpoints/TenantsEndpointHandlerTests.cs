using FluentAssertions;
using GameGuild.API.Endpoints;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Moq;
using EndpointCreateTenantRequest = GameGuild.API.Endpoints.CreateTenantRequest;
using EndpointUpdateTenantRequest = GameGuild.API.Endpoints.UpdateTenantRequest;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class TenantsEndpointHandlerTests
{
    [Fact]
    public void TenantDetailRoutes_ShouldExposeTenantIdToTenantResolutionMiddleware()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        new TenantsEndpoint().MapEndpoint(app);

        var routePatterns = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        routePatterns.Should().Contain("/tenants/{tenantId:guid}");
        routePatterns.Should().NotContain("/tenants/{id:guid}");
    }

    [Fact]
    public async Task GetTenants_ShouldUseTenantQueryAndMapReturnedTenants()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Game Guild",
            Slug = "game-guild",
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
        };
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<GetTenantsPageQuery>(query => query.Page == 2 && query.PageSize == 25), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Tenant>([tenant], 1, 25, 25));

        var result = await TenantsEndpointHandlers.GetTenants(sender.Object, 2, 25, CancellationToken.None);

        var ok = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var tenants = ok.Value.Should().BeAssignableTo<IReadOnlyCollection<TenantResponse>>().Subject;
        tenants.Should().ContainSingle().Which.Should().Match<TenantResponse>(response =>
            response.Id == tenant.Id &&
            response.Name == tenant.Name &&
            response.Slug == tenant.Slug &&
            response.IsActive == tenant.IsActive &&
            response.CreatedAt == tenant.CreatedAt);
    }

    [Fact]
    public async Task GetTenant_ShouldReturnNotFoundWhenQueryReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<GetTenantByIdQuery>(query => query.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await TenantsEndpointHandlers.GetTenant(tenantId, sender.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task CreateTenant_ShouldRequireAdminEmailBeforeSendingCommand()
    {
        var sender = new Mock<ISender>();
        var request = new EndpointCreateTenantRequest("Game Guild", "game-guild");

        var result = await TenantsEndpointHandlers.CreateTenant(request, sender.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        sender.Verify(x => x.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTenant_ShouldSendCreateCommandAndFetchCreatedTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Example Tenant",
            Slug = "game-guild",
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
        };
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<CreateTenantCommand>(command =>
                command.Name == "Game Guild" &&
                command.Slug == "game-guild" &&
                command.AdminEmail == "admin@game-guild.com" &&
                command.Description == "Internal tenant"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        sender
            .Setup(x => x.Send(It.Is<GetTenantByIdQuery>(query => query.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var request = new EndpointCreateTenantRequest("Game Guild", "game-guild", "admin@game-guild.com", "Internal tenant");

        var result = await TenantsEndpointHandlers.CreateTenant(request, sender.Object, CancellationToken.None);

        var created = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        created.Value.Should().BeAssignableTo<TenantResponse>()
            .Which.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task UpdateTenant_ShouldSendUpdateCommandAndReturnUpdatedTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Updated",
            Slug = "game-guild",
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
        };
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<UpdateTenantCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender
            .Setup(x => x.Send(It.Is<ActivateTenantCommand>(command => command.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActivateTenantResponse { Success = true, TenantId = tenantId });
        sender
            .Setup(x => x.Send(It.Is<GetTenantByIdQuery>(query => query.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await TenantsEndpointHandlers.UpdateTenant(
            tenantId,
            new EndpointUpdateTenantRequest("Updated", true, "Updated description"),
            sender.Object,
            CancellationToken.None);

        var ok = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        ok.Value.Should().BeAssignableTo<TenantResponse>().Which.Name.Should().Be("Updated");
        sender.Verify(x => x.Send(It.Is<UpdateTenantCommand>(command =>
            command.TenantId == tenantId &&
            command.Name == "Updated" &&
            command.Description == "Updated description"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTenant_ShouldArchiveTenantAndReportNotFoundWhenArchiveFails()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<ArchiveTenantCommand>(command =>
                command.TenantId == tenantId &&
                command.Reason == "Deleted through legacy /tenants endpoint"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchiveTenantResponse { Success = false, TenantId = tenantId, Message = "missing" });

        var result = await TenantsEndpointHandlers.DeleteTenant(tenantId, sender.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetTenants_ShouldNormalizeInvalidPagination()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<GetTenantsPageQuery>(query => query.Page == 1 && query.PageSize == 500), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Tenant>([], 0, 1, 500));

        var result = await TenantsEndpointHandlers.GetTenants(sender.Object, 0, 501, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetTenant_ShouldReturnMappedTenantWhenFound()
    {
        var tenant = CreateTenant();
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.Is<GetTenantByIdQuery>(query => query.TenantId == tenant.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await TenantsEndpointHandlers.GetTenant(tenant.Id, sender.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IValueHttpResult>().Which.Value
            .Should().BeAssignableTo<TenantResponse>().Which.Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task CreateTenant_ShouldReturnIdentifierWhenCreatedTenantCannotBeReadBack()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenantId);
        sender.Setup(x => x.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var result = await TenantsEndpointHandlers.CreateTenant(
            new EndpointCreateTenantRequest("Platform", "platform", AdminEmail: "admin@example.com"),
            sender.Object,
            CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task UpdateTenant_ShouldDeactivateAndReturnNotFoundWhenTenantCannotBeReadBack()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<UpdateTenantCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.Is<DeactivateTenantCommand>(command => command.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var result = await TenantsEndpointHandlers.UpdateTenant(
            tenantId,
            new EndpointUpdateTenantRequest("Platform", false),
            sender.Object,
            CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task DeleteTenant_ShouldReturnNoContentWhenArchiveSucceeds()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<ArchiveTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchiveTenantResponse { Success = true, TenantId = tenantId });

        var result = await TenantsEndpointHandlers.DeleteTenant(tenantId, sender.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Platform",
        Slug = "platform",
        IsActive = true,
        CreatedAt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc)
    };
}
