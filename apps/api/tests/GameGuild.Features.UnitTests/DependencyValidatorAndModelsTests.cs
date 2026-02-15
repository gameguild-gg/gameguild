using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Features.UnitTests;

#region FeatureFlagDependencyValidator

public class FeatureFlagDependencyValidatorTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _repoMock = new();
    private readonly FeatureFlagDependencyValidator _sut;

    public FeatureFlagDependencyValidatorTests()
    {
        _sut = new FeatureFlagDependencyValidator(_repoMock.Object);
    }

    [Fact]
    public async Task HasCircularDependencyAsync_NoDependencies_ReturnsFalse()
    {
        var flag = new FeatureFlag { Key = "flag-b", Targets = [] };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flag);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_DirectCircle_ReturnsTrue()
    {
        // flag-a depends on flag-b, and we're checking if flag-b depends on flag-a
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_NullFlag_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync((FeatureFlag?)null);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateDependencyGraphAsync_NoCycle_ReturnsValid()
    {
        var flag = new FeatureFlag { Key = "flag-a", Targets = [] };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flag);

        var (isValid, cycle) = await _sut.ValidateDependencyGraphAsync("flag-a");

        isValid.Should().BeTrue();
        cycle.Should().BeNull();
    }

    [Fact]
    public async Task ValidateDependencyGraphAsync_WithCycle_ReturnsInvalid()
    {
        var flagA = new FeatureFlag
        {
            Key = "flag-a",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-b" }]
        };
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flagA);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var (isValid, cycle) = await _sut.ValidateDependencyGraphAsync("flag-a");

        isValid.Should().BeFalse();
        cycle.Should().NotBeNull();
        cycle.Should().Contain("flag-a");
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_NoFlags_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<FeatureFlag>());

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_NoCycles_ReturnsEmpty()
    {
        var flags = new List<FeatureFlag>
        {
            new() { Key = "flag-a", Targets = [] },
            new() { Key = "flag-b", Targets = [] }
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(flags);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flags[0]);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flags[1]);

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_WithCycles_FindsCycles()
    {
        var flagA = new FeatureFlag
        {
            Key = "flag-a",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-b" }]
        };
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<FeatureFlag> { flagA, flagB });
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flagA);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_IndirectCycle_ReturnsTrue()
    {
        // flag-a → flag-b → flag-c → flag-a (indirect cycle)
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-c" }]
        };
        var flagC = new FeatureFlag
        {
            Key = "flag-c",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-c", default)).ReturnsAsync(flagC);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeTrue();
    }
}

#endregion

#region SubscriptionPlan (via concrete subclass)

public class SubscriptionPlanTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var plan = new ConcretePlan();

        plan.Id.Should().BeEmpty();
        plan.Name.Should().BeEmpty();
        plan.Tier.Should().BeEmpty();
        plan.MaxFeatures.Should().Be(0);
        plan.AdvancedFeaturesEnabled.Should().BeFalse();
        plan.CustomTargetingEnabled.Should().BeFalse();
        plan.AnalyticsEnabled.Should().BeFalse();
        plan.Priority.Should().Be(0);
        plan.ExpiresAt.Should().BeNull();
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var plan = new ConcretePlan
        {
            Id = "pro",
            Name = "Professional",
            Tier = "Premium",
            MaxFeatures = 100,
            AdvancedFeaturesEnabled = true,
            CustomTargetingEnabled = true,
            AnalyticsEnabled = true,
            Priority = 10,
            ExpiresAt = new DateTime(2026, 1, 1),
            IsActive = false
        };

        plan.Id.Should().Be("pro");
        plan.Name.Should().Be("Professional");
        plan.Tier.Should().Be("Premium");
        plan.MaxFeatures.Should().Be(100);
        plan.AdvancedFeaturesEnabled.Should().BeTrue();
        plan.IsActive.Should().BeFalse();
    }

    private class ConcretePlan : SubscriptionPlan { }
}

#endregion

#region AnalyticsExportRequest (via concrete subclass)

public class AnalyticsExportRequestTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var req = new ConcreteExportRequest();

        req.StartDate.Should().BeNull();
        req.EndDate.Should().BeNull();
        req.FeatureKeys.Should().BeEmpty();
        req.TenantIds.Should().BeEmpty();
        req.Format.Should().Be("json");
        req.IncludeDetails.Should().BeTrue();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;
        var req = new ConcreteExportRequest
        {
            StartDate = start,
            EndDate = end,
            FeatureKeys = ["feat-1", "feat-2"],
            TenantIds = [Guid.NewGuid()],
            Format = "csv",
            IncludeDetails = false
        };

        req.StartDate.Should().Be(start);
        req.EndDate.Should().Be(end);
        req.FeatureKeys.Should().HaveCount(2);
        req.Format.Should().Be("csv");
        req.IncludeDetails.Should().BeFalse();
    }

    private class ConcreteExportRequest : AnalyticsExportRequest { }
}

