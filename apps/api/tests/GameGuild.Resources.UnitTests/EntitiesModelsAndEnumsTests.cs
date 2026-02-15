using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests;

#region Entity Tests — ResourceQuota extended

public class ResourceQuotaExtendedTests
{
    [Fact]
    public void GetUsagePercentage_NoHardLimit_ReturnsZero()
    {
        var quota = new ResourceQuota { HardLimit = null, CurrentUsage = 100 };
        quota.GetUsagePercentage().Should().Be(0);
    }

    [Fact]
    public void GetUsagePercentage_ZeroHardLimit_ReturnsZero()
    {
        var quota = new ResourceQuota { HardLimit = 0, CurrentUsage = 100 };
        quota.GetUsagePercentage().Should().Be(0);
    }

    [Fact]
    public void GetUsagePercentage_Normal_ReturnsCorrectPercentage()
    {
        var quota = new ResourceQuota { HardLimit = 200, CurrentUsage = 100 };
        quota.GetUsagePercentage().Should().Be(50.0);
    }

    [Fact]
    public void IsSoftLimitExceeded_NoSoftLimit_ReturnsFalse()
    {
        var quota = new ResourceQuota { SoftLimit = null, CurrentUsage = 1000 };
        quota.IsSoftLimitExceeded().Should().BeFalse();
    }

    [Fact]
    public void IsSoftLimitExceeded_BelowLimit_ReturnsFalse()
    {
        var quota = new ResourceQuota { SoftLimit = 100, CurrentUsage = 50 };
        quota.IsSoftLimitExceeded().Should().BeFalse();
    }

    [Fact]
    public void IsSoftLimitExceeded_AboveLimit_ReturnsTrue()
    {
        var quota = new ResourceQuota { SoftLimit = 100, CurrentUsage = 101 };
        quota.IsSoftLimitExceeded().Should().BeTrue();
    }

