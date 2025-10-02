using FluentAssertions;
using GameGuild.Modules.Authentication.Configuration;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the AuthenticationAnomalyOptions configuration
/// Tests the properties and behavior of authentication anomaly detection configuration
/// </summary>
public class AuthenticationAnomalyOptionsTests
{
    [Fact]
    public void AuthenticationAnomalyOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new AuthenticationAnomalyOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.MaxFailedAttemptsPerHour.Should().Be(5);
        options.MaxFailedAttemptsPerDay.Should().Be(20);
        options.MaxAttemptsPerIpPerHour.Should().Be(50);
        options.SuspiciousThreshold.Should().Be(30);
        options.ThrottleMinutes.Should().Be(15);
        options.LogTimingAnalysis.Should().BeFalse();
        options.AutoBlockSuspiciousIps.Should().BeFalse();
        options.AutoBlockThreshold.Should().Be(80);
        options.AutoBlockDurationHours.Should().Be(24);
    }

    [Fact]
    public void AuthenticationAnomalyOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new AuthenticationAnomalyOptions
        {
            Enabled = false,
            MaxFailedAttemptsPerHour = 3,
            MaxFailedAttemptsPerDay = 10,
            MaxAttemptsPerIpPerHour = 25,
            SuspiciousThreshold = 50,
            ThrottleMinutes = 30,
            LogTimingAnalysis = true,
            AutoBlockSuspiciousIps = true,
            AutoBlockThreshold = 90,
            AutoBlockDurationHours = 48
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.MaxFailedAttemptsPerHour.Should().Be(3);
        options.MaxFailedAttemptsPerDay.Should().Be(10);
        options.MaxAttemptsPerIpPerHour.Should().Be(25);
        options.SuspiciousThreshold.Should().Be(50);
        options.ThrottleMinutes.Should().Be(30);
        options.LogTimingAnalysis.Should().BeTrue();
        options.AutoBlockSuspiciousIps.Should().BeTrue();
        options.AutoBlockThreshold.Should().Be(90);
        options.AutoBlockDurationHours.Should().Be(48);
    }

    [Fact]
    public void AuthenticationAnomalyOptions_Should_Support_Strict_Security()
    {
        // Arrange & Act
        var strictOptions = new AuthenticationAnomalyOptions
        {
            Enabled = true,
            MaxFailedAttemptsPerHour = 2,
            MaxFailedAttemptsPerDay = 5,
            SuspiciousThreshold = 20,
            AutoBlockSuspiciousIps = true,
            AutoBlockThreshold = 50
        };

        // Assert
        strictOptions.Enabled.Should().BeTrue();
        strictOptions.MaxFailedAttemptsPerHour.Should().BeLessThan(5);
        strictOptions.MaxFailedAttemptsPerDay.Should().BeLessThan(10);
        strictOptions.AutoBlockSuspiciousIps.Should().BeTrue();
        strictOptions.AutoBlockThreshold.Should().BeLessThan(80);
    }

    [Fact]
    public void AuthenticationAnomalyOptions_Should_Validate_Threshold_Relationships()
    {
        // Arrange & Act
        var options = new AuthenticationAnomalyOptions
        {
            SuspiciousThreshold = 30,
            AutoBlockThreshold = 80
        };

        // Assert
        options.AutoBlockThreshold.Should().BeGreaterThan(options.SuspiciousThreshold);
    }

    [Fact]
    public void AuthenticationAnomalyOptions_Should_Validate_Attempt_Limits()
    {
        // Arrange & Act
        var options = new AuthenticationAnomalyOptions
        {
            MaxFailedAttemptsPerHour = 5,
            MaxFailedAttemptsPerDay = 20
        };

        // Assert
        options.MaxFailedAttemptsPerDay.Should().BeGreaterThan(options.MaxFailedAttemptsPerHour);
    }
}
