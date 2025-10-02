using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the SessionOptions configuration
/// Tests the properties and behavior of session management configuration options
/// </summary>
public class SessionOptionsTests
{
    [Fact]
    public void SessionOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new SessionOptions();

        // Assert
        options.SessionLifetime.Should().Be(TimeSpan.FromHours(24));
        options.TrustedDeviceLifetime.Should().Be(TimeSpan.FromDays(30));
        options.MaxSessionsPerUser.Should().Be(5);
        options.RequireMfaForNewDevice.Should().BeTrue();
        options.RequireMfaForNewLocation.Should().BeFalse();
        options.SessionCleanupInterval.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void SessionOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new SessionOptions
        {
            SessionLifetime = TimeSpan.FromHours(12),
            TrustedDeviceLifetime = TimeSpan.FromDays(60),
            MaxSessionsPerUser = 10,
            RequireMfaForNewDevice = false,
            RequireMfaForNewLocation = true,
            SessionCleanupInterval = TimeSpan.FromMinutes(30)
        };

        // Assert
        options.SessionLifetime.Should().Be(TimeSpan.FromHours(12));
        options.TrustedDeviceLifetime.Should().Be(TimeSpan.FromDays(60));
        options.MaxSessionsPerUser.Should().Be(10);
        options.RequireMfaForNewDevice.Should().BeFalse();
        options.RequireMfaForNewLocation.Should().BeTrue();
        options.SessionCleanupInterval.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void SessionOptions_Should_Allow_Null_TrustedDeviceLifetime()
    {
        // Arrange & Act
        var options = new SessionOptions
        {
            TrustedDeviceLifetime = null
        };

        // Assert
        options.TrustedDeviceLifetime.Should().BeNull();
    }

    [Fact]
    public void SessionOptions_Should_Support_Security_Configuration()
    {
        // Arrange & Act
        var strictOptions = new SessionOptions
        {
            SessionLifetime = TimeSpan.FromHours(2),
            MaxSessionsPerUser = 1,
            RequireMfaForNewDevice = true,
            RequireMfaForNewLocation = true
        };

        // Assert
        strictOptions.SessionLifetime.Should().BeLessThan(TimeSpan.FromHours(24));
        strictOptions.MaxSessionsPerUser.Should().Be(1);
        strictOptions.RequireMfaForNewDevice.Should().BeTrue();
        strictOptions.RequireMfaForNewLocation.Should().BeTrue();
    }
}