    [Fact]
    public void AddUsage_NegativeAmount_ShouldThrow()
    {
        var quota = new ResourceQuota { CurrentUsage = 10 };
        var act = () => quota.AddUsage(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveUsage_NegativeAmount_ShouldThrow()
    {
        var quota = new ResourceQuota { CurrentUsage = 10 };
        var act = () => quota.RemoveUsage(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResetUsage_ShouldClearCurrentUsage()
    {
        var quota = new ResourceQuota { CurrentUsage = 500 };
        quota.ResetUsage();
        quota.CurrentUsage.Should().Be(0);
        quota.LastReset.Should().NotBeNull();
    }

    [Fact]
    public void Reset_ShouldClearCurrentUsage()
    {
        var quota = new ResourceQuota { CurrentUsage = 500 };
        quota.Reset();
        quota.CurrentUsage.Should().Be(0);
    }

    [Fact]
    public void ShouldReset_NoLastReset_ReturnsTrue()
    {
        var quota = new ResourceQuota { LastReset = null };
        quota.ShouldReset().Should().BeTrue();
    }

    [Fact]
    public void GetNextResetTime_NoLastReset_ReturnsNull()
    {
        var quota = new ResourceQuota { LastReset = null };
        quota.GetNextResetTime().Should().BeNull();
    }

    [Fact]
    public void GetNextResetTime_Daily_ReturnsNextDay()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var quota = new ResourceQuota { LastReset = now, Period = ResourceQuotaPeriod.Daily };
        var next = quota.GetNextResetTime();
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetNextResetTime_Weekly_ReturnsNext7Days()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var quota = new ResourceQuota { LastReset = now, Period = ResourceQuotaPeriod.Weekly };
        var next = quota.GetNextResetTime();
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetNextResetTime_Monthly_ReturnsNextMonth()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var quota = new ResourceQuota { LastReset = now, Period = ResourceQuotaPeriod.Monthly };
        var next = quota.GetNextResetTime();
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetNextResetTime_Quarterly_Returns3Months()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var quota = new ResourceQuota { LastReset = now, Period = ResourceQuotaPeriod.Quarterly };
        var next = quota.GetNextResetTime();
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetNextResetTime_Yearly_Returns1Year()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var quota = new ResourceQuota { LastReset = now, Period = ResourceQuotaPeriod.Yearly };
        var next = quota.GetNextResetTime();
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetNextResetTime_Unlimited_ReturnsNull()
    {
        var quota = new ResourceQuota { LastReset = DateTime.UtcNow, Period = ResourceQuotaPeriod.Unlimited };
        quota.GetNextResetTime().Should().BeNull();
    }
}

#endregion

#region Entity Tests — SlaImpactAnalysis

public class SlaImpactAnalysisTests
{
    [Fact]
    public void CalculateDuration_WithEndTime_ShouldSetDuration()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-2),
            ViolationEndTime = DateTime.UtcNow
        };
        analysis.CalculateDuration();
        analysis.DurationSeconds.Should().BeCloseTo(7200, 60);
    }

    [Fact]
    public void CalculateDuration_WithoutEndTime_ShouldNotSet()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-2),
            ViolationEndTime = null,
            DurationSeconds = 0
        };
        analysis.CalculateDuration();
        analysis.DurationSeconds.Should().Be(0);
    }

    [Fact]
    public void CalculateDeviation_NormalValues()
    {
        var analysis = new SlaImpactAnalysis { ExpectedValue = 100, ActualValue = 120 };
        analysis.CalculateDeviation();
        analysis.DeviationPercentage.Should().Be(20.00m);
    }

    [Fact]
    public void CalculateDeviation_ZeroExpected_ShouldNotChange()
    {
        var analysis = new SlaImpactAnalysis { ExpectedValue = 0, ActualValue = 50 };
        analysis.CalculateDeviation();
        analysis.DeviationPercentage.Should().Be(0);
    }

    [Fact]
    public void Resolve_ShouldSetAllFields()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-1),
            ViolationEndTime = null
        };
        var userId = Guid.NewGuid();

        analysis.Resolve(userId, "fixed the issue");

        analysis.IsResolved.Should().BeTrue();
        analysis.ResolvedByUserId.Should().Be(userId);
        analysis.ResolvedAt.Should().NotBeNull();
        analysis.ViolationEndTime.Should().NotBeNull();
        analysis.MitigationActions.Should().Be("fixed the issue");
    }

    [Fact]
    public void Resolve_WithExistingEndTime_ShouldNotOverwrite()
    {
        var endTime = DateTime.UtcNow.AddMinutes(-30);
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-1),
            ViolationEndTime = endTime
        };

        analysis.Resolve(Guid.NewGuid());

        analysis.ViolationEndTime.Should().Be(endTime);
    }

    [Fact]
    public void IsCriticalAndOngoing_CriticalAndUnresolved_ReturnsTrue()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.Critical,
            IsResolved = false,
            ViolationEndTime = null
        };
        analysis.IsCriticalAndOngoing().Should().BeTrue();
    }

    [Fact]
    public void IsCriticalAndOngoing_NotCritical_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.High,
            IsResolved = false,
            ViolationEndTime = null
        };
        analysis.IsCriticalAndOngoing().Should().BeFalse();
    }

    [Fact]
    public void IsCriticalAndOngoing_Resolved_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.Critical,
            IsResolved = true,
            ViolationEndTime = null
        };
        analysis.IsCriticalAndOngoing().Should().BeFalse();
    }

    [Fact]
    public void ExceedsDuration_AboveThreshold_ReturnsTrue()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddMinutes(-30),
            ViolationEndTime = null
        };
        analysis.ExceedsDuration(20).Should().BeTrue();
    }

    [Fact]
    public void ExceedsDuration_BelowThreshold_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddMinutes(-5),
            ViolationEndTime = null
        };
        analysis.ExceedsDuration(60).Should().BeFalse();
    }
}

#endregion

#region Entity Tests — ResourceUsageTrend

