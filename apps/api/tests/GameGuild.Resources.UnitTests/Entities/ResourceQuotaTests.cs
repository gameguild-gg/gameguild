using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Entities;

public class ResourceQuotaTests
{
    [Fact]
    public void RemoveUsage_NeverGoesNegative_WhenAmountExceedsUsage()
    {
        // Arrange
        var quota = CreateQuota(currentUsage: 5);

        // Act
        quota.RemoveUsage(10); // Try to remove more than current usage

        // Assert
        quota.CurrentUsage.Should().Be(0, "usage should be clamped to 0, never negative");
    }

    [Fact]
    public void RemoveUsage_DecreasesUsageCorrectly()
    {
        // Arrange
        var quota = CreateQuota(currentUsage: 10);

        // Act
        quota.RemoveUsage(3);

        // Assert
        quota.CurrentUsage.Should().Be(7, "usage should decrease from 10 to 7");
    }

    [Fact]
    public void RemoveUsage_SetsToZero_WhenExactlyMatches()
    {
        // Arrange
        var quota = CreateQuota(currentUsage: 5);

        // Act
        quota.RemoveUsage(5);

        // Assert
        quota.CurrentUsage.Should().Be(0, "usage should be exactly 0");
    }

    [Fact]
    public void AddUsage_IncreasesUsageCorrectly()
    {
        // Arrange
        var quota = CreateQuota(currentUsage: 5);

        // Act
        quota.AddUsage(3);

        // Assert
        quota.CurrentUsage.Should().Be(8, "usage should increase from 5 to 8");
    }

    [Fact]
    public void IsHardLimitExceeded_ReturnsTrue_WhenUsageEqualsLimit()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 10);

        // Act
        var exceeded = quota.IsHardLimitExceeded();

        // Assert
        exceeded.Should().BeTrue("usage at exactly the hard limit is considered exceeded");
    }

    [Fact]
    public void IsHardLimitExceeded_ReturnsTrue_WhenUsageExceedsLimit()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 11);

        // Act
        var exceeded = quota.IsHardLimitExceeded();

        // Assert
        exceeded.Should().BeTrue();
    }

    [Fact]
    public void IsHardLimitExceeded_ReturnsFalse_WhenBelowLimit()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 9);

        // Act
        var exceeded = quota.IsHardLimitExceeded();

        // Assert
        exceeded.Should().BeFalse();
    }

    [Fact]
    public void IsHardLimitExceeded_ReturnsFalse_WhenNoLimitSet()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: null, currentUsage: 1000);

        // Act
        var exceeded = quota.IsHardLimitExceeded();

        // Assert
        exceeded.Should().BeFalse("no hard limit means never exceeded");
    }

    [Fact]
    public void GetRemainingQuota_ReturnsCorrectValue()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 7);

        // Act
        var remaining = quota.GetRemainingQuota();

        // Assert
        remaining.Should().Be(3, "10 - 7 = 3 remaining");
    }

    [Fact]
    public void GetRemainingQuota_ReturnsZero_WhenAtLimit()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 10);

        // Act
        var remaining = quota.GetRemainingQuota();

        // Assert
        remaining.Should().Be(0);
    }

    [Fact]
    public void GetRemainingQuota_ReturnsZero_WhenOverLimit()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: 10, currentUsage: 12);

        // Act
        var remaining = quota.GetRemainingQuota();

        // Assert
        remaining.Should().Be(0, "remaining should never be negative");
    }

    [Fact]
    public void GetRemainingQuota_ReturnsMaxValue_WhenNoLimitSet()
    {
        // Arrange
        var quota = CreateQuota(hardLimit: null, currentUsage: 100);

        // Act
        var remaining = quota.GetRemainingQuota();

        // Assert
        remaining.Should().Be(long.MaxValue, "no limit means effectively unlimited");
    }

    private ResourceQuota CreateQuota(long? hardLimit = null, long currentUsage = 0)
    {
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Users,
            HardLimit = hardLimit,
            CurrentUsage = currentUsage,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            LastReset = DateTime.UtcNow.AddDays(-1)
        };
        quota.SetProperties(new Dictionary<string, object?> { ["TenantId"] = Guid.NewGuid() });
        return quota;
    }
}
