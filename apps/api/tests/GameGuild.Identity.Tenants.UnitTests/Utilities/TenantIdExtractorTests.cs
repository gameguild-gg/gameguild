using FluentAssertions;
using GameGuild.Identity.Tenants.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Utilities;

public class TenantIdExtractorTests
{
    [Fact]
    public void FromHeader_Should_Return_TenantId_When_Valid()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = tenantId.ToString();

        TenantIdExtractor.FromHeader(context).Should().Be(tenantId);
    }

    [Fact]
    public void FromHeader_Should_Return_Null_On_Invalid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = "invalid";

        TenantIdExtractor.FromHeader(context).Should().BeNull();
    }

    [Fact]
    public void FromQuery_Should_Return_TenantId_When_Valid()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [TenantIdExtractor.DefaultTenantIdKey] = tenantId.ToString()
        });

        TenantIdExtractor.FromQuery(context).Should().Be(tenantId);
    }

    [Fact]
    public void FromRoute_Should_Return_TenantId_When_Valid()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Request.RouteValues[TenantIdExtractor.DefaultTenantIdKey] = tenantId.ToString();

        TenantIdExtractor.FromRoute(context).Should().Be(tenantId);
    }

    [Fact]
    public void FromRoute_Should_Return_Null_For_Empty_Guid()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues[TenantIdExtractor.DefaultTenantIdKey] = Guid.Empty.ToString();

        TenantIdExtractor.FromRoute(context).Should().BeNull();
    }

    [Fact]
    public void FromRoute_Should_Return_Null_For_Null_Route_Value()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues[TenantIdExtractor.DefaultTenantIdKey] = null;

        TenantIdExtractor.FromRoute(context).Should().BeNull();
    }

    [Fact]
    public void FromAnySource_Should_Prioritize_Header()
    {
        var context = new DefaultHttpContext();
        var headerId = Guid.NewGuid();
        var queryId = Guid.NewGuid();

        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = headerId.ToString();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [TenantIdExtractor.DefaultTenantIdKey] = queryId.ToString()
        });

        TenantIdExtractor.FromAnySource(context).Should().Be(headerId);
    }

    [Fact]
    public void FromAnySource_Should_Fallback_To_Query_Then_Route()
    {
        var queryContext = new DefaultHttpContext();
        var queryId = Guid.NewGuid();
        queryContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [TenantIdExtractor.DefaultTenantIdKey] = queryId.ToString()
        });

        TenantIdExtractor.FromAnySource(queryContext).Should().Be(queryId);

        var routeContext = new DefaultHttpContext();
        var routeId = Guid.NewGuid();
        routeContext.Request.RouteValues[TenantIdExtractor.DefaultTenantIdKey] = routeId.ToString();

        TenantIdExtractor.FromAnySource(routeContext).Should().Be(routeId);
    }

    [Fact]
    public void FromAnySource_Should_Return_Null_When_No_Source_Is_Valid()
    {
        var context = new DefaultHttpContext();

        TenantIdExtractor.FromAnySource(context).Should().BeNull();
    }

    [Fact]
    public void IsLocalhost_Should_Detect_Local_Hosts()
    {
        TenantIdExtractor.IsLocalhost("localhost").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("127.0.0.1").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("::1").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("example.com").Should().BeFalse();
    }

    [Fact]
    public void IsLocalhost_Should_Use_HttpContext_Host()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");

        TenantIdExtractor.IsLocalhost(context).Should().BeTrue();
    }

    [Fact]
    public void ExtractSubdomain_Should_Return_Subdomain_When_Present()
    {
        TenantIdExtractor.ExtractSubdomain("tenant.example.com").Should().Be("tenant");
        TenantIdExtractor.ExtractSubdomain("example.com").Should().BeNull();
    }

    [Fact]
    public void ExtractSubdomain_Should_Use_HttpContext_Host()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("team.example.com");

        TenantIdExtractor.ExtractSubdomain(context).Should().Be("team");
    }
}