public class ResourceUsageTrendTests
{
    [Fact]
    public void IsAnomaly_ZeroStdDev_ReturnsFalse()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, StandardDeviation = 0 };
        trend.IsAnomaly(200).Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_NormalUsage_ReturnsFalse()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, StandardDeviation = 10 };
        trend.IsAnomaly(105).Should().BeFalse(); // z-score = 0.5
    }

    [Fact]
    public void IsAnomaly_AnomalousUsage_ReturnsTrue()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, StandardDeviation = 10 };
        trend.IsAnomaly(130).Should().BeTrue(); // z-score = 3.0
    }

    [Fact]
    public void ForecastNextPeriod_PositiveGrowth()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, GrowthRate = 10 };
        trend.ForecastNextPeriod().Should().BeApproximately(110, 0.01);
    }

    [Fact]
    public void ForecastNextPeriod_NegativeGrowth()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, GrowthRate = -10 };
        trend.ForecastNextPeriod().Should().BeApproximately(90, 0.01);
    }

    [Fact]
    public void GetTrendDirection_Growing()
    {
        var trend = new ResourceUsageTrend { GrowthRate = 10 };
        trend.GetTrendDirection().Should().Be("Growing");
    }

    [Fact]
    public void GetTrendDirection_Declining()
    {
        var trend = new ResourceUsageTrend { GrowthRate = -10 };
        trend.GetTrendDirection().Should().Be("Declining");
    }

    [Fact]
    public void GetTrendDirection_Steady()
    {
        var trend = new ResourceUsageTrend { GrowthRate = 2 };
        trend.GetTrendDirection().Should().Be("Steady");
    }

    [Fact]
    public void IsVolatile_ZeroAverage_ReturnsFalse()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 0, StandardDeviation = 10 };
        trend.IsVolatile().Should().BeFalse();
    }

    [Fact]
    public void IsVolatile_HighVariation_ReturnsTrue()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, StandardDeviation = 50 };
        trend.IsVolatile().Should().BeTrue(); // CV = 0.5 > 0.3
    }

    [Fact]
    public void IsVolatile_LowVariation_ReturnsFalse()
    {
        var trend = new ResourceUsageTrend { AverageUsage = 100, StandardDeviation = 10 };
        trend.IsVolatile().Should().BeFalse(); // CV = 0.1 < 0.3
    }

    [Fact]
    public void Type_AliasSetsResourceType()
    {
        var trend = new ResourceUsageTrend();
        trend.Type = ResourceUsageType.ApiCalls;
        trend.ResourceType.Should().Be(ResourceUsageType.ApiCalls);
    }
}

#endregion

#region Entity Tests — ResourceThrottlingPolicy

public class ResourceThrottlingPolicyTests
{
    [Fact]
    public void CalculateDelayMs_BelowThreshold_ReturnsZero()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.GradualDegradation,
            ThrottlingThresholdPercent = 80
        };
        policy.CalculateDelayMs(70).Should().Be(0);
    }

    [Fact]
    public void CalculateDelayMs_Inactive_ReturnsZero()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = false,
            Strategy = ThrottlingStrategy.HardCutoff
        };
        policy.CalculateDelayMs(100).Should().Be(0);
    }

    [Fact]
    public void CalculateDelayMs_None_ReturnsZero()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.None,
            ThrottlingThresholdPercent = 50
        };
        policy.CalculateDelayMs(90).Should().Be(0);
    }

    [Fact]
    public void CalculateDelayMs_HardCutoff_ReturnsMaxInt()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.HardCutoff,
            ThrottlingThresholdPercent = 80
        };
        policy.CalculateDelayMs(90).Should().Be(int.MaxValue);
    }

    [Fact]
    public void CalculateDelayMs_GradualDegradation_ReturnsPositiveDelay()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.GradualDegradation,
            ThrottlingThresholdPercent = 80,
            DegradationFactor = 1.0m
        };
        var delay = policy.CalculateDelayMs(90);
        delay.Should().BeGreaterThan(0);
        delay.Should().BeLessThan(5001);
    }

    [Fact]
    public void CalculateDelayMs_RateLimiting_ReturnsDelay()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.RateLimiting,
            ThrottlingThresholdPercent = 80,
            MaxRequestsPerWindow = 100,
            WindowDurationSeconds = 60
        };
        var delay = policy.CalculateDelayMs(90);
        delay.Should().Be(600); // 60000 / 100
    }

    [Fact]
    public void CalculateDelayMs_RateLimiting_NoWindowConfig_ReturnsZero()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.RateLimiting,
            ThrottlingThresholdPercent = 80,
            MaxRequestsPerWindow = null,
            WindowDurationSeconds = null
        };
        policy.CalculateDelayMs(90).Should().Be(0);
    }

    [Fact]
    public void ShouldBlock_HardCutoff_AboveThreshold_ReturnsTrue()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.HardCutoff,
            ThrottlingThresholdPercent = 80
        };
        policy.ShouldBlock(90).Should().BeTrue();
    }

    [Fact]
    public void ShouldBlock_Inactive_ReturnsFalse()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = false,
            Strategy = ThrottlingStrategy.HardCutoff
        };
        policy.ShouldBlock(100).Should().BeFalse();
    }

    [Fact]
    public void ShouldBlock_NonHardCutoff_ReturnsFalse()
    {
        var policy = new ResourceThrottlingPolicy
        {
            IsActive = true,
            Strategy = ThrottlingStrategy.GradualDegradation,
            ThrottlingThresholdPercent = 80
        };
        policy.ShouldBlock(90).Should().BeFalse();
    }

    [Fact]
    public void Threshold_Alias_SetsThrottlingThresholdPercent()
    {
        var policy = new ResourceThrottlingPolicy();
        policy.Threshold = 75;
        policy.ThrottlingThresholdPercent.Should().Be(75);
    }
}

