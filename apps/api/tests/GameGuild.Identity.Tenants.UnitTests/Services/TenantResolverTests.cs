using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

public class TenantResolverTests
{
    [Fact]
    public async Task ResolveAsync_Should_Use_Header_When_Tenant_Found()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Header", Slug = "header", IsActive = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var domainsRepo = new Mock<ITenantDomainsRepository>();
        var resolver = new TenantResolver(mediator.Object, domainsRepo.Object, NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[TenantResolver.TenantIdHeader] = tenantId.ToString();

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.Header);
    }

    [Fact]
    public async Task ResolveAsync_Should_Use_Domain_When_Header_Missing()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Domain", Slug = "domain", IsActive = true };
        var domain = new TenantDomain { TenantId = tenantId, Tenant = tenant, TopLevelDomain = "example.com" };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var domainsRepo = new Mock<ITenantDomainsRepository>();
        domainsRepo.Setup(r => r.GetByDomainAsync("tenant.example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(domain);

        var resolver = new TenantResolver(mediator.Object, domainsRepo.Object, NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant.example.com");

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.Domain);
    }

    [Fact]
    public async Task ResolveAsync_Should_Use_Query_String_When_Present()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Query", Slug = "query", IsActive = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [TenantResolver.TenantIdQueryKey] = tenantId.ToString()
        });

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.QueryString);
    }

    [Fact]
    public async Task ResolveAsync_Should_Use_Route_Value_When_Present()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Route", Slug = "route", IsActive = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.RouteValues[TenantResolver.TenantIdQueryKey] = tenantId.ToString();

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.RouteValue);
    }

    [Fact]
    public async Task ResolveAsync_Should_Use_Claims_When_Present()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Claims", Slug = "claims", IsActive = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(TenantResolver.TenantIdClaimType, tenantId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.Claims);
    }

    [Fact]
    public async Task ResolveAsync_Should_Use_Default_Tenant_When_No_Other_Source()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Default", Slug = "default", IsActive = true, IsDefault = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(context);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.Default);
    }

    [Fact]
    public async Task ResolveAsync_Should_Return_None_When_No_Tenant_Resolved()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveByIdentifierAsync_Should_Return_Null_For_Empty()
    {
        var resolver = new TenantResolver(Mock.Of<IMediator>(), Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var result = await resolver.ResolveByIdentifierAsync("  ");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveByIdentifierAsync_Should_Handle_Guid_And_Slug()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsActive = true };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        mediator.Setup(m => m.Send(It.Is<GetTenantBySlugQuery>(q => q.Slug == "tenant"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var fromGuid = await resolver.ResolveByIdentifierAsync(tenantId.ToString());
        var fromSlug = await resolver.ResolveByIdentifierAsync("tenant");

        fromGuid.Should().Be(tenant);
        fromSlug.Should().Be(tenant);
    }

    [Fact]
    public void GetResolvedTenantId_Should_Return_TenantId_When_Present()
    {
        var resolver = new TenantResolver(Mock.Of<IMediator>(), Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Items[HttpContextKeys.AuthorizationTenantId] = tenantId;

        resolver.GetResolvedTenantId(context).Should().Be(tenantId);
    }
}