#endregion

#region AnalyticsExportResult

public class AnalyticsExportResultAdditionalTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var result = new AnalyticsExportResult();

        result.Content.Should().BeEmpty();
        result.ContentType.Should().BeEmpty();
        result.FileName.Should().BeEmpty();
        result.RecordCount.Should().Be(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

#endregion

#region FeatureFlag Entity additional

public class FeatureFlagAdditionalTests
{
    [Fact]
    public void IsExpired_NoExpiresAt_ReturnsFalse()
    {
        var ff = new FeatureFlag();
        ff.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_FutureDate_ReturnsFalse()
    {
        var ff = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) };
        ff.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_PastDate_ReturnsTrue()
    {
        var ff = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        ff.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsStale_NoReviewDate_ReturnsFalse()
    {
        var ff = new FeatureFlag();
        ff.IsStale().Should().BeFalse();
    }

    [Fact]
    public void IsStale_FutureDate_ReturnsFalse()
    {
        var ff = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(30) };
        ff.IsStale().Should().BeFalse();
    }

    [Fact]
    public void IsStale_PastDate_ReturnsTrue()
    {
        var ff = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(-1) };
        ff.IsStale().Should().BeTrue();
    }

    [Fact]
    public void GetDaysUntilExpiration_NoExpiry_ReturnsNull()
    {
        var ff = new FeatureFlag();
        ff.GetDaysUntilExpiration().Should().BeNull();
    }

    [Fact]
    public void GetDaysUntilExpiration_FutureDate_ReturnsPositive()
    {
        var ff = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(10) };
        ff.GetDaysUntilExpiration().Should().BeGreaterOrEqualTo(9);
    }

    [Fact]
    public void GetDaysUntilExpiration_PastDate_ReturnsZero()
    {
        var ff = new FeatureFlag { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-5) };
        ff.GetDaysUntilExpiration().Should().Be(0);
    }

    [Fact]
    public void GetDaysUntilReview_NoReviewDate_ReturnsNull()
    {
        var ff = new FeatureFlag();
        ff.GetDaysUntilReview().Should().BeNull();
    }

    [Fact]
    public void GetDaysUntilReview_FutureDate_ReturnsPositive()
    {
        var ff = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(15) };
        ff.GetDaysUntilReview().Should().BeGreaterOrEqualTo(14);
    }

    [Fact]
    public void GetDaysUntilReview_PastDate_ReturnsZero()
    {
        var ff = new FeatureFlag { ReviewDate = DateTimeOffset.UtcNow.AddDays(-3) };
        ff.GetDaysUntilReview().Should().Be(0);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var ff = new FeatureFlag();

        ff.Key.Should().BeEmpty();
        ff.Name.Should().BeEmpty();
        ff.Description.Should().BeEmpty();
        ff.IsEnabled.Should().BeFalse();
        ff.Type.Should().Be(FeatureFlagType.Toggle);
        ff.DefaultValue.Should().BeNull();
        ff.EnabledValue.Should().BeNull();
        ff.RolloutPercentage.Should().Be(100);
        ff.Environment.Should().Be("production");
        ff.ExpiresAt.Should().BeNull();
        ff.ReviewDate.Should().BeNull();
        ff.IsKillSwitch.Should().BeFalse();
        ff.Owner.Should().BeNull();
        ff.RequiresEncryption.Should().BeFalse();
        ff.Targets.Should().BeEmpty();
        ff.UsageAnalytics.Should().BeEmpty();
    }
}

#endregion

#region FeatureUsageRanking additional

public class FeatureUsageRankingAdditionalTests
{
    [Fact]
    public void EnabledPercentage_ZeroAccess_ReturnsZero()
    {
        var r = new FeatureUsageRanking { AccessCount = 0 };
        r.EnabledPercentage.Should().Be(0);
    }

    [Fact]
    public void EnabledPercentage_AllEnabled_Returns100()
    {
        var r = new FeatureUsageRanking { AccessCount = 100, EnabledCount = 100 };
        r.EnabledPercentage.Should().Be(100);
    }

    [Fact]
    public void EnabledPercentage_Half_Returns50()
    {
        var r = new FeatureUsageRanking { AccessCount = 200, EnabledCount = 100 };
        r.EnabledPercentage.Should().Be(50);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var r = new FeatureUsageRanking
        {
            FeatureKey = "feat",
            AccessCount = 500,
            EnabledCount = 400,
            DisabledCount = 100,
            UniqueUserCount = 50,
            UniqueTenantCount = 5,
            Rank = 1
        };
        r.FeatureKey.Should().Be("feat");
        r.Rank.Should().Be(1);
    }
}

#endregion

#region RealtimeUsageStats additional

public class RealtimeUsageStatsAdditionalTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var stats = new RealtimeUsageStats
        {
            EvaluationsLastMinute = 100,
            EvaluationsLastHour = 5000,
            EvaluationsToday = 100000,
            ActiveFeatureCount = 42,
            ErrorRate = 0.5,
            AverageLatencyMs = 12.5,
            CacheHitRate = 95.2
        };

        stats.EvaluationsLastMinute.Should().Be(100);
        stats.EvaluationsLastHour.Should().Be(5000);
        stats.EvaluationsToday.Should().Be(100000);
        stats.ActiveFeatureCount.Should().Be(42);
        stats.ErrorRate.Should().Be(0.5);
        stats.AverageLatencyMs.Should().Be(12.5);
        stats.CacheHitRate.Should().Be(95.2);
    }
}

#endregion

#region TenantFeatureAnalytics additional

public class TenantFeatureAnalyticsAdditionalTests
{
    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var tenantId = Guid.NewGuid();
        var analytics = new TenantFeatureAnalytics
        {
            TenantId = tenantId,
            TotalFeaturesAccessed = 10,
            EnabledFeaturesCount = 8,
            DisabledFeaturesCount = 2,
            TotalAccessCount = 1000,
            FirstAccessDate = DateTime.UtcNow.AddDays(-30),
            LastAccessDate = DateTime.UtcNow,
            AccessByEnvironment = new Dictionary<string, long>
            {
                { "production", 800 },
                { "staging", 200 }
            }
        };

        analytics.TenantId.Should().Be(tenantId);
        analytics.TotalFeaturesAccessed.Should().Be(10);
        analytics.EnabledFeaturesCount.Should().Be(8);
        analytics.DisabledFeaturesCount.Should().Be(2);
        analytics.TotalAccessCount.Should().Be(1000);
        analytics.AccessByEnvironment.Should().HaveCount(2);
    }
}

#endregion
#region FeaturesModule DI Registration

public class FeaturesModuleTests
{
    [Fact]
    public void AddFeaturesModule_RegistersAllServices()
    {
        var services = new ServiceCollection();

        // Add required dependencies that FeaturesModule needs
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddSingleton(new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);
        services.AddOptions();

        services.AddFeaturesModule();

        // Verify key services are registered
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagQueryRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagTargetingRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagAnalyticsRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagConfigurationService));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagAnalyticsService));
        services.Should().Contain(d => d.ServiceType == typeof(ISubscriptionFeatureService));
        services.Should().Contain(d => d.ServiceType == typeof(ICapabilityService));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagManagementService));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagEncryptionService));
        services.Should().Contain(d => d.ServiceType == typeof(IFeatureFlagEvaluationService));
        services.Should().Contain(d => d.ServiceType == typeof(DatabaseFeatureFlagProvider));
    }

    [Fact]
    public void AddFeaturesModule_RegistersStrategies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddSingleton(new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);
        services.AddOptions();

        services.AddFeaturesModule();

        // Should register multiple evaluation strategies
        var strategyDescriptors = services.Where(d => d.ServiceType == typeof(IFeatureEvaluationStrategy)).ToList();
        strategyDescriptors.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public void AddFeaturesModule_RegistersTargetingHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddSingleton(new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);
        services.AddOptions();

        services.AddFeaturesModule();

        // Should register multiple targeting handlers
        var handlerDescriptors = services.Where(d => d.ServiceType == typeof(ITargetingRuleHandler)).ToList();
        handlerDescriptors.Should().HaveCountGreaterOrEqualTo(5);
    }
}

#endregion

#region CapabilityAuditLogDto

public class CapabilityAuditLogDtoTests
{
    [Fact]
    public void CanCreate_WithAllProperties()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        var dto = new CapabilityAuditLogDto(
            id, tenantId, "lms.certificates", true, false,
            "plan:pro", "override:admin", userId,
            "Admin disabled", "Modified", changedAt);

        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.CapabilityKey.Should().Be("lms.certificates");
        dto.OldValue.Should().BeTrue();
        dto.NewValue.Should().BeFalse();
        dto.OldSource.Should().Be("plan:pro");
        dto.NewSource.Should().Be("override:admin");
        dto.ChangedByUserId.Should().Be(userId);
        dto.ChangeReason.Should().Be("Admin disabled");
        dto.ChangeType.Should().Be("Modified");
        dto.ChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public void NullableFields_CanBeNull()
    {
        var dto = new CapabilityAuditLogDto(
            Guid.NewGuid(), Guid.NewGuid(), "key", null, true,
            null, null, null, null, "Granted", DateTimeOffset.UtcNow);

        dto.OldValue.Should().BeNull();
        dto.OldSource.Should().BeNull();
        dto.NewSource.Should().BeNull();
        dto.ChangedByUserId.Should().BeNull();
        dto.ChangeReason.Should().BeNull();
    }
}

#endregion