using FluentAssertions;
using GameGuild.Identity.Tenants.Utilities;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Contexts;

/// <summary>
/// Unit tests for tenant context extraction helpers used by tenant-aware middleware.
/// </summary>
public class TenantContextTests
{
    [Fact]
    public void FromAnySource_ShouldPreferHeaderOverQueryAndRoute()
    {
        var headerTenantId = Guid.NewGuid();
        var queryTenantId = Guid.NewGuid();
        var routeTenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();

        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = headerTenantId.ToString();
        context.Request.QueryString = new QueryString($"?tenantId={queryTenantId}");
        context.Request.RouteValues[TenantIdExtractor.DefaultTenantIdKey] = routeTenantId.ToString();

        TenantIdExtractor.FromAnySource(context).Should().Be(headerTenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void FromHeader_ShouldRejectInvalidTenantIds(string tenantHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = tenantHeader;

        TenantIdExtractor.FromHeader(context).Should().BeNull();
    }

    [Fact]
    public void FromHeader_ShouldRejectEmptyGuid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TenantIdExtractor.DefaultTenantIdHeader] = Guid.Empty.ToString();

        TenantIdExtractor.FromHeader(context).Should().BeNull();
    }

    [Theory]
    [InlineData("tenant.example.com", "tenant")]
    [InlineData("localhost", null)]
    [InlineData("example.com", null)]
    public void ExtractSubdomain_ShouldReturnOnlyMultiSegmentSubdomain(string host, string? expected)
    {
        TenantIdExtractor.ExtractSubdomain(host).Should().Be(expected);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void IsLocalhost_ShouldRecognizeDevelopmentHosts(string host)
    {
        TenantIdExtractor.IsLocalhost(host).Should().BeTrue();
    }
}
