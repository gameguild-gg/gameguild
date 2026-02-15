using System.Collections.ObjectModel;
using FluentAssertions;

namespace GameGuild.Features.UnitTests;

public class FeatureFlagTargetTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var target = new FeatureFlagTarget();
        target.TargetType.Should().Be(string.Empty);
        target.TargetIdentifier.Should().Be(string.Empty);
        target.IsEnabled.Should().BeFalse();
        target.RolloutPercentage.Should().Be(100);
        target.CustomValue.Should().BeNull();
        target.Metadata.Should().BeNull();
        target.Priority.Should().Be(0);
        target.DependsOn.Should().BeNull();
        target.FeatureFlag.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var flagId = Guid.NewGuid();
        var target = new FeatureFlagTarget
        {
            FeatureFlagId = flagId,
            TargetType = "tenant",
            TargetIdentifier = "tenant-123",
            IsEnabled = true,
            RolloutPercentage = 50,
            CustomValue = "custom",
            Metadata = "{}",
            Priority = 10,
            DependsOn = "other-flag"
        };

        target.FeatureFlagId.Should().Be(flagId);
        target.TargetType.Should().Be("tenant");
        target.TargetIdentifier.Should().Be("tenant-123");
        target.IsEnabled.Should().BeTrue();
        target.RolloutPercentage.Should().Be(50);
        target.CustomValue.Should().Be("custom");
        target.Metadata.Should().Be("{}");
        target.Priority.Should().Be(10);
        target.DependsOn.Should().Be("other-flag");
    }
}

public class FeatureFlagUsageTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var usage = new FeatureFlagUsage();
        usage.TenantId.Should().BeNull();
        usage.UserId.Should().BeNull();
        usage.Environment.Should().Be("production");
        usage.AccessCount.Should().Be(1);
        usage.WasEnabled.Should().BeFalse();
        usage.ReturnedValue.Should().BeNull();
        usage.ContextData.Should().BeNull();
        usage.FeatureFlag.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var flagId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var usage = new FeatureFlagUsage
        {
            FeatureFlagId = flagId,
            TenantId = tenantId,
            UserId = userId,
            Environment = "staging",
            AccessCount = 42,
            WasEnabled = true,
            ReturnedValue = "true",
            FirstAccessAt = now.AddHours(-1),
            LastAccessAt = now,
            ContextData = "{\"key\":\"value\"}"
        };

        usage.FeatureFlagId.Should().Be(flagId);
        usage.TenantId.Should().Be(tenantId);
        usage.UserId.Should().Be(userId);
        usage.Environment.Should().Be("staging");
        usage.AccessCount.Should().Be(42);
        usage.WasEnabled.Should().BeTrue();
        usage.ReturnedValue.Should().Be("true");
        usage.FirstAccessAt.Should().Be(now.AddHours(-1));
        usage.LastAccessAt.Should().Be(now);
        usage.ContextData.Should().Be("{\"key\":\"value\"}");
    }
}

