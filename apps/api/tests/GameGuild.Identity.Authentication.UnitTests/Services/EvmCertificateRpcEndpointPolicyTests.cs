using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class EvmCertificateRpcEndpointPolicyTests
{
    [Theory]
    [InlineData("https://rpc.sepolia.example/")]
    [InlineData("https://sepolia.infura.io/v3/")] // path-bearing public endpoint shape
    [InlineData("https://1.1.1.1/")]
    [InlineData("https://83.12.55.9/")]
    [InlineData("https://172.32.0.1/")]   // first public address after RFC 1918 172.16.0.0/12
    [InlineData("https://100.128.0.1/")]  // first public address after RFC 6598 100.64.0.0/10
    [InlineData("https://[2606:4700:4700::1111]/")]
    [InlineData("https://[2a01:4f8::1]/")]
    public void PublicEndpointsAreAccepted(string endpoint)
    {
        Assert.Empty(EvmCertificateRpcEndpointPolicy.Validate(new Uri(endpoint, UriKind.Absolute)));
    }

    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("https://LOCALHOST/")]
    [InlineData("https://localhost./")]
    [InlineData("https://node.localhost/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("https://127.9.9.9/")]
    [InlineData("https://0.0.0.0/")]
    [InlineData("https://10.0.0.5/")]
    [InlineData("https://172.16.0.1/")]
    [InlineData("https://172.31.255.255/")]
    [InlineData("https://169.254.169.254/")] // cloud instance metadata
    [InlineData("https://192.168.1.10/")]
    [InlineData("https://192.0.2.1/")]
    [InlineData("https://198.51.100.7/")]
    [InlineData("https://203.0.113.9/")]
    [InlineData("https://100.64.1.1/")]
    [InlineData("https://224.0.0.1/")]
    [InlineData("https://239.1.1.1/")]
    [InlineData("https://240.1.2.3/")]
    [InlineData("https://255.255.255.255/")]
    [InlineData("https://[::]/")]
    [InlineData("https://[::1]/")]
    [InlineData("https://[::ffff:127.0.0.1]/")] // IPv4-mapped loopback
    [InlineData("https://[::ffff:10.0.0.1]/")]  // IPv4-mapped private
    [InlineData("https://[64:ff9b::101:202]/")] // well-known NAT64
    [InlineData("https://[fe80::1]/")]
    [InlineData("https://[fc00::1]/")]
    [InlineData("https://[fd12:3456:789a::1]/")]
    [InlineData("https://[fec0::1]/")]
    [InlineData("https://[ff02::1]/")]
    [InlineData("https://[2001:db8::1]/")]
    [InlineData("https://[2002:0a00:0001::]/")] // 6to4 embedding 10.0.0.1
    public void LoopbackPrivateReservedAndMulticastTargetsAreRejected(string endpoint)
    {
        Assert.NotEmpty(EvmCertificateRpcEndpointPolicy.Validate(new Uri(endpoint, UriKind.Absolute)));
    }

    [Theory]
    [InlineData("http://rpc.sepolia.example/")]
    [InlineData("ftp://rpc.sepolia.example/")]
    public void OnlyHttpsSchemesAreAccepted(string endpoint)
    {
        Assert.Contains(EvmCertificateRpcEndpointPolicy.Validate(new Uri(endpoint, UriKind.Absolute)),
            error => error.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void NullEndpointFailsClosed()
    {
        Assert.Contains(EvmCertificateRpcEndpointPolicy.Validate(null),
            error => error.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void RelativeUrisFailClosed()
    {
        Assert.Contains(EvmCertificateRpcEndpointPolicy.Validate(new Uri("/relative", UriKind.Relative)),
            error => error.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void HostlessEndpointsCannotEvenBeConstructedAsAbsoluteUris()
    {
        // Layered rejection: the Uri parser refuses hostless HTTPS strings before this policy runs,
        // and the policy keeps its own hostless branch as a defensive backstop for programmatic callers.
        Assert.False(Uri.TryCreate("https:///path", UriKind.Absolute, out _));
    }

    [Fact]
    public void RejectionsNeverEchoTheEndpointOrHost()
    {
        var endpoint = new Uri("https://169.254.169.254/latest/meta-data/", UriKind.Absolute);
        var errors = EvmCertificateRpcEndpointPolicy.Validate(endpoint);
        Assert.NotEmpty(errors);
        foreach (var error in errors)
        {
            Assert.DoesNotContain("169.254", error, StringComparison.Ordinal);
            Assert.DoesNotContain("meta-data", error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AcceptedTestPlaceholderNameRemainsUsableForUnitFixtures()
    {
        // The existing unit fixtures use the reserved .invalid TLD as a non-resolving DNS placeholder;
        // it is a hostname, not an address, and remains acceptable to this address-focused policy.
        Assert.Empty(EvmCertificateRpcEndpointPolicy.Validate(new Uri("https://rpc.invalid/", UriKind.Absolute)));
    }
}
