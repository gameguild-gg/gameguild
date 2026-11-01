using System.Net;
using FluentAssertions;
using GameGuild.Notifications.Services.WhatsApp;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.WhatsApp;

/// <summary>
///     Fail-closed default tests (issues #54/#55): while the channel is unconfigured the
///     NoOp provider reports itself unavailable and declines every operation with
///     WhatsApp.NotConfigured — never a silent success.
/// </summary>
public sealed class NoOpWhatsAppProviderTests
{
    [Fact]
    public void IsConfigured_IsFalse()
    {
        NoOpWhatsAppProvider.Instance.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task EveryOperation_FailsWithNotConfigured()
    {
        var provider = NoOpWhatsAppProvider.Instance;

        var send = await provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));
        var template = await provider.SendTemplateAsync(new WhatsAppTemplateMessage("+15551234567", "t", null, null));
        var verify = await provider.VerifyNumberAsync("+15551234567");
        var status = await provider.GetMessageStatusAsync("wamid.TEST");

        send.IsFailure.Should().BeTrue();
        template.IsFailure.Should().BeTrue();
        verify.IsFailure.Should().BeTrue();
        status.IsFailure.Should().BeTrue();

        send.Error.Code.Should().Be("WhatsApp.NotConfigured");
        template.Error.Code.Should().Be("WhatsApp.NotConfigured");
        verify.Error.Code.Should().Be("WhatsApp.NotConfigured");
        status.Error.Code.Should().Be("WhatsApp.NotConfigured");
    }

    [Fact]
    public async Task NotConfiguredError_NamesTheEnvironmentVariables()
    {
        var result = await NoOpWhatsAppProvider.Instance.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));

        result.Error.Description.Should().Contain(WhatsAppSettings.AppIdEnvironmentVariable);
        result.Error.Description.Should().Contain(WhatsAppSettings.PhoneNumberIdEnvironmentVariable);
        result.Error.Description.Should().Contain(WhatsAppSettings.AccessTokenEnvironmentVariable);
    }
}

/// <summary>
///     Settings + credential resolution tests: enabled-by-default is false, credentials
///     resolve only from the environment, partial credentials never half-configure the
///     adapter, and the allowlist gates template sends.
/// </summary>
public sealed class WhatsAppSettingsTests
{
    [Fact]
    public void DefaultSettings_AreDisabled()
    {
        var settings = new WhatsAppSettings();

        settings.Enabled.Should().BeFalse();
        settings.ApiBaseUrl.Should().Be("https://graph.facebook.com");
        settings.TemplateAllowlist.Should().BeEmpty();
    }

    [Fact]
    public void FromEnvironment_ReturnsNullWhenAnyVariableIsMissing()
    {
        WhatsAppCredentials.FromEnvironment(_ => null).Should().BeNull();
        WhatsAppCredentials.FromEnvironment(name => name == WhatsAppSettings.AppIdEnvironmentVariable ? "app" : null).Should().BeNull();
        WhatsAppCredentials.FromEnvironment(name => name == WhatsAppSettings.PhoneNumberIdEnvironmentVariable ? "phone" : null).Should().BeNull();
        WhatsAppCredentials.FromEnvironment(name => name == WhatsAppSettings.AccessTokenEnvironmentVariable ? "token" : null).Should().BeNull();
    }