public class CapabilityAuditLogTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var log = new CapabilityAuditLog();
        log.CapabilityKey.Should().Be(string.Empty);
        log.OldValue.Should().BeNull();
        log.NewValue.Should().BeFalse();
        log.OldSource.Should().BeNull();
        log.NewSource.Should().BeNull();
        log.ChangedByUserId.Should().BeNull();
        log.ChangeReason.Should().BeNull();
        log.IpAddress.Should().BeNull();
        log.UserAgent.Should().BeNull();
        log.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var log = new CapabilityAuditLog
        {
            TenantId = tenantId,
            CapabilityKey = "lxp.discovery",
            OldValue = false,
            NewValue = true,
            OldSource = "plan:free",
            NewSource = "plan:pro",
            ChangedByUserId = userId,
            ChangeReason = "Upgraded plan",
            ChangeType = CapabilityChangeType.PlanChange,
            ChangedAt = now,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            CorrelationId = "corr-123"
        };

        log.TenantId.Should().Be(tenantId);
        log.CapabilityKey.Should().Be("lxp.discovery");
        log.OldValue.Should().BeFalse();
        log.NewValue.Should().BeTrue();
        log.OldSource.Should().Be("plan:free");
        log.NewSource.Should().Be("plan:pro");
        log.ChangedByUserId.Should().Be(userId);
        log.ChangeReason.Should().Be("Upgraded plan");
        log.ChangeType.Should().Be(CapabilityChangeType.PlanChange);
        log.ChangedAt.Should().Be(now);
        log.IpAddress.Should().Be("192.168.1.1");
        log.UserAgent.Should().Be("Mozilla/5.0");
        log.CorrelationId.Should().Be("corr-123");
    }

    [Theory]
    [InlineData(CapabilityChangeType.Granted, 0)]
    [InlineData(CapabilityChangeType.Revoked, 1)]
    [InlineData(CapabilityChangeType.Modified, 2)]
    [InlineData(CapabilityChangeType.Expired, 3)]
    [InlineData(CapabilityChangeType.Restored, 4)]
    [InlineData(CapabilityChangeType.AdminOverride, 5)]
    [InlineData(CapabilityChangeType.PlanChange, 6)]
    public void CapabilityChangeType_ShouldHaveExpectedValues(CapabilityChangeType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

public class FeatureFlagToggledEventTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var flagId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var evt = new FeatureFlagToggledEvent(flagId, "my-flag", true, tenantId);

        evt.FeatureFlagId.Should().Be(flagId);
        evt.Key.Should().Be("my-flag");
        evt.IsEnabled.Should().BeTrue();
        evt.TenantId.Should().Be(tenantId);
        evt.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutTenantId_ShouldDefaultToNull()
    {
        var evt = new FeatureFlagToggledEvent(Guid.NewGuid(), "key", false);
        evt.TenantId.Should().BeNull();
    }
}

public class UsageLimitExceededEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var evt = new UsageLimitExceededEvent(tenantId, "api-calls", 1500, 1000, 150.0);

        evt.TenantId.Should().Be(tenantId);
        evt.MetricName.Should().Be("api-calls");
        evt.CurrentUsage.Should().Be(1500);
        evt.Limit.Should().Be(1000);
        evt.UtilizationPercentage.Should().Be(150.0);
        evt.EventId.Should().NotBeEmpty();
    }
}

public class FeatureFlagUsageStatsTests
{
    [Fact]
    public void EnabledPercentage_WhenNoAccess_ShouldReturnZero()
    {
        var stats = new FeatureFlagUsageStats { TotalAccessCount = 0, EnabledCount = 0 };
        stats.EnabledPercentage.Should().Be(0);
    }

    [Fact]
    public void EnabledPercentage_WhenAllEnabled_ShouldReturn100()
    {
        var stats = new FeatureFlagUsageStats { TotalAccessCount = 100, EnabledCount = 100 };
        stats.EnabledPercentage.Should().Be(100);
    }

    [Fact]
    public void EnabledPercentage_WhenHalfEnabled_ShouldReturn50()
    {
        var stats = new FeatureFlagUsageStats { TotalAccessCount = 200, EnabledCount = 100 };
        stats.EnabledPercentage.Should().Be(50);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var stats = new FeatureFlagUsageStats
        {
            TotalAccessCount = 500,
            EnabledCount = 300,
            DisabledCount = 200,
            UniqueUserCount = 50,
            UniqueTenantCount = 5,
            FirstAccessDate = now.AddDays(-30),
            LastAccessDate = now
        };

        stats.TotalAccessCount.Should().Be(500);
        stats.EnabledCount.Should().Be(300);
        stats.DisabledCount.Should().Be(200);
        stats.UniqueUserCount.Should().Be(50);
        stats.UniqueTenantCount.Should().Be(5);
        stats.FirstAccessDate.Should().Be(now.AddDays(-30));
        stats.LastAccessDate.Should().Be(now);
    }
}

