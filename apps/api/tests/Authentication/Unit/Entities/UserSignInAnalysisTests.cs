using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the UserSignInAnalysis entity
/// Tests the properties and behavior of user sign-in pattern analysis
/// </summary>
public class UserSignInAnalysisTests
{
    [Fact]
    public void UserSignInAnalysis_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var analysis = new UserSignInAnalysis();

        // Assert
        analysis.UserId.Should().Be(Guid.Empty);
        analysis.IsNewUser.Should().BeFalse();
        analysis.IsNewLocation.Should().BeFalse();
        analysis.IsNewDevice.Should().BeFalse();
        analysis.IsUnusualTime.Should().BeFalse();
        analysis.RecentSuccessfulLogins.Should().Be(0);
        analysis.UniqueLocations.Should().Be(0);
        analysis.UniqueDevices.Should().Be(0);
    }

    [Fact]
    public void UserSignInAnalysis_Should_Track_New_User_Pattern()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var analysis = new UserSignInAnalysis
        {
            UserId = userId,
            IsNewUser = true,
            IsNewLocation = true,
            IsNewDevice = true,
            RecentSuccessfulLogins = 0,
            UniqueLocations = 1,
            UniqueDevices = 1
        };

        // Assert
        analysis.UserId.Should().Be(userId);
        analysis.IsNewUser.Should().BeTrue();
        analysis.IsNewLocation.Should().BeTrue();
        analysis.IsNewDevice.Should().BeTrue();
        analysis.RecentSuccessfulLogins.Should().Be(0);
        analysis.UniqueLocations.Should().Be(1);
        analysis.UniqueDevices.Should().Be(1);
    }

    [Fact]
    public void UserSignInAnalysis_Should_Track_Established_User_Pattern()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var analysis = new UserSignInAnalysis
        {
            UserId = userId,
            IsNewUser = false,
            IsNewLocation = false,
            IsNewDevice = false,
            IsUnusualTime = false,
            RecentSuccessfulLogins = 25,
            UniqueLocations = 3,
            UniqueDevices = 2
        };

        // Assert
        analysis.UserId.Should().Be(userId);
        analysis.IsNewUser.Should().BeFalse();
        analysis.RecentSuccessfulLogins.Should().BeGreaterThan(0);
        analysis.UniqueLocations.Should().BeGreaterThan(1);
        analysis.UniqueDevices.Should().BeGreaterThan(1);
    }

    [Fact]
    public void UserSignInAnalysis_Should_Detect_Suspicious_Patterns()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var analysis = new UserSignInAnalysis
        {
            UserId = userId,
            IsNewLocation = true,
            IsNewDevice = true,
            IsUnusualTime = true,
            RecentSuccessfulLogins = 5,
            UniqueLocations = 10,
            UniqueDevices = 8
        };

        // Assert
        analysis.IsNewLocation.Should().BeTrue();
        analysis.IsNewDevice.Should().BeTrue();
        analysis.IsUnusualTime.Should().BeTrue();
        analysis.UniqueLocations.Should().BeGreaterThan(5);
        analysis.UniqueDevices.Should().BeGreaterThan(5);
    }
}