    [Fact]
    public void FromEnvironment_ReadsAllThreeVariables()
    {
        var credentials = WhatsAppCredentials.FromEnvironment(name => name switch
        {
            WhatsAppSettings.AppIdEnvironmentVariable => " app-id ",
            WhatsAppSettings.PhoneNumberIdEnvironmentVariable => " phone-id ",
            WhatsAppSettings.AccessTokenEnvironmentVariable => " access-token ",
            _ => null
        });

        credentials.Should().NotBeNull();
        credentials!.AppId.Should().Be("app-id");
        credentials.PhoneNumberId.Should().Be("phone-id");
        credentials.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public void TemplateAllowlist_IsCaseInsensitiveAndTrims()
    {
        var settings = new WhatsAppSettings { TemplateAllowlist = ["Campaign_Update"] };

        settings.IsTemplateAllowed("campaign_update").Should().BeTrue();
        settings.IsTemplateAllowed(" campaign_update ").Should().BeTrue();
        settings.IsTemplateAllowed("other").Should().BeFalse();
        settings.IsTemplateAllowed("").Should().BeFalse();
    }

    [Fact]
    public void Validator_FailsWhenEnabledWithoutEnvironmentCredentials()
    {
        var validator = new WhatsAppSettingsValidator();

        var result = validator.Validate(null, new WhatsAppSettings { Enabled = true });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(WhatsAppSettings.AccessTokenEnvironmentVariable);
    }

    [Fact]
    public void Validator_PassesWhenEnabledWithEnvironmentCredentials()
    {
        var validator = new WhatsAppSettingsValidator();
        Environment.SetEnvironmentVariable(WhatsAppSettings.AppIdEnvironmentVariable, "unit-test-app");
        Environment.SetEnvironmentVariable(WhatsAppSettings.PhoneNumberIdEnvironmentVariable, "unit-test-phone");
        Environment.SetEnvironmentVariable(WhatsAppSettings.AccessTokenEnvironmentVariable, "unit-test-token");
        try
        {
            var result = validator.Validate(null, new WhatsAppSettings { Enabled = true });
            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(WhatsAppSettings.AppIdEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(WhatsAppSettings.PhoneNumberIdEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(WhatsAppSettings.AccessTokenEnvironmentVariable, null);
        }
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://graph.facebook.com")]
    public void Validator_FailsOnInvalidBaseUrl(string baseUrl)
    {
        var validator = new WhatsAppSettingsValidator();

        var result = validator.Validate(null, new WhatsAppSettings { ApiBaseUrl = baseUrl });

        result.Failed.Should().BeTrue();
    }
}

/// <summary>
///     Endpoint guard tests (server-side request rules): only public http(s) targets are
///     allowed; loopback, private, link-local, reserved, multicast and documentation
///     addresses are rejected, as are localhost-style host names.
/// </summary>
public sealed class WhatsAppEndpointGuardTests
{
    private static WhatsAppEndpointGuard GuardReturning(params IPAddress[] addresses) => new(_ => addresses);

    [Theory]
    [InlineData("157.240.1.35")]   // public IPv4
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")] // public IPv6
    public void PublicAddresses_AreAllowed(string address)
    {
        WhatsAppEndpointGuard.IsPublicUnicast(IPAddress.Parse(address)).Should().BeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1")]      // loopback
    [InlineData("10.0.0.1")]       // private
    [InlineData("172.16.0.1")]     // private
    [InlineData("172.31.255.255")] // private edge
    [InlineData("192.168.1.1")]    // private
    [InlineData("169.254.169.254")] // link-local metadata
    [InlineData("0.0.0.0")]        // this-network
    [InlineData("100.64.0.1")]     // CGNAT
    [InlineData("192.0.2.1")]      // TEST-NET-1
    [InlineData("198.51.100.1")]   // TEST-NET-2
    [InlineData("203.0.113.1")]    // TEST-NET-3
    [InlineData("240.0.0.1")]      // reserved
    [InlineData("255.255.255.255")] // broadcast
    [InlineData("::1")]            // IPv6 loopback
    [InlineData("::")]             // unspecified
    [InlineData("fe80::1")]        // IPv6 link-local
    [InlineData("fc00::1")]        // IPv6 unique-local
    [InlineData("fd12:3456::1")]   // IPv6 unique-local
    [InlineData("ff02::1")]        // IPv6 multicast
    [InlineData("2001:db8::1")]    // documentation
    [InlineData("::ffff:10.0.0.1")] // IPv4-mapped private
    public void NonPublicAddresses_AreRejected(string address)
    {
        WhatsAppEndpointGuard.IsPublicUnicast(IPAddress.Parse(address)).Should().BeFalse();
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://10.0.0.1/api")]
    [InlineData("ftp://graph.facebook.com")]
    public void ValidateBaseUri_RejectsLocalAndNonHttpTargets(string url)
    {
        // IP-literal hosts resolve to themselves so their address ranges are evaluated.
        var guard = new WhatsAppEndpointGuard(host =>
            IPAddress.TryParse(host, out var literal) ? [literal] : [IPAddress.Parse("157.240.1.35")]);

        var act = () => guard.ValidateBaseUri(new Uri(url, UriKind.Absolute));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateBaseUri_RejectsNamesResolvingToPrivateAddresses()
    {
        var guard = GuardReturning(IPAddress.Parse("192.168.0.10"), IPAddress.Parse("157.240.1.35"));

        var act = () => guard.ValidateBaseUri(new Uri("https://graph.unit-test.example", UriKind.Absolute));

        act.Should().Throw<InvalidOperationException>().WithMessage("*192.168.0.10*");
    }

    [Fact]
    public void ValidateBaseUri_AcceptsPublicHttpTargets()
    {
        var guard = GuardReturning(IPAddress.Parse("157.240.1.35"));

        var act = () => guard.ValidateBaseUri(new Uri("https://graph.facebook.com/v21.0", UriKind.Absolute));

        act.Should().NotThrow();
    }
}