public class FeatureFlagConstantsTests
{
    [Fact]
    public void TopLevel_ShouldHaveExpectedValues()
    {
        FeatureFlagConstants.DefaultEnvironment.Should().Be("production");
        FeatureFlagConstants.MaxRolloutPercentage.Should().Be(100);
        FeatureFlagConstants.MinRolloutPercentage.Should().Be(0);
        FeatureFlagConstants.DefaultRolloutPercentage.Should().Be(100);
        FeatureFlagConstants.DefaultRolloutSalt.Should().Be("default");
        FeatureFlagConstants.AnonymousIdentifier.Should().Be("anonymous");
    }

    [Fact]
    public void TargetTypes_ShouldHaveExpectedValues()
    {
        FeatureFlagConstants.TargetTypes.Tenant.Should().Be("tenant");
        FeatureFlagConstants.TargetTypes.User.Should().Be("user");
        FeatureFlagConstants.TargetTypes.Plan.Should().Be("plan");
        FeatureFlagConstants.TargetTypes.Country.Should().Be("country");
        FeatureFlagConstants.TargetTypes.Environment.Should().Be("environment");
        FeatureFlagConstants.TargetTypes.Role.Should().Be("role");
        FeatureFlagConstants.TargetTypes.Custom.Should().Be("custom");
    }

    [Fact]
    public void CacheKeys_ShouldHaveExpectedValues()
    {
        FeatureFlagConstants.CacheKeys.FeatureFlagPrefix.Should().Be("feature:");
        FeatureFlagConstants.CacheKeys.ConfigPrefix.Should().Be("config:");
        FeatureFlagConstants.CacheKeys.AnalyticsPrefix.Should().Be("analytics:");
        FeatureFlagConstants.CacheKeys.SdkPrefix.Should().Be("sdk:");
        FeatureFlagConstants.CacheKeys.EnvironmentPrefix.Should().Be("env:");
    }

    [Fact]
    public void FlagTypes_ShouldHaveExpectedValues()
    {
        FeatureFlagConstants.FlagTypes.Toggle.Should().Be("toggle");
        FeatureFlagConstants.FlagTypes.Experiment.Should().Be("experiment");
        FeatureFlagConstants.FlagTypes.Rollout.Should().Be("rollout");
        FeatureFlagConstants.FlagTypes.Permission.Should().Be("permission");
        FeatureFlagConstants.FlagTypes.KillSwitch.Should().Be("killswitch");
    }

    [Fact]
    public void AssetFeatureFlags_ShouldHaveExpectedValues()
    {
        FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.AllowedTransformations.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.MaxTransformDimension.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.DownloadWindowHours.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.HotlinkLimitPerHour.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.PerceptualDedupEnabled.Should().Contain("asset:");
        FeatureFlagConstants.AssetFeatureFlags.QualityUpgradeThreshold.Should().Contain("asset:");
    }
}

public class FeatureFlagOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeFeatureFlags()
    {
        FeatureFlagOptions.SectionName.Should().Be("FeatureFlags");
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var options = new FeatureFlagOptions();
        options.CacheTtlMinutes.Should().Be(5);
        options.SdkRefreshIntervalSeconds.Should().Be(300);
        options.DefaultEnvironment.Should().Be("production");
        options.EnableAnalytics.Should().BeTrue();
        options.EnableCaching.Should().BeTrue();
        options.MaxBulkEvaluationSize.Should().Be(50);
        options.SdkCacheTtlSeconds.Should().Be(600);
        options.EnableDetailedLogging.Should().BeFalse();
        options.EncryptionKey.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var options = new FeatureFlagOptions
        {
            CacheTtlMinutes = 10,
            SdkRefreshIntervalSeconds = 120,
            DefaultEnvironment = "staging",
            EnableAnalytics = false,
            EnableCaching = false,
            MaxBulkEvaluationSize = 100,
            SdkCacheTtlSeconds = 300,
            EnableDetailedLogging = true,
            EncryptionKey = "my-key"
        };

        options.CacheTtlMinutes.Should().Be(10);
        options.EnableAnalytics.Should().BeFalse();
        options.EncryptionKey.Should().Be("my-key");
    }
}

