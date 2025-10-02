using FluentAssertions;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Authentication.Configuration;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the AuthenticationSecurityOptions configuration
/// Tests the properties and behavior of authentication security configuration
/// </summary>
public class AuthenticationSecurityOptionsTests
{
    [Fact]
    public void AuthenticationSecurityOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new AuthenticationSecurityOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.Anomaly.Should().NotBeNull();
        options.UserEnumerationProtection.Should().NotBeNull();
        options.RequireHttps.Should().BeTrue();
        options.EnforceStrongPasswords.Should().BeTrue();
        options.RequireEmailVerification.Should().BeTrue();
        options.LogAllAuthEvents.Should().BeTrue();
        options.UseBCryptHashing.Should().BeTrue();
        options.BCryptWorkFactor.Should().Be(12);
    }

    [Fact]
    public void AuthenticationSecurityOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new AuthenticationSecurityOptions
        {
            Enabled = false,
            RequireHttps = false,
            EnforceStrongPasswords = false,
            RequireEmailVerification = false,
            LogAllAuthEvents = false,
            UseBCryptHashing = false,
            BCryptWorkFactor = 10
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.RequireHttps.Should().BeFalse();
        options.EnforceStrongPasswords.Should().BeFalse();
        options.RequireEmailVerification.Should().BeFalse();
        options.LogAllAuthEvents.Should().BeFalse();
        options.UseBCryptHashing.Should().BeFalse();
        options.BCryptWorkFactor.Should().Be(10);
    }

    [Fact]
    public void AuthenticationSecurityOptions_Should_Initialize_Nested_Options()
    {
        // Arrange & Act
        var options = new AuthenticationSecurityOptions();

        // Assert
        options.Anomaly.Should().BeOfType<AuthenticationAnomalyOptions>();
        options.Anomaly.Enabled.Should().BeTrue();
        options.UserEnumerationProtection.Should().BeOfType<UserEnumerationProtectionOptions>();
        options.UserEnumerationProtection.Enabled.Should().BeTrue();
    }

    [Fact]
    public void AuthenticationSecurityOptions_Should_Support_Strict_Security()
    {
        // Arrange & Act
        var strictOptions = new AuthenticationSecurityOptions
        {
            Enabled = true,
            RequireHttps = true,
            EnforceStrongPasswords = true,
            RequireEmailVerification = true,
            LogAllAuthEvents = true,
            UseBCryptHashing = true,
            BCryptWorkFactor = 14
        };

        // Assert
        strictOptions.Enabled.Should().BeTrue();
        strictOptions.RequireHttps.Should().BeTrue();
        strictOptions.EnforceStrongPasswords.Should().BeTrue();
        strictOptions.RequireEmailVerification.Should().BeTrue();
        strictOptions.LogAllAuthEvents.Should().BeTrue();
        strictOptions.UseBCryptHashing.Should().BeTrue();
        strictOptions.BCryptWorkFactor.Should().BeGreaterThan(12);
    }

    [Fact]
    public void AuthenticationSecurityOptions_Should_Allow_Custom_Nested_Options()
    {
        // Arrange & Act
        var options = new AuthenticationSecurityOptions
        {
            Anomaly = new AuthenticationAnomalyOptions { Enabled = false },
            UserEnumerationProtection = new UserEnumerationProtectionOptions { Enabled = false }
        };

        // Assert
        options.Anomaly.Enabled.Should().BeFalse();
        options.UserEnumerationProtection.Enabled.Should().BeFalse();
    }
}
