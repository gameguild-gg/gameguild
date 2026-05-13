using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Entities;

public class ResourceQuotaExtendedTests
{
    [Fact]
    public void GetUsagePercentage_NoHardLimit_ReturnsZero()
    {
        var quota = CreateQuota(hardLimit: null, currentUsage: 100);

        quota.GetUsagePercentage().Should().Be(0);
    }

    [Fact]
    public void GetUsagePercentage_ZeroHardLimit_ReturnsZero()
    {
        var quota = CreateQuota(hardLimit: 0, currentUsage: 100);

        quota.GetUsagePercentage().Should().Be(0);
    }

    [Fact]
    public void GetUsagePercentage_Normal_ReturnsCorrectPercentage()
    {
        var quota = CreateQuota(hardLimit: 200, currentUsage: 100);

        quota.GetUsagePercentage().Should().Be(50.0);
    }

    [Fact]
    public void IsSoftLimitExceeded_NoSoftLimit_ReturnsFalse()
    {
        var quota = CreateQuota(softLimit: null, currentUsage: 1000);

        quota.IsSoftLimitExceeded().Should().BeFalse();
    }

    [Fact]
    public void IsSoftLimitExceeded_BelowLimit_ReturnsFalse()
    {
        var quota = CreateQuota(softLimit: 100, currentUsage: 50);

        quota.IsSoftLimitExceeded().Should().BeFalse();
    }

    [Fact]
    public void IsSoftLimitExceeded_AboveLimit_ReturnsTrue()
    {
        var quota = CreateQuota(softLimit: 100, currentUsage: 101);

        quota.IsSoftLimitExceeded().Should().BeTrue();
    }

    [Fact]
    public void AddUsage_NegativeAmount_ThrowsArgumentException()
    {
        var quota = CreateQuota(currentUsage: 10);
        var act = () => quota.AddUsage(-1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveUsage_NegativeAmount_ThrowsArgumentException()
    {
        var quota = CreateQuota(currentUsage: 10);
        var act = () => quota.RemoveUsage(-1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResetUsage_ClearsCurrentUsageAndUpdatesLastReset()
    {
        var before = DateTime.UtcNow;
        var quota = CreateQuota(currentUsage: 500, lastReset: DateTime.UtcNow.AddDays(-2));

        quota.ResetUsage();

        quota.CurrentUsage.Should().Be(0);
        quota.LastReset.Should().NotBeNull();
        quota.LastReset.Should().BeOnOrAfter(before);
        quota.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Reset_AliasClearsCurrentUsage()
    {
        var quota = CreateQuota(currentUsage: 500);

        quota.Reset();

        quota.CurrentUsage.Should().Be(0);
        quota.LastReset.Should().NotBeNull();
    }

    [Fact]
    public void ShouldReset_NoLastReset_ReturnsTrue()
    {
        var quota = CreateQuota(lastReset: null);

        quota.ShouldReset().Should().BeTrue();
    }

    [Fact]
    public void ShouldReset_WhenNextResetInFuture_ReturnsFalse()
    {
        var quota = CreateQuota(lastReset: DateTime.UtcNow.AddHours(-1), period: ResourceQuotaPeriod.Daily);

        quota.ShouldReset().Should().BeFalse();
    }

    [Fact]
    public void GetNextResetTime_NoLastReset_ReturnsNull()
    {
        var quota = CreateQuota(lastReset: null);

        quota.GetNextResetTime().Should().BeNull();
    }

    [Theory]
    [InlineData(ResourceQuotaPeriod.Daily, 1)]
    [InlineData(ResourceQuotaPeriod.Weekly, 7)]
    public void GetNextResetTime_ForDayBasedPeriods_ReturnsExpectedDate(ResourceQuotaPeriod period, int daysToAdd)
    {
        var lastReset = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var quota = CreateQuota(lastReset: lastReset, period: period);

        var nextReset = quota.GetNextResetTime();

        nextReset.Should().Be(lastReset.AddDays(daysToAdd));
    }

    [Theory]
    [InlineData(ResourceQuotaPeriod.Monthly, 1)]
    [InlineData(ResourceQuotaPeriod.Quarterly, 3)]
    [InlineData(ResourceQuotaPeriod.Yearly, 12)]
    public void GetNextResetTime_ForMonthBasedPeriods_ReturnsExpectedDate(ResourceQuotaPeriod period, int monthsToAdd)
    {
        var lastReset = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var quota = CreateQuota(lastReset: lastReset, period: period);

        var nextReset = quota.GetNextResetTime();

        nextReset.Should().Be(lastReset.AddMonths(monthsToAdd));
    }

    [Fact]
    public void GetNextResetTime_WithPastResetTime_AdjustsBaseBeforeApplyingPeriod()
    {
        var lastReset = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var quota = CreateQuota(lastReset: lastReset, period: ResourceQuotaPeriod.Daily, resetTime: new TimeSpan(8, 0, 0));

        var nextReset = quota.GetNextResetTime();

        nextReset.Should().Be(new DateTime(2026, 1, 17, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetNextResetTime_Unlimited_ReturnsNull()
    {
        var quota = CreateQuota(lastReset: DateTime.UtcNow, period: ResourceQuotaPeriod.Unlimited);

        quota.GetNextResetTime().Should().BeNull();
    }

    private static ResourceQuota CreateQuota(
        long? hardLimit = 10,
        long? softLimit = null,
        long currentUsage = 0,
        DateTime? lastReset = null,
        ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly,
        TimeSpan? resetTime = null)
    {
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Users,
            HardLimit = hardLimit,
            SoftLimit = softLimit,
            CurrentUsage = currentUsage,
            IsActive = true,
            Period = period,
            LastReset = lastReset,
            ResetTime = resetTime
        };

        quota.SetProperties(new Dictionary<string, object?> { ["TenantId"] = Guid.NewGuid() });
        return quota;
    }
}
