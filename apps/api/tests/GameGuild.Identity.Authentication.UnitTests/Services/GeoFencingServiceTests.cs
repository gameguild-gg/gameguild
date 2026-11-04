using FluentAssertions;
using GameGuild;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Geo-fencing policy unit tests (issue #1087): the decision must compose the
///     platform-wide blocked list with the active tenant's additions, fail open on
///     unknown geography unless configured otherwise, and stay inert while disabled.
/// </summary>
public sealed class GeoFencingServiceTests
{
    private readonly Mock<IGeolocationProvider> _provider = new();
    private readonly GeoFencingOptions _options = new();

    private GeoFencingService CreateSut() =>
        new(_provider.Object, Options.Create(_options), NullLogger<GeoFencingService>.Instance);

    private void ResolvesTo(string? countryCode) =>
        _provider.Setup(p => p.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countryCode is null
                ? null
                : new GeolocationResult { CountryCode = countryCode });

    [Fact]
    public async Task Disabled_By_Default_And_Always_Allows()
    {
        ResolvesTo("RU");

        var decision = await CreateSut().EvaluateAsync("203.0.113.10", tenantId: null);

        decision.IsBlocked.Should().BeFalse("the policy ships off and must stay inert until configured");
        _provider.Verify(p => p.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "no geolocation work may happen while the policy is disabled");
    }

    [Fact]
    public async Task Platform_Blocked_Country_Is_Blocked()
    {
        _options.Enabled = true;
        _options.BlockedCountries = ["RU"];
        ResolvesTo("ru"); // matching must be case-insensitive on the resolved code.

        var decision = await CreateSut().EvaluateAsync("203.0.113.10", tenantId: null);

        decision.IsBlocked.Should().BeTrue();
        decision.MatchedCountry.Should().Be("RU");
        decision.CountryUnknown.Should().BeFalse();
    }

    [Fact]
    public async Task Allowed_Country_Passes()
    {
        _options.Enabled = true;
        _options.BlockedCountries = ["RU"];
        ResolvesTo("PT");

        var decision = await CreateSut().EvaluateAsync("203.0.113.10", tenantId: null);

        decision.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Tenant_Additions_Extend_The_Platform_List_Only_For_That_Tenant()
    {
        _options.Enabled = true;
        var tenantId = Guid.NewGuid();
        _options.TenantBlockedCountries[tenantId.ToString()] = ["BR"];
        ResolvesTo("BR");

        (await CreateSut().EvaluateAsync("203.0.113.10", tenantId)).IsBlocked
            .Should().BeTrue("the tenant's additions compose with the platform list");

        (await CreateSut().EvaluateAsync("203.0.113.10", tenantId: Guid.NewGuid())).IsBlocked
            .Should().BeFalse("another tenant must not inherit the tenant-specific list");
    }

    [Fact]
    public async Task Unknown_Country_Fails_Open_By_Default()
    {
        _options.Enabled = true;
        ResolvesTo(null);

        var decision = await CreateSut().EvaluateAsync("192.0.2.7", tenantId: null);

        decision.IsBlocked.Should().BeFalse("a geolocation outage must not lock every user out");
    }

    [Fact]
    public async Task Unknown_Country_Can_Be_Configured_To_Fail_Closed()
    {
        _options.Enabled = true;
        _options.TreatUnknownCountryAsBlocked = true;
        ResolvesTo(null);

        var decision = await CreateSut().EvaluateAsync("192.0.2.7", tenantId: null);

        decision.IsBlocked.Should().BeTrue();
        decision.CountryUnknown.Should().BeTrue();
        decision.MatchedCountry.Should().BeNull();
    }

    [Fact]
    public async Task Missing_Ip_Address_Follows_The_Unknown_Country_Posture()
    {
        _options.Enabled = true;

        var failOpen = await CreateSut().EvaluateAsync(ipAddress: null, tenantId: null);
        _options.TreatUnknownCountryAsBlocked = true;
        var failClosed = await CreateSut().EvaluateAsync(ipAddress: "  ", tenantId: null);

        failOpen.IsBlocked.Should().BeFalse();
        failClosed.IsBlocked.Should().BeTrue();
        _provider.Verify(p => p.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
