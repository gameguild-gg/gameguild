using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the UserEnumerationProtectionOptions configuration
/// Tests the properties and behavior of user enumeration protection configuration
/// </summary>
public class UserEnumerationProtectionOptionsTests
{
    [Fact]
    public void UserEnumerationProtectionOptions_Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new UserEnumerationProtectionOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.MinProcessingTimeMs.Should().Be(200);
        options.MaxProcessingTimeMs.Should().Be(800);
        options.TargetProcessingTimeMs.Should().Be(400);
        options.LogTimingAnalysis.Should().BeFalse();
        options.CustomErrorMessage.Should().BeNull();
        options.PerformDummyHashing.Should().BeTrue();
        options.DelayVarianceMs.Should().Be(100);
    }

    [Fact]
    public void UserEnumerationProtectionOptions_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var options = new UserEnumerationProtectionOptions
        {
            Enabled = false,
            MinProcessingTimeMs = 100,
            MaxProcessingTimeMs = 1000,
            TargetProcessingTimeMs = 500,
            LogTimingAnalysis = true,
            CustomErrorMessage = "Invalid credentials",
            PerformDummyHashing = false,
            DelayVarianceMs = 200
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.MinProcessingTimeMs.Should().Be(100);
        options.MaxProcessingTimeMs.Should().Be(1000);
        options.TargetProcessingTimeMs.Should().Be(500);
        options.LogTimingAnalysis.Should().BeTrue();
        options.CustomErrorMessage.Should().Be("Invalid credentials");
        options.PerformDummyHashing.Should().BeFalse();
        options.DelayVarianceMs.Should().Be(200);
    }

    [Fact]
    public void UserEnumerationProtectionOptions_Should_Validate_Timing_Ranges()
    {
        // Arrange & Act
        var options = new UserEnumerationProtectionOptions
        {
            MinProcessingTimeMs = 200,
            TargetProcessingTimeMs = 400,
            MaxProcessingTimeMs = 800
        };

        // Assert
        options.MinProcessingTimeMs.Should().BeLessThan(options.TargetProcessingTimeMs);
        options.TargetProcessingTimeMs.Should().BeLessThan(options.MaxProcessingTimeMs);
    }

    [Fact]
    public void UserEnumerationProtectionOptions_Should_Support_Security_Configuration()
    {
        // Arrange & Act
        var strictOptions = new UserEnumerationProtectionOptions
        {
            Enabled = true,
            PerformDummyHashing = true,
            LogTimingAnalysis = true,
            DelayVarianceMs = 150
        };

        // Assert
        strictOptions.Enabled.Should().BeTrue();
        strictOptions.PerformDummyHashing.Should().BeTrue();
        strictOptions.LogTimingAnalysis.Should().BeTrue();
        strictOptions.DelayVarianceMs.Should().BeGreaterThan(0);
    }
}