#endregion

#region Entity Tests — UsageRecord

public class UsageRecordTests
{
    [Fact]
    public void GetUsagePerDay_NormalPeriod()
    {
        var record = new UsageRecord
        {
            Count = 300,
            PeriodStart = DateTime.UtcNow.AddDays(-10),
            PeriodEnd = DateTime.UtcNow
        };
        record.GetUsagePerDay().Should().BeApproximately(30.0, 1.0);
    }

    [Fact]
    public void GetUsagePerDay_SameDayPeriod_ReturnsCount()
    {
        var now = DateTime.UtcNow;
        var record = new UsageRecord { Count = 100, PeriodStart = now, PeriodEnd = now };
        record.GetUsagePerDay().Should().Be(100);
    }

    [Fact]
    public void CreateDaily_SetsPropertiesCorrectly()
    {
        var date = new DateTime(2025, 1, 15);
        var tenantId = Guid.NewGuid();
        var record = UsageRecord.CreateDaily(ResourceUsageType.ApiCalls, tenantId, 500, date, source: "API");

        record.Type.Should().Be(ResourceUsageType.ApiCalls);
        record.TenantId.Should().Be(tenantId);
        record.Count.Should().Be(500);
        record.Source.Should().Be("API");
        record.PeriodStart.Should().Be(date.Date);
        record.AveragePerDay.Should().Be(500);
    }

    [Fact]
    public void CreateMonthly_SetsPropertiesCorrectly()
    {
        var month = new DateTime(2025, 2, 1);
        var tenantId = Guid.NewGuid();
        var record = UsageRecord.CreateMonthly(ResourceUsageType.Storage, tenantId, 10000, month, 1500, new DateTime(2025, 2, 14));

        record.Type.Should().Be(ResourceUsageType.Storage);
        record.Count.Should().Be(10000);
        record.PeakUsage.Should().Be(1500);
        record.PeakUsageDate.Should().Be(new DateTime(2025, 2, 14));
        record.AveragePerDay.Should().BeApproximately(10000.0 / 28, 1.0);
    }

    [Fact]
    public void UsageAmount_Alias_SetsCount()
    {
        var record = new UsageRecord();
        record.UsageAmount = 42;
        record.Count.Should().Be(42);
    }
}

#endregion

#region Entity Tests — UsageRetentionPolicy