public class EvaluationContextTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var ctx = new EvaluationContext();
        ctx.UserId.Should().BeNull();
        ctx.TenantId.Should().BeNull();
        ctx.Environment.Should().Be("production");
        ctx.Attributes.Should().NotBeNull().And.BeEmpty();
        ctx.SessionId.Should().BeNull();
        ctx.UserGroups.Should().NotBeNull().And.BeEmpty();
        ctx.Location.Should().BeNull();
        ctx.DeviceType.Should().BeNull();
        ctx.AppVersion.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var ctx = new EvaluationContext
        {
            UserId = "user-1",
            TenantId = "tenant-1",
            Environment = "staging",
            SessionId = "session-1",
            Location = "US",
            DeviceType = "mobile",
            AppVersion = "2.0.0"
        };
        ctx.Attributes["plan"] = "pro";
        ctx.UserGroups.Add("beta");

        ctx.UserId.Should().Be("user-1");
        ctx.Attributes.Should().ContainKey("plan");
        ctx.UserGroups.Should().Contain("beta");
    }
}

public class SdkConfigurationTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var config = new SdkConfiguration();
        config.ApiKey.Should().BeEmpty();
        config.BaseUrl.Should().BeEmpty();
        config.Environment.Should().Be("production");
        config.TimeoutSeconds.Should().Be(30);
        config.PollingIntervalSeconds.Should().Be(60);
        config.EnableCaching.Should().BeTrue();
        config.CacheExpirationMinutes.Should().Be(5);
        config.EnableAnalytics.Should().BeTrue();
        config.EnableDebugLogging.Should().BeFalse();
        config.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var config = new SdkConfiguration
        {
            ApiKey = "key-123",
            BaseUrl = "https://api.example.com",
            Environment = "staging",
            TimeoutSeconds = 60,
            PollingIntervalSeconds = 30,
            EnableCaching = false,
            CacheExpirationMinutes = 10,
            EnableAnalytics = false,
            EnableDebugLogging = true,
            Version = "2.0.0"
        };
        config.ApiKey.Should().Be("key-123");
        config.EnableDebugLogging.Should().BeTrue();
    }
}

public class SdkEndpointsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var endpoints = new SdkEndpoints();
        endpoints.Features.Should().Be("/features");
        endpoints.Evaluate.Should().Be("/features/evaluate");
        endpoints.Analytics.Should().Be("/features/analytics");
        endpoints.Health.Should().Be("/health");
        endpoints.Config.Should().Be("/sdk/config");
    }
}

public class FeatureFlagTypeEnumTests
{
    [Theory]
    [InlineData(FeatureFlagType.Toggle, 0)]
    [InlineData(FeatureFlagType.Numeric, 1)]
    [InlineData(FeatureFlagType.String, 2)]
    [InlineData(FeatureFlagType.Percentage, 3)]
    [InlineData(FeatureFlagType.UserSegment, 4)]
    public void FeatureFlagType_ShouldHaveExpectedValues(FeatureFlagType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

public class FeatureFlagDtoTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var id = Guid.NewGuid();
        var dto = new FeatureFlagDto
        {
            Id = id,
            Key = "my-flag",
            Name = "My Flag",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            CreatedAt = DateTime.UtcNow
        };

        dto.Id.Should().Be(id);
        dto.Key.Should().Be("my-flag");
        dto.Name.Should().Be("My Flag");
        dto.IsEnabled.Should().BeTrue();
        dto.Type.Should().Be(FeatureFlagType.Toggle);
        dto.Description.Should().BeNull();
        dto.Environment.Should().BeNull();
        dto.TenantId.Should().BeNull();
        dto.DefaultValue.Should().BeNull();
        dto.UpdatedAt.Should().BeNull();
        dto.DeletedAt.Should().BeNull();
        dto.Targets.Should().BeNull();
    }
}

