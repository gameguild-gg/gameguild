using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

public sealed class RateLimitAccessOptionsTests
{
    [Fact]
    public void Validate_MatchesAllowlistedNetworkContainingRequestIp()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["172.16.0.0/12"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.20.1.5");

        options.IsAllowlisted(context).Should().BeTrue();
    }

    [Fact]
    public void Validate_MatchesAllowlistedNetworkForIpv4MappedIpv6RequestIp()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["172.16.0.0/12"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:172.20.1.5");

        options.IsAllowlisted(context).Should().BeTrue();
    }

    [Fact]
    public void Validate_DoesNotMatchAllowlistedNetworkForForeignRequestIp()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["172.16.0.0/12"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

        options.IsAllowlisted(context).Should().BeFalse();
    }

    [Fact]
    public void Validate_DenylistedNetworkWinsOverOverlappingAllowlistedNetwork()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["10.0.0.0/8"];
        options.DenylistedIpNetworks = ["10.0.0.0/8"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");

        // The middleware evaluates the denylist before the allowlist, so a denied
        // network member must be rejected even when it also matches an allowlist.
        options.IsDenylisted(context).Should().BeTrue();
    }

    [Fact]
    public void Validate_ThrowsForInvalidCidrNetworkWithOptionNameAndValue()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["not-a-cidr"];

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rate limiting access option 'AllowlistedIpNetworks' contains invalid CIDR network 'not-a-cidr'.");
    }

    [Fact]
    public void Validate_RejectsDenylistedInvalidCidrNetworkWithOptionNameAndValue()
    {
        var options = new RateLimitAccessOptions();
        options.DenylistedIpNetworks = ["172.16.0.0"];

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rate limiting access option 'DenylistedIpNetworks' contains invalid CIDR network '172.16.0.0'.");
    }

    [Fact]
    public void IsAllowlisted_KeepsExactIpBehaviorWhenNoNetworksAreConfigured()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpAddresses = ["192.0.2.5"];
        options.Validate();

        var member = new DefaultHttpContext();
        member.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.5");
        var outsider = new DefaultHttpContext();
        outsider.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.6");

        options.IsAllowlisted(member).Should().BeTrue();
        options.IsAllowlisted(outsider).Should().BeFalse();
    }

    [Fact]
    public void IsDenylisted_KeepsExactIpBehaviorWhenNoNetworksAreConfigured()
    {
        var options = new RateLimitAccessOptions();
        options.DenylistedIpAddresses = ["192.0.2.5"];
        options.Validate();

        var member = new DefaultHttpContext();
        member.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.5");
        var outsider = new DefaultHttpContext();
        outsider.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.6");

        options.IsDenylisted(member).Should().BeTrue();
        options.IsDenylisted(outsider).Should().BeFalse();
    }

    [Fact]
    public void IsDenylisted_MatchesDenylistedNetworkMemberIp()
    {
        var options = new RateLimitAccessOptions();
        options.DenylistedIpNetworks = ["10.0.0.0/8"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.42.0.7");

        options.IsDenylisted(context).Should().BeTrue();
    }

    [Fact]
    public void IsAllowlisted_IgnoresUserIdClaimsWhenOnlyNetworksAreConfigured()
    {
        var options = new RateLimitAccessOptions();
        options.AllowlistedIpNetworks = ["172.16.0.0/12"];
        options.Validate();

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            authenticationType: "test"));
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

        options.IsAllowlisted(context).Should().BeFalse();
    }
}