public class UsageRetentionPolicyTests
{
    [Fact]
    public void CalculateNextCompaction_SetsFromLastExecution()
    {
        var policy = new UsageRetentionPolicy
        {
            LastExecutedAt = DateTime.UtcNow.AddDays(-1),
            CompactionIntervalDays = 7
        };
        var next = policy.CalculateNextCompaction();
        next.Should().BeCloseTo(DateTime.UtcNow.AddDays(6), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ShouldExecute_Inactive_ReturnsFalse()
    {
        var policy = new UsageRetentionPolicy { IsActive = false };
        policy.ShouldExecute().Should().BeFalse();
    }

    [Fact]
    public void ShouldExecute_NoNextExecution_ReturnsTrue()
    {
        var policy = new UsageRetentionPolicy { IsActive = true, NextExecutionAt = null };
        policy.ShouldExecute().Should().BeTrue();
    }

    [Fact]
    public void ShouldExecute_PastDue_ReturnsTrue()
    {
        var policy = new UsageRetentionPolicy
        {
            IsActive = true,
            NextExecutionAt = DateTime.UtcNow.AddHours(-1)
        };
        policy.ShouldExecute().Should().BeTrue();
    }

    [Fact]
    public void ShouldExecute_FutureExecution_ReturnsFalse()
    {
        var policy = new UsageRetentionPolicy
        {
            IsActive = true,
            NextExecutionAt = DateTime.UtcNow.AddHours(1)
        };
        policy.ShouldExecute().Should().BeFalse();
    }

    [Fact]
    public void GetArchiveThresholdDate_ReturnsCorrectDate()
    {
        var policy = new UsageRetentionPolicy { ArchiveAfterDays = 30 };
        var threshold = policy.GetArchiveThresholdDate();
        threshold.Should().BeCloseTo(DateTime.UtcNow.AddDays(-30), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetDeletionThresholdDate_ReturnsCorrectDate()
    {
        var policy = new UsageRetentionPolicy { RetentionDays = 90 };
        var threshold = policy.GetDeletionThresholdDate();
        threshold.Should().BeCloseTo(DateTime.UtcNow.AddDays(-90), TimeSpan.FromMinutes(5));
    }
}

#endregion

#region Entity Tests — Other Entities

public class CostAllocationReportTests
{
    [Fact]
    public void ShouldCreateWithDefaults()
    {
        var report = new CostAllocationReport
        {
            ResourceUsageType = ResourceUsageType.Storage,
            TotalUsage = 5000,
            CostPerUnit = 0.001m,
            TotalCost = 5.00m,
            CostCenter = "Engineering"
        };
        report.IsExported.Should().BeFalse();
        report.CostCenter.Should().Be("Engineering");
    }
}

public class ResourceSettingsTests
{
    [Fact]
    public void GetEffectiveValue_WithValue_ReturnsValue()
    {
        var setting = new ResourceSettings { Value = "custom", DefaultValue = "default" };
        setting.GetEffectiveValue().Should().Be("custom");
    }

    [Fact]
    public void GetEffectiveValue_NoValue_ReturnsDefault()
    {
        var setting = new ResourceSettings { Value = null, DefaultValue = "default" };
        setting.GetEffectiveValue().Should().Be("default");
    }

    [Fact]
    public void GetEffectiveValue_NeitherSet_ReturnsNull()
    {
        var setting = new ResourceSettings { Value = null, DefaultValue = null };
        setting.GetEffectiveValue().Should().BeNull();
    }
}

public class ResourceMetadataTests
{
    [Fact]
    public void ShouldCreateWithDefaults()
    {
        var meta = new ResourceMetadata { Key = "max-upload-size", Value = "10485760" };
        meta.Key.Should().Be("max-upload-size");
    }
}

public class ResourceQuotaMetadataTests
{
    [Fact]
    public void ShouldCreateWithDefaults()
    {
        var meta = new ResourceQuotaMetadata();
        meta.Should().NotBeNull();
    }
}

#endregion

#region Model/DTO Tests

public class ResourceQuotaEnforcementResultTests
{
    [Fact]
    public void RemainingQuota_WithHardLimit_ReturnsCorrect()
    {
        var result = new ResourceQuotaEnforcementResult { HardLimit = 100, CurrentUsage = 75 };
        result.RemainingQuota.Should().Be(25);
    }

    [Fact]
    public void RemainingQuota_OverLimit_ReturnsZero()
    {
        var result = new ResourceQuotaEnforcementResult { HardLimit = 100, CurrentUsage = 120 };
        result.RemainingQuota.Should().Be(0);
    }

    [Fact]
    public void RemainingQuota_NoLimit_ReturnsNull()
    {
        var result = new ResourceQuotaEnforcementResult { HardLimit = null };
        result.RemainingQuota.Should().BeNull();
    }

    [Fact]
    public void ThrowIfNotAllowed_WhenAllowed_ShouldNotThrow()
    {
        var result = new ResourceQuotaEnforcementResult { IsAllowed = true };
        var act = () => result.ThrowIfNotAllowed(Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfNotAllowed_WhenNotAllowed_ShouldThrow()
    {
        var result = new ResourceQuotaEnforcementResult
        {
            IsAllowed = false,
            Type = ResourceUsageType.ApiCalls,
            CurrentUsage = 100,
            HardLimit = 50
        };
        var act = () => result.ThrowIfNotAllowed(Guid.NewGuid());
        act.Should().Throw<QuotaExceededException>();
    }

    [Fact]
    public void ThrowIfNotAllowed_WithCustomMessage_ShouldUseIt()
    {
        var result = new ResourceQuotaEnforcementResult
        {
            IsAllowed = false,
            Message = "Custom limit message"
        };
        var act = () => result.ThrowIfNotAllowed(Guid.NewGuid());
        act.Should().Throw<QuotaExceededException>()
            .WithMessage("*Custom limit message*");
    }
}

#endregion

#region Enum Tests

public class ResourceEnumTests
{
    [Theory]
    [InlineData(ResourceUsageType.Users, 1)]
    [InlineData(ResourceUsageType.Projects, 2)]
    [InlineData(ResourceUsageType.Storage, 3)]
    [InlineData(ResourceUsageType.ApiCalls, 4)]
    [InlineData(ResourceUsageType.Programs, 5)]
    [InlineData(ResourceUsageType.Courses, 6)]
    [InlineData(ResourceUsageType.FeatureFlags, 7)]
    [InlineData(ResourceUsageType.Assets, 24)]
    [InlineData(ResourceUsageType.AssetTransformations, 27)]
    public void ResourceUsageType_ShouldHaveExpectedValues(ResourceUsageType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Theory]
    [InlineData(ResourceQuotaPeriod.Daily, 1)]
    [InlineData(ResourceQuotaPeriod.Weekly, 2)]
    [InlineData(ResourceQuotaPeriod.Monthly, 3)]
    [InlineData(ResourceQuotaPeriod.Quarterly, 4)]
    [InlineData(ResourceQuotaPeriod.Yearly, 5)]
    [InlineData(ResourceQuotaPeriod.Unlimited, 6)]
    public void ResourceQuotaPeriod_ShouldHaveExpectedValues(ResourceQuotaPeriod period, int expected)
    {
        ((int)period).Should().Be(expected);
    }

    [Theory]
    [InlineData(ThrottlingStrategy.None, 0)]
    [InlineData(ThrottlingStrategy.HardCutoff, 1)]
    [InlineData(ThrottlingStrategy.GradualDegradation, 2)]
    [InlineData(ThrottlingStrategy.RateLimiting, 3)]
    [InlineData(ThrottlingStrategy.PriorityBased, 4)]
    public void ThrottlingStrategy_ShouldHaveExpectedValues(ThrottlingStrategy strategy, int expected)
    {
        ((int)strategy).Should().Be(expected);
    }

    [Theory]
    [InlineData(SlaViolationSeverity.None, 0)]
    [InlineData(SlaViolationSeverity.Low, 1)]
    [InlineData(SlaViolationSeverity.Medium, 2)]
    [InlineData(SlaViolationSeverity.High, 3)]
    [InlineData(SlaViolationSeverity.Critical, 4)]
    public void SlaViolationSeverity_ShouldHaveExpectedValues(SlaViolationSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }

    [Theory]
    [InlineData(SlaViolationType.None, 0)]
    [InlineData(SlaViolationType.QuotaExceeded, 1)]
    [InlineData(SlaViolationType.ResponseTimeExceeded, 2)]
    [InlineData(SlaViolationType.AvailabilityBreach, 3)]
    [InlineData(SlaViolationType.PerformanceDegradation, 4)]
    [InlineData(SlaViolationType.ThrottlingActivated, 5)]
    [InlineData(SlaViolationType.ResourceUnavailable, 6)]
    [InlineData(SlaViolationType.Other, 99)]
    public void SlaViolationType_ShouldHaveExpectedValues(SlaViolationType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

#endregion
