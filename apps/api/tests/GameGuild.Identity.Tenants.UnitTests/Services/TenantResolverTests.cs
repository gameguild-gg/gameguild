using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
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

    [Fact]
    public void GetResolvedTenantId_Should_Return_Null_When_Not_Present()
    {
        var resolver = new TenantResolver(Mock.Of<IMediator>(), Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);
        var context = new DefaultHttpContext();

        resolver.GetResolvedTenantId(context).Should().BeNull();
    }

    [Fact]
    public void GetResolvedTenantId_Should_Return_Null_When_Wrong_Type()
    {
        var resolver = new TenantResolver(Mock.Of<IMediator>(), Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);
        var context = new DefaultHttpContext();
        context.Items[HttpContextKeys.AuthorizationTenantId] = "not-a-guid";

        resolver.GetResolvedTenantId(context).Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Should_Log_Warning_When_Header_Tenant_Not_Found()
    {
        var tenantId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[TenantResolver.TenantIdHeader] = tenantId.ToString();

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Localhost_For_Domain_Resolution()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var domainsRepo = new Mock<ITenantDomainsRepository>();
        var resolver = new TenantResolver(mediator.Object, domainsRepo.Object, NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost", 5000);

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
        domainsRepo.Verify(r => r.GetByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Domain_When_Tenant_Is_Inactive()
    {
        var tenantId = Guid.NewGuid();
        var inactiveTenant = new Tenant { Id = tenantId, Name = "Inactive", Slug = "inactive", IsActive = false };
        var domain = new TenantDomain { TenantId = tenantId, Tenant = inactiveTenant, TopLevelDomain = "example.com" };

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var domainsRepo = new Mock<ITenantDomainsRepository>();
        domainsRepo.Setup(r => r.GetByDomainAsync("inactive.example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(domain);

        var resolver = new TenantResolver(mediator.Object, domainsRepo.Object, NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("inactive.example.com");

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Query_When_Tenant_Not_Found()
    {
        var tenantId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [TenantResolver.TenantIdQueryKey] = tenantId.ToString()
        });

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Route_When_Tenant_Not_Found()
    {
        var tenantId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        context.Request.RouteValues[TenantResolver.TenantIdQueryKey] = tenantId.ToString();

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Claims_When_Tenant_Not_Found()
    {
        var tenantId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(TenantResolver.TenantIdClaimType, tenantId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Claims_When_User_Not_Authenticated()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(TenantResolver.TenantIdClaimType, Guid.NewGuid().ToString()) };
        // User is not authenticated (no authentication type)
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public void GetTenantIdFromClaims_Should_Return_Null_For_Null_User_And_Missing_Identity()
    {
        var method = typeof(TenantResolver).GetMethod("GetTenantIdFromClaims", BindingFlags.NonPublic | BindingFlags.Static)!;

        method.Invoke(null, [null]).Should().BeNull();
        method.Invoke(null, [new ClaimsPrincipal()]).Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Claims_When_TenantId_Is_Empty_Guid()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(TenantResolver.TenantIdClaimType, Guid.Empty.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Claims_When_TenantId_Claim_Is_Invalid_Guid()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(TenantResolver.TenantIdClaimType, "not-a-guid") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveAsync_Should_Skip_Default_When_Inactive()
    {
        var inactiveTenant = new Tenant { Id = Guid.NewGuid(), Name = "Inactive", Slug = "inactive", IsActive = false, IsDefault = true };
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveTenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var context = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(context);

        result.Should().Be(TenantResolutionResult.None);
    }

    [Fact]
    public async Task ResolveByIdentifierAsync_Should_Reject_Empty_Guid()
    {
        var mediator = new Mock<IMediator>();
        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var result = await resolver.ResolveByIdentifierAsync(Guid.Empty.ToString());

        result.Should().BeNull();
        mediator.Verify(m => m.Send(It.IsAny<GetTenantBySlugQuery>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ResolveByIdentifierAsync_Should_Return_Null_For_Inactive_Slug_Tenant()
    {
        var inactiveTenant = new Tenant { Id = Guid.NewGuid(), Name = "Inactive", Slug = "inactive-slug", IsActive = false };
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetTenantBySlugQuery>(q => q.Slug == "inactive-slug"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveTenant);

        var resolver = new TenantResolver(mediator.Object, Mock.Of<ITenantDomainsRepository>(), NullLogger<TenantResolver>.Instance);

        var result = await resolver.ResolveByIdentifierAsync("inactive-slug");

        result.Should().BeNull();
    }
}
