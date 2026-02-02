using FluentAssertions;
using Xunit;

namespace GameGuild.Features.UnitTests.Entities;

/// <summary>
/// Unit tests for FeatureFlag entity
/// </summary>
public class FeatureFlagTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrectlySet()
    {
        // Arrange & Act
        var flag = new FeatureFlag();

        // Assert
        flag.Key.Should().BeEmpty();
        flag.Name.Should().BeEmpty();
        flag.Description.Should().BeEmpty();
        flag.IsEnabled.Should().BeFalse();
        flag.Type.Should().Be(FeatureFlagType.Toggle);
        flag.RolloutPercentage.Should().Be(100);
        flag.Environment.Should().Be("production");
        flag.IsKillSwitch.Should().BeFalse();
        flag.RequiresEncryption.Should().BeFalse();
    }

    [Fact]
    public void SetProperties_ShouldUpdateValues()
    {
        // Arrange
        var flag = new FeatureFlag();

        // Act
        flag.Key = "new-feature";
        flag.Name = "New Feature";
        flag.Description = "A new experimental feature";
        flag.IsEnabled = true;
        flag.Type = FeatureFlagType.Percentage;
        flag.RolloutPercentage = 50;
        flag.DefaultValue = "false";
        flag.EnabledValue = "true";

        // Assert
        flag.Key.Should().Be("new-feature");
        flag.Name.Should().Be("New Feature");
        flag.Description.Should().Be("A new experimental feature");
        flag.IsEnabled.Should().BeTrue();
        flag.Type.Should().Be(FeatureFlagType.Percentage);
        flag.RolloutPercentage.Should().Be(50);
        flag.DefaultValue.Should().Be("false");
        flag.EnabledValue.Should().Be("true");
    }

    [Fact]
    public void IsExpired_WhenNoExpirySet_ShouldReturnFalse()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = null };

        // Act & Assert
        flag.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiredInPast_ShouldReturnTrue()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };

        // Act & Assert
        flag.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNotYetExpired_ShouldReturnFalse()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) };

        // Act & Assert
        flag.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsStale_WhenNoReviewDateSet_ShouldReturnFalse()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = null };

        // Act & Assert
        flag.IsStale().Should().BeFalse();
    }

    [Fact]
    public void IsStale_WhenReviewDatePassed_ShouldReturnTrue()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(-1) };

        // Act & Assert
        flag.IsStale().Should().BeTrue();
    }

    [Fact]
    public void IsStale_WhenReviewDateFuture_ShouldReturnFalse()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(30) };

        // Act & Assert
        flag.IsStale().Should().BeFalse();
    }

    [Fact]
    public void GetDaysUntilExpiration_WhenNoExpirySet_ShouldReturnNull()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = null };

        // Act & Assert
        flag.GetDaysUntilExpiration().Should().BeNull();
    }

    [Fact]
    public void GetDaysUntilExpiration_WhenExpired_ShouldReturnZero()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-5) };

        // Act & Assert
        flag.GetDaysUntilExpiration().Should().Be(0);
    }

    [Fact]
    public void GetDaysUntilExpiration_WhenFuture_ShouldReturnCorrectDays()
    {
        // Arrange
        var flag = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(10) };

        // Act & Assert
        flag.GetDaysUntilExpiration().Should().BeInRange(9, 10);
    }

    [Fact]
    public void GetDaysUntilReview_WhenNoReviewDateSet_ShouldReturnNull()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = null };

        // Act & Assert
        flag.GetDaysUntilReview().Should().BeNull();
    }

    [Fact]
    public void GetDaysUntilReview_WhenReviewPassed_ShouldReturnZero()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(-10) };

        // Act & Assert
        flag.GetDaysUntilReview().Should().Be(0);
    }

    [Fact]
    public void GetDaysUntilReview_WhenFuture_ShouldReturnCorrectDays()
    {
        // Arrange
        var flag = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(15) };

        // Act & Assert
        flag.GetDaysUntilReview().Should().BeInRange(14, 15);
    }

    [Fact]
    public void KillSwitch_ShouldBeConfigurable()
    {
        // Arrange
        var flag = new FeatureFlag
        {
            Key = "emergency-shutdown",
            Name = "Emergency Shutdown",
            IsKillSwitch = true,
            IsEnabled = false
        };

        // Assert
        flag.IsKillSwitch.Should().BeTrue();
    }

    [Fact]
    public void Environment_ShouldAcceptDifferentValues()
    {
        // Arrange
        var flag = new FeatureFlag();

        // Act & Assert
        flag.Environment = "development";
        flag.Environment.Should().Be("development");

        flag.Environment = "staging";
        flag.Environment.Should().Be("staging");

        flag.Environment = "production";
        flag.Environment.Should().Be("production");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void RolloutPercentage_ShouldAcceptValidRange(int percentage)
    {
        // Arrange
        var flag = new FeatureFlag();

        // Act
        flag.RolloutPercentage = percentage;

        // Assert
        flag.RolloutPercentage.Should().Be(percentage);
    }

    [Fact]
    public void RequiresEncryption_ShouldBeConfigurable()
    {
        // Arrange
        var flag = new FeatureFlag
        {
            Key = "sensitive-feature",
            RequiresEncryption = true
        };

        // Assert
        flag.RequiresEncryption.Should().BeTrue();
    }

    [Fact]
    public void Owner_ShouldBeSettable()
    {
        // Arrange
        var flag = new FeatureFlag();

        // Act
        flag.Owner = "Platform Team";
        flag.EscalationContact = "platform-team@example.com";

        // Assert
        flag.Owner.Should().Be("Platform Team");
        flag.EscalationContact.Should().Be("platform-team@example.com");
    }

    [Fact]
    public void Collections_ShouldBeInitialized()
    {
        // Arrange & Act
        var flag = new FeatureFlag();

        // Assert
        flag.Targets.Should().NotBeNull();
        flag.Targets.Should().BeEmpty();
        flag.UsageAnalytics.Should().NotBeNull();
        flag.UsageAnalytics.Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for FeatureFlagType enum
/// </summary>
public class FeatureFlagTypeTests
{
    [Theory]
    [InlineData(FeatureFlagType.Toggle)]
    [InlineData(FeatureFlagType.Numeric)]
    [InlineData(FeatureFlagType.String)]
    [InlineData(FeatureFlagType.Percentage)]
    [InlineData(FeatureFlagType.UserSegment)]
    public void FeatureFlagType_ShouldHaveExpectedValues(FeatureFlagType type)
    {
        // Assert
        Enum.IsDefined(typeof(FeatureFlagType), type).Should().BeTrue();
    }
}
