using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the MfaOptions configuration
/// Tests the properties and behavior of MFA configuration options
/// </summary>
public class MfaOptionsTests
{
    [Fact]
    public void MfaOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new MfaOptions();

        // Assert
        options.Issuer.Should().Be("GameGuild");
        options.MaxFailedAttempts.Should().Be(5);
        options.LockoutDurationMinutes.Should().Be(15);
        options.BackupCodeCount.Should().Be(10);
        options.TotpWindowSeconds.Should().Be(30);
    }

    [Fact]
    public void MfaOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new MfaOptions
        {
            Issuer = "CustomIssuer",
            MaxFailedAttempts = 3,
            LockoutDurationMinutes = 30,
            BackupCodeCount = 5,
            TotpWindowSeconds = 60
        };

        // Assert
        options.Issuer.Should().Be("CustomIssuer");
        options.MaxFailedAttempts.Should().Be(3);
        options.LockoutDurationMinutes.Should().Be(30);
        options.BackupCodeCount.Should().Be(5);
        options.TotpWindowSeconds.Should().Be(60);
    }

    [Fact]
    public void MfaOptions_Should_Support_Security_Configuration()
    {
        // Arrange & Act
        var strictOptions = new MfaOptions
        {
            MaxFailedAttempts = 2,
            LockoutDurationMinutes = 60
        };

        // Assert
        strictOptions.MaxFailedAttempts.Should().BeLessThan(5);
        strictOptions.LockoutDurationMinutes.Should().BeGreaterThan(15);
    }
}
