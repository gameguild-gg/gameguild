using System.Reflection;
using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class WebAuthnRegistrationServiceFriendlyNameTests
{
    [Theory]
    [InlineData(WebAuthnAuthenticatorType.Platform, "Mozilla Windows", "Windows Hello")]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, "Mozilla Windows", "Security Key")]
    [InlineData(WebAuthnAuthenticatorType.Platform, "Mozilla Mac", "Touch ID")]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, "Mozilla Mac", "Security Key")]
    [InlineData(WebAuthnAuthenticatorType.Platform, "Mozilla iPhone", "Face ID / Touch ID")]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, "Mozilla iPad", "Face ID / Touch ID")]
    [InlineData(WebAuthnAuthenticatorType.Platform, "Mozilla Android", "Android Biometric")]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, "Mozilla Android", "Security Key")]
    [InlineData(WebAuthnAuthenticatorType.Platform, "Mozilla Linux", "Built-in Authenticator")]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, null, "Security Key")]
    public void GetDefaultFriendlyName_ShouldMapPlatformAndUserAgent(WebAuthnAuthenticatorType type, string? userAgent, string expected)
    {
        var method = typeof(WebAuthnRegistrationService).GetMethod(
            "GetDefaultFriendlyName",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var result = method!.Invoke(null, [type, userAgent]);

        result.Should().Be(expected);
    }
}