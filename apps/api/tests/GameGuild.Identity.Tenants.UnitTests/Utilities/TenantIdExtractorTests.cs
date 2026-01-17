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
    public void IsLocalhost_Should_Detect_Local_Hosts()
    {
        TenantIdExtractor.IsLocalhost("localhost").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("127.0.0.1").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("::1").Should().BeTrue();
        TenantIdExtractor.IsLocalhost("example.com").Should().BeFalse();
    }

    [Fact]
    public void ExtractSubdomain_Should_Return_Subdomain_When_Present()
    {
        TenantIdExtractor.ExtractSubdomain("tenant.example.com").Should().Be("tenant");
        TenantIdExtractor.ExtractSubdomain("example.com").Should().BeNull();
    }
}
