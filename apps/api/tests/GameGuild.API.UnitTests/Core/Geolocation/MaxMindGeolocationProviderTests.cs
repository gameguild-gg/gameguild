using FluentAssertions;
using GameGuild.API.Geolocation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.API.UnitTests.Core.Geolocation;

/// <summary>
///     MaxMind GeoLite2 adapter tests (issue #242): fail-closed behavior when configuration or
///     the database file is absent — every lookup resolves to null (Unknown), never throws.
///     The positive-path tests (issue #278 IP geolocation integration) run against MaxMind's
///     official GeoIP2 test databases (Fixtures/Geolocation, from maxmind/MaxMind-DB
///     test-data, CC-SA 4.0), so city resolution and anonymous-IP trait resolution are
///     exercised against real .mmdb readers.
/// </summary>
public sealed class MaxMindGeolocationProviderTests
{
    private static string CityDatabasePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Geolocation", "GeoIP2-City-Test.mmdb");

    private static string AnonymousDatabasePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Geolocation", "GeoIP2-Anonymous-IP-Test.mmdb");

    [Fact]
    public async Task NoDatabasePathConfigured_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider((string?)null, NullLogger<MaxMindGeolocationProvider>.Instance);

        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MissingDatabaseFile_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider("Z:/does-not-exist/GeoLite2-City.mmdb", NullLogger<MaxMindGeolocationProvider>.Instance);

        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    public async Task MalformedIp_ResolvesToNull(string ipAddress)
    {
        using var provider = new MaxMindGeolocationProvider((string?)null, NullLogger<MaxMindGeolocationProvider>.Instance);

        var result = await provider.ResolveAsync(ipAddress);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("192.168.1.1")]
    [InlineData("172.16.0.9")]
    [InlineData("::1")]
    public async Task LoopbackAndPrivateAddresses_AreNotResolved(string ipAddress)
    {
        using var provider = new MaxMindGeolocationProvider((string?)null, NullLogger<MaxMindGeolocationProvider>.Instance);

        var result = await provider.ResolveAsync(ipAddress);

        result.Should().BeNull();
    }

    // ── Anonymous-IP (VPN/proxy/Tor/hosting) database, issue #278 ───────

    [Fact]
    public async Task MissingAnonymousDatabaseFile_Alone_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider(
            (string?)null, NullLogger<MaxMindGeolocationProvider>.Instance, "Z:/does-not-exist/GeoIP2-Anonymous-IP.mmdb");

        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    [Fact]
    public async Task NoDatabasesConfigured_AtAll_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider(
            (string?)null, NullLogger<MaxMindGeolocationProvider>.Instance, (string?)null);

        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    // ── Positive paths against MaxMind's official GeoIP2 test databases (issue #278) ──

    [Fact]
    public async Task CityDatabase_ResolvesPublicAddressToLocation()
    {
        using var provider = new MaxMindGeolocationProvider(
            CityDatabasePath, NullLogger<MaxMindGeolocationProvider>.Instance);

        // 81.2.69.160 is MaxMind's canonical City test record (London, GB).
        var result = await provider.ResolveAsync("81.2.69.160");

        result.Should().NotBeNull();
        result!.Country.Should().Be("United Kingdom");
        result.CountryCode.Should().Be("GB");
        result.City.Should().Be("London");
        result.Latitude.Should().BeApproximately(51.5142, 0.01);
        result.Longitude.Should().BeApproximately(-0.0931, 0.01);
        result.Timezone.Should().Be("Europe/London");
        // No anonymous-IP database configured: anonymizer traits stay unknown (null).
        result.IsAnonymousVpn.Should().BeNull();
        result.IsPublicProxy.Should().BeNull();
        result.IsTorExitNode.Should().BeNull();
        result.IsHostingProvider.Should().BeNull();
    }

    [Fact]
    public async Task CityDatabase_PublicAddressOutsideTheDatabase_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider(
            CityDatabasePath, NullLogger<MaxMindGeolocationProvider>.Instance);

        // TEST-NET-3 documentation range: never present in MaxMind data. A lookup miss must
        // resolve to null (Unknown), not throw and not produce a hollow partial result.
        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AnonymousDatabase_VpnAddress_ResolvesAnonymousTraitsWithoutLocation()
    {
        using var provider = new MaxMindGeolocationProvider(
            (string?)null, NullLogger<MaxMindGeolocationProvider>.Instance, AnonymousDatabasePath);

        // 1.2.0.1 is MaxMind's canonical Anonymous-IP VPN test record.
        var result = await provider.ResolveAsync("1.2.0.1");

        result.Should().NotBeNull();
        result!.IsAnonymousVpn.Should().BeTrue();
        result.IsPublicProxy.Should().BeFalse();
        result.IsTorExitNode.Should().BeFalse();
        result.IsHostingProvider.Should().BeFalse();
        // No city database configured: location fields stay unknown.
        result.CountryCode.Should().BeNull();
        result.City.Should().BeNull();
    }

    [Fact]
    public async Task AnonymousDatabase_AllTraitsAddress_ReportsEveryTrait()
    {
        using var provider = new MaxMindGeolocationProvider(
            (string?)null, NullLogger<MaxMindGeolocationProvider>.Instance, AnonymousDatabasePath);

        // 81.2.69.0/24 carries the all-true anonymous record in MaxMind's test data.
        var result = await provider.ResolveAsync("81.2.69.1");

        result.Should().NotBeNull();
        result!.IsAnonymousVpn.Should().BeTrue();
        result.IsPublicProxy.Should().BeTrue();
        result.IsTorExitNode.Should().BeTrue();
        result.IsHostingProvider.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousDatabase_KnownNonAnonymousAddressWithoutCityData_ResolvesToNull()
    {
        using var provider = new MaxMindGeolocationProvider(
            (string?)null, NullLogger<MaxMindGeolocationProvider>.Instance, AnonymousDatabasePath);

        // Listed nowhere (city data absent, address not anonymous): a miss, not a partial result.
        var result = await provider.ResolveAsync("203.0.113.25");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CityAndAnonymousDatabases_CombineLocationAndTraits()
    {
        using var provider = new MaxMindGeolocationProvider(
            CityDatabasePath, NullLogger<MaxMindGeolocationProvider>.Instance, AnonymousDatabasePath);

        // London city record (81.2.69.160) inside the all-traits 81.2.69.0/24 anonymous record.
        var result = await provider.ResolveAsync("81.2.69.160");

        result.Should().NotBeNull();
        result!.City.Should().Be("London");
        result.CountryCode.Should().Be("GB");
        result.IsAnonymousVpn.Should().BeTrue();
        result.IsPublicProxy.Should().BeTrue();
        result.IsTorExitNode.Should().BeTrue();
        result.IsHostingProvider.Should().BeTrue();
    }

    [Fact]
    public async Task CityDatabase_Ipv4MappedIpv6Address_ResolvesLikeTheIpv4Form()
    {
        using var provider = new MaxMindGeolocationProvider(
            CityDatabasePath, NullLogger<MaxMindGeolocationProvider>.Instance);

        // ::ffff:81.2.69.160 must normalize to 81.2.69.160 before the lookup.
        var result = await provider.ResolveAsync("::ffff:81.2.69.160");

        result.Should().NotBeNull();
        result!.City.Should().Be("London");
        result.CountryCode.Should().Be("GB");
    }
}
