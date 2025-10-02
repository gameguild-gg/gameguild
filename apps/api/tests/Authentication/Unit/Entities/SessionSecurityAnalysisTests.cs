using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the SessionSecurityAnalysis entity
/// Tests the properties and behavior of session security analysis results
/// </summary>
public class SessionSecurityAnalysisTests
{
    [Fact]
    public void SessionSecurityAnalysis_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var analysis = new SessionSecurityAnalysis();

        // Assert
        analysis.IsNewLocation.Should().BeFalse();
        analysis.IsNewDevice.Should().BeFalse();
        analysis.RecentLocationCount.Should().Be(0);
        analysis.RecentDeviceCount.Should().Be(0);
        analysis.LastSeenAt.Should().BeNull();
        analysis.RiskScore.Should().Be(RiskLevel.Low);
    }

    [Fact]
    public void SessionSecurityAnalysis_Should_Detect_New_Location_And_Device()
    {
        // Arrange & Act
        var analysis = new SessionSecurityAnalysis
        {
            IsNewLocation = true,
            IsNewDevice = true,
            RecentLocationCount = 1,
            RecentDeviceCount = 1,
            RiskScore = RiskLevel.Medium
        };

        // Assert
        analysis.IsNewLocation.Should().BeTrue();
        analysis.IsNewDevice.Should().BeTrue();
        analysis.RecentLocationCount.Should().Be(1);
        analysis.RecentDeviceCount.Should().Be(1);
        analysis.RiskScore.Should().Be(RiskLevel.Medium);
    }

    [Fact]
    public void SessionSecurityAnalysis_Should_Track_Recent_Activity()
    {
        // Arrange & Act
        var lastSeen = DateTime.UtcNow.AddDays(-5);
        var analysis = new SessionSecurityAnalysis
        {
            RecentLocationCount = 3,
            RecentDeviceCount = 2,
            LastSeenAt = lastSeen
        };

        // Assert
        analysis.RecentLocationCount.Should().Be(3);
        analysis.RecentDeviceCount.Should().Be(2);
        analysis.LastSeenAt.Should().Be(lastSeen);
    }

    [Fact]
    public void SessionSecurityAnalysis_Should_Support_High_Risk_Scenarios()
    {
        // Arrange & Act
        var analysis = new SessionSecurityAnalysis
        {
            IsNewLocation = true,
            IsNewDevice = true,
            RecentLocationCount = 10,
            RecentDeviceCount = 8,
            LastSeenAt = DateTime.UtcNow.AddMonths(-6),
            RiskScore = RiskLevel.High
        };

        // Assert
        analysis.RiskScore.Should().Be(RiskLevel.High);
        analysis.IsNewLocation.Should().BeTrue();
        analysis.IsNewDevice.Should().BeTrue();
        analysis.RecentLocationCount.Should().BeGreaterThan(5);
        analysis.RecentDeviceCount.Should().BeGreaterThan(5);
    }
}
