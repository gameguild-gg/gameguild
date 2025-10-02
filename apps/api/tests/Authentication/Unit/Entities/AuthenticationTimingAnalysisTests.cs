using FluentAssertions;
using GameGuild.Modules.Authentication.Services;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the AuthenticationTimingAnalysis entity
/// Tests the properties and behavior of authentication timing analysis
/// </summary>
public class AuthenticationTimingAnalysisTests
{
    [Fact]
    public void AuthenticationTimingAnalysis_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var analysis = new AuthenticationTimingAnalysis();

        // Assert
        analysis.EmailHash.Should().BeEmpty();
        analysis.UserExists.Should().BeFalse();
        analysis.ActualProcessingTime.Should().Be(TimeSpan.Zero);
        analysis.TargetProcessingTime.Should().Be(TimeSpan.Zero);
        analysis.TimingDeviation.Should().Be(TimeSpan.Zero);
        analysis.Timestamp.Should().Be(default);
        analysis.IpAddress.Should().BeEmpty();
    }

    [Fact]
    public void AuthenticationTimingAnalysis_Should_Track_Timing_Metrics()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var actual = TimeSpan.FromMilliseconds(450);
        var target = TimeSpan.FromMilliseconds(400);
        var deviation = TimeSpan.FromMilliseconds(50);

        // Act
        var analysis = new AuthenticationTimingAnalysis
        {
            EmailHash = "abc123",
            UserExists = true,
            ActualProcessingTime = actual,
            TargetProcessingTime = target,
            TimingDeviation = deviation,
            Timestamp = timestamp,
            IpAddress = "192.168.1.1"
        };

        // Assert
        analysis.EmailHash.Should().Be("abc123");
        analysis.UserExists.Should().BeTrue();
        analysis.ActualProcessingTime.Should().Be(actual);
        analysis.TargetProcessingTime.Should().Be(target);
        analysis.TimingDeviation.Should().Be(deviation);
        analysis.Timestamp.Should().Be(timestamp);
        analysis.IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public void AuthenticationTimingAnalysis_Should_Track_Non_Existent_User()
    {
        // Arrange & Act
        var analysis = new AuthenticationTimingAnalysis
        {
            EmailHash = "def456",
            UserExists = false,
            ActualProcessingTime = TimeSpan.FromMilliseconds(420),
            TargetProcessingTime = TimeSpan.FromMilliseconds(400),
            TimingDeviation = TimeSpan.FromMilliseconds(20)
        };

        // Assert
        analysis.UserExists.Should().BeFalse();
        analysis.EmailHash.Should().NotBeEmpty();
        analysis.ActualProcessingTime.Should().BeCloseTo(analysis.TargetProcessingTime, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void AuthenticationTimingAnalysis_Should_Detect_Timing_Anomalies()
    {
        // Arrange & Act
        var analysis = new AuthenticationTimingAnalysis
        {
            ActualProcessingTime = TimeSpan.FromMilliseconds(1200),
            TargetProcessingTime = TimeSpan.FromMilliseconds(400),
            TimingDeviation = TimeSpan.FromMilliseconds(800)
        };

        // Assert
        analysis.TimingDeviation.Should().BeGreaterThan(TimeSpan.FromMilliseconds(200));
        analysis.ActualProcessingTime.Should().BeGreaterThan(analysis.TargetProcessingTime);
    }
}