public class FeatureFlagDependencyTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var dto = new FeatureFlagDependency
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = Guid.NewGuid(),
            DependsOnFeatureFlagId = Guid.NewGuid(),
            DependencyType = "requires",
            FeatureFlagKey = "flag-a",
            DependsOnFeatureFlagKey = "flag-b",
            CreatedAt = DateTime.UtcNow
        };
        dto.DependencyType.Should().Be("requires");
    }
}

public class FeatureFlagTargetDtoTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var dto = new FeatureFlagTargetDto
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "tenant",
            TargetIdentifier = "t-1",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        dto.TargetType.Should().Be("tenant");
        dto.RolloutPercentage.Should().Be(0); // default for int
        dto.CustomValue.Should().BeNull();
        dto.Metadata.Should().BeNull();
        dto.Priority.Should().Be(0);
        dto.UpdatedAt.Should().BeNull();
        dto.DeletedAt.Should().BeNull();
    }
}

public class FeatureFlagUsageSummaryTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var dto = new FeatureFlagUsageSummary
        {
            FeatureFlagId = Guid.NewGuid(),
            FeatureFlagKey = "key",
            Name = "My Flag",
            IsEnabled = true,
            TotalEvaluations = 100,
            UniqueUsers = 10,
            LastEvaluatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        dto.TotalEvaluations.Should().Be(100);
        dto.Environment.Should().BeNull();
        dto.TenantId.Should().BeNull();
    }
}

public class FeatureFlagStatisticsTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var now = DateTime.UtcNow;
        var dto = new FeatureFlagStatistics
        {
            FeatureFlagId = Guid.NewGuid(),
            FeatureFlagKey = "key",
            TotalEvaluations = 1000,
            EnabledEvaluations = 750,
            DisabledEvaluations = 250,
            EnabledPercentage = 75.0,
            UniqueUsers = 50,
            FirstEvaluationAt = now.AddDays(-30),
            LastEvaluationAt = now,
            PeriodStart = now.AddDays(-30),
            PeriodEnd = now
        };
        dto.EnabledPercentage.Should().Be(75.0);
    }
}

public class FeatureFlagEvaluationHistoryTests
{
    [Fact]
    public void CanCreate_WithRequiredProperties()
    {
        var dto = new FeatureFlagEvaluationHistory
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = Guid.NewGuid(),
            FeatureFlagKey = "key",
            UserId = "user-1",
            EvaluatedValue = true,
            WasEnabled = true,
            EvaluatedAt = DateTime.UtcNow
        };
        dto.Environment.Should().BeNull();
        dto.TenantId.Should().BeNull();
        dto.Context.Should().BeNull();
    }
}

public class FeatureAccessResultTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var result = new FeatureAccessResult();
        result.HasAccess.Should().BeFalse();
        result.Reason.Should().BeNull();
        result.FeatureFlag.Should().BeNull();
        result.Plan.Should().BeNull();
        result.RequiresUpgrade.Should().BeFalse();
    }
}

public class FeatureContextTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var ctx = new FeatureContext();
        ctx.TenantId.Should().BeNull();
        ctx.UserId.Should().BeNull();
        ctx.SubscriptionPlanId.Should().BeNull();
        ctx.Environment.Should().Be("production");
        ctx.Permissions.Should().NotBeNull().And.BeEmpty();
        ctx.CustomAttributes.Should().NotBeNull().And.BeEmpty();
        ctx.UserAgent.Should().BeNull();
        ctx.IpAddress.Should().BeNull();
        ctx.Country.Should().BeNull();
    }
}

public class FeatureEvaluationRequestTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var req = new FeatureEvaluationRequest();
        req.FeatureKey.Should().BeEmpty();
        req.DefaultValue.Should().BeNull();
        req.Context.Should().NotBeNull();
    }
}

public class FeatureEvaluationResultTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var result = new FeatureEvaluationResult();
        result.FeatureKey.Should().BeEmpty();
        result.IsEnabled.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Reason.Should().BeNull();
        result.RolloutPercentage.Should().Be(0);
        result.IsTargeted.Should().BeFalse();
        result.TargetType.Should().BeEmpty();
        result.Metadata.Should().NotBeNull().And.BeEmpty();
    }
}

public class FeatureFlagAnalyticsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var analytics = new FeatureFlagAnalytics();
        analytics.FeatureKey.Should().BeEmpty();
        analytics.TotalAccesses.Should().Be(0);
        analytics.AccessesByTenant.Should().NotBeNull().And.BeEmpty();
        analytics.AccessesByEnvironment.Should().NotBeNull().And.BeEmpty();
    }
}

public class FeatureFlagConfigTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var config = new FeatureFlagConfig();
        config.Key.Should().BeEmpty();
        config.Name.Should().BeEmpty();
        config.IsEnabled.Should().BeFalse();
        config.TargetingRules.Should().NotBeNull().And.BeEmpty();
    }
}

public class TargetingRuleTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var rule = new TargetingRule();
        rule.TargetType.Should().BeEmpty();
        rule.TargetIdentifier.Should().BeEmpty();
        rule.IsEnabled.Should().BeFalse();
        rule.RolloutPercentage.Should().Be(100);
        rule.CustomValue.Should().BeNull();
        rule.Priority.Should().Be(0);
        rule.Conditions.Should().NotBeNull().And.BeEmpty();
    }
}

public class FeatureFlagChangeTypeTests
{
    [Theory]
    [InlineData(FeatureFlagChangeType.Created, 0)]
    [InlineData(FeatureFlagChangeType.Updated, 1)]
    [InlineData(FeatureFlagChangeType.Deleted, 2)]
    [InlineData(FeatureFlagChangeType.Enabled, 3)]
    [InlineData(FeatureFlagChangeType.Disabled, 4)]
    [InlineData(FeatureFlagChangeType.TargetingChanged, 5)]
    [InlineData(FeatureFlagChangeType.RolloutChanged, 6)]
    public void ShouldHaveExpectedValues(FeatureFlagChangeType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

public class FeatureUsageRankingTests
{
    [Fact]
    public void EnabledPercentage_WhenNoAccess_ShouldReturnZero()
    {
        var ranking = new FeatureUsageRanking();
        ranking.EnabledPercentage.Should().Be(0);
    }

    [Fact]
    public void EnabledPercentage_ShouldCompute()
    {
        var ranking = new FeatureUsageRanking { AccessCount = 200, EnabledCount = 100 };
        ranking.EnabledPercentage.Should().Be(50);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var ranking = new FeatureUsageRanking
        {
            FeatureKey = "flag-1",
            AccessCount = 500,
            EnabledCount = 300,
            DisabledCount = 200,
            UniqueUserCount = 50,
            UniqueTenantCount = 5,
            Rank = 1
        };
        ranking.Rank.Should().Be(1);
    }
}

public class RealtimeUsageStatsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var stats = new RealtimeUsageStats();
        stats.EvaluationsLastMinute.Should().Be(0);
        stats.EvaluationsLastHour.Should().Be(0);
        stats.EvaluationsToday.Should().Be(0);
        stats.ActiveFeatureCount.Should().Be(0);
        stats.ErrorRate.Should().Be(0);
        stats.AverageLatencyMs.Should().Be(0);
        stats.CacheHitRate.Should().Be(0);
    }
}

public class UsageMetricTests
{
    [Fact]
    public void UtilizationPercentage_WithLimit_ShouldCompute()
    {
        var metric = new UsageMetric { Name = "api-calls", CurrentUsage = 750, Limit = 1000 };
        metric.UtilizationPercentage.Should().Be(75.0);
        metric.IsOverLimit.Should().BeFalse();
    }

    [Fact]
    public void UtilizationPercentage_WithoutLimit_ShouldReturnZero()
    {
        var metric = new UsageMetric { Name = "api-calls", CurrentUsage = 750, Limit = null };
        metric.UtilizationPercentage.Should().Be(0);
    }

    [Fact]
    public void IsOverLimit_WhenOver_ShouldReturnTrue()
    {
        var metric = new UsageMetric { Name = "api-calls", CurrentUsage = 1500, Limit = 1000 };
        metric.IsOverLimit.Should().BeTrue();
    }
}

public class BulkEvaluationRequestTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var req = new BulkEvaluationRequest();
        req.FeatureKeys.Should().NotBeNull().And.BeEmpty();
        req.Context.Should().NotBeNull();
    }
}

public class ToggleFeatureRequestTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var req = new ToggleFeatureRequest();
        req.FeatureKey.Should().BeEmpty();
        req.IsEnabled.Should().BeFalse();
        req.Reason.Should().BeNull();
        req.TenantId.Should().BeNull();
        req.Environment.Should().BeNull();
    }
}

public class UpdateFeatureRequestTests
{
    [Fact]
    public void CanCreate_WithAllNull()
    {
        var req = new UpdateFeatureRequest(null, null, null, null, null, null);
        req.Name.Should().BeNull();
        req.Description.Should().BeNull();
        req.IsEnabled.Should().BeNull();
        req.RolloutPercentage.Should().BeNull();
        req.EnabledValue.Should().BeNull();
        req.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void CanCreate_WithValues()
    {
        var req = new UpdateFeatureRequest("name", "desc", true, 50, "enabled", "default");
        req.Name.Should().Be("name");
        req.RolloutPercentage.Should().Be(50);
    }
}

public class TenantFeatureAccessResultTests
{
    [Fact]
    public void Granted_ShouldReturnAccessTrue()
    {
        var result = TenantFeatureAccessResult.Granted("my-flag");
        result.HasAccess.Should().BeTrue();
        result.FeatureKey.Should().Be("my-flag");
        result.DenialReason.Should().BeNull();
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Granted_WithMetadata_ShouldIncludeMetadata()
    {
        var metadata = new Dictionary<string, object> { { "plan", "pro" } };
        var result = TenantFeatureAccessResult.Granted("my-flag", metadata);
        result.Metadata.Should().ContainKey("plan");
    }

    [Fact]
    public void Denied_ShouldReturnAccessFalse()
    {
        var result = TenantFeatureAccessResult.Denied("my-flag", "Plan too low");
        result.HasAccess.Should().BeFalse();
        result.FeatureKey.Should().Be("my-flag");
        result.DenialReason.Should().Be("Plan too low");
    }
}

public class TenantFeatureAnalyticsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var analytics = new TenantFeatureAnalytics();
        analytics.TotalFeaturesAccessed.Should().Be(0);
        analytics.EnabledFeaturesCount.Should().Be(0);
        analytics.DisabledFeaturesCount.Should().Be(0);
        analytics.TotalAccessCount.Should().Be(0);
        analytics.TopFeatures.Should().NotBeNull().And.BeEmpty();
        analytics.FirstAccessDate.Should().BeNull();
        analytics.LastAccessDate.Should().BeNull();
        analytics.AccessByEnvironment.Should().NotBeNull().And.BeEmpty();
    }
}

public class AnalyticsExportResultTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var result = new AnalyticsExportResult();
        result.Content.Should().NotBeNull().And.BeEmpty();
        result.ContentType.Should().BeEmpty();
        result.FileName.Should().BeEmpty();
        result.RecordCount.Should().Be(0);
    }
}

public class SdkConfigurationMoreTests
{
    [Fact]
    public void GeneratedAt_ShouldBeSet()
    {
        var config = new SdkConfiguration();
        config.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
