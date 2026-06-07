using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using GameGuild.Resources;

namespace GameGuild.Resources.UnitTests;

public class EfConfigAndServicesTests
{
    // ── EF Configuration Classes (9 configs) ────────────────────────────
    [Fact]
    public void CostAllocationReportConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new CostAllocationReportConfiguration();
        cfg.Configure(mb.Entity<CostAllocationReport>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void ResourceMetadataConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new ResourceMetadataConfiguration();
        cfg.Configure(mb.Entity<ResourceMetadata>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void ResourceQuotaConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new ResourceQuotaConfiguration();
        cfg.Configure(mb.Entity<ResourceQuota>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void ResourceSettingsConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new ResourceSettingsConfiguration();
        cfg.Configure(mb.Entity<ResourceSettings>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void ResourceThrottlingPolicyConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new ResourceThrottlingPolicyConfiguration();
        cfg.Configure(mb.Entity<ResourceThrottlingPolicy>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void ResourceUsageTrendConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new ResourceUsageTrendConfiguration();
        cfg.Configure(mb.Entity<ResourceUsageTrend>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void SlaImpactAnalysisConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new SlaImpactAnalysisConfiguration();
        cfg.Configure(mb.Entity<SlaImpactAnalysis>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void UsageRecordConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new UsageRecordConfiguration();
        cfg.Configure(mb.Entity<UsageRecord>());
        mb.Model.Should().NotBeNull();
    }

    [Fact]
    public void UsageRetentionPolicyConfiguration_CanConfigure()
    {
        var mb = new ModelBuilder(new ConventionSet());
        var cfg = new UsageRetentionPolicyConfiguration();
        cfg.Configure(mb.Entity<UsageRetentionPolicy>());
        mb.Model.Should().NotBeNull();
    }

    // ── DI Extensions ───────────────────────────────────────────────────
    [Fact]
    public void AddResourceQuotaBehavior_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddResourceQuotaBehavior();
        services.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RegisterResourceUsageType_CanRegister()
    {
        var services = new ServiceCollection();
        var info = new ResourceUsageTypeInfo
        {
            Id = 1001,
            Key = "test_type",
            DisplayName = "Test Type"
        };
        services.RegisterResourceUsageType(info);
        services.Should().NotBeNull();
    }

    [Fact]
    public void RegisterResourceUsageTypes_Bulk()
    {
        var services = new ServiceCollection();
        var info1 = new ResourceUsageTypeInfo { Id = 1002, Key = "bulk_1", DisplayName = "Bulk 1" };
        var info2 = new ResourceUsageTypeInfo { Id = 1003, Key = "bulk_2", DisplayName = "Bulk 2" };
        services.RegisterResourceUsageTypes(info1, info2);
        services.Should().NotBeNull();
    }

    // ── Repository Constructors ─────────────────────────────────────────
    [Fact]
    public void ResourceQuotaRepository_CanBeCreated()
    {
        var repo = new ResourceQuotaRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void UsageRecordRepository_CanBeCreated()
    {
        var repo = new UsageRecordRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void CostAllocationReportRepository_CanBeCreated()
    {
        var repo = new CostAllocationReportRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void ResourceThrottlingPolicyRepository_CanBeCreated()
    {
        var repo = new ResourceThrottlingPolicyRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void SlaImpactAnalysisRepository_CanBeCreated()
    {
        var repo = new SlaImpactAnalysisRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void UsageRetentionPolicyRepository_CanBeCreated()
    {
        var repo = new UsageRetentionPolicyRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void ResourceUsageTrendRepository_CanBeCreated()
    {
        var repo = new ResourceUsageTrendRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void ResourceMetadataRepository_CanBeCreated()
    {
        var repo = new ResourceMetadataRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    [Fact]
    public void ResourceSettingsRepository_CanBeCreated()
    {
        var repo = new ResourceSettingsRepository(Mock.Of<IApplicationDbContext>());
        repo.Should().NotBeNull();
    }

    // ── Service Constructors ────────────────────────────────────────────
    [Fact]
    public void UsageService_CanBeCreated()
    {
        var svc = new UsageService(
            Mock.Of<IUsageRecordRepository>(),
            Mock.Of<IResourceQuotaRepository>(),
            NullLogger<UsageService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task UsageService_GetCurrentUsageAsync_ReturnsQuotaAndCurrentRecords()
    {
        var tenantId = Guid.NewGuid();
        var quotaRepository = new Mock<IResourceQuotaRepository>();
        var usageRecordRepository = new Mock<IUsageRecordRepository>();

        quotaRepository
            .Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ResourceQuota
                {
                    TenantId = tenantId,
                    Type = ResourceUsageType.ApiCalls,
                    CurrentUsage = 25,
                    HardLimit = 100
                }
            ]);

        usageRecordRepository
            .Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                UsageRecord.CreateDaily(ResourceUsageType.Storage, tenantId, 50, DateTime.UtcNow)
            ]);

        var svc = new UsageService(usageRecordRepository.Object, quotaRepository.Object, NullLogger<UsageService>.Instance);

        var result = await svc.GetCurrentUsageAsync(tenantId);

        result.TenantId.Should().Be(tenantId);
        result.CurrentUsage[ResourceUsageType.ApiCalls.ToString()].Should().Be(25);
        result.CurrentUsage[ResourceUsageType.Storage.ToString()].Should().Be(50);
        result.Limits[ResourceUsageType.ApiCalls.ToString()].Should().Be(100);
    }

    [Fact]
    public async Task UsageService_TrackUsageAsync_IncrementsQuotaAndStoresDailyRecord()
    {
        var tenantId = Guid.NewGuid();
        UsageRecord? capturedRecord = null;
        var quotaRepository = new Mock<IResourceQuotaRepository>();
        var usageRecordRepository = new Mock<IUsageRecordRepository>();

        quotaRepository
            .Setup(r => r.TryIncrementUsageAsync(tenantId, ResourceUsageType.ApiCalls, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new ResourceQuota { TenantId = tenantId, Type = ResourceUsageType.ApiCalls, CurrentUsage = 3 }));

        usageRecordRepository
            .Setup(r => r.CreateAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback<UsageRecord, CancellationToken>((record, _) => capturedRecord = record)
            .ReturnsAsync((UsageRecord record, CancellationToken _) => record);

        var svc = new UsageService(usageRecordRepository.Object, quotaRepository.Object, NullLogger<UsageService>.Instance);

        await svc.TrackUsageAsync(tenantId, "ApiCalls", 3);

        quotaRepository.Verify(r => r.TryIncrementUsageAsync(tenantId, ResourceUsageType.ApiCalls, 3, It.IsAny<CancellationToken>()), Times.Once);
        capturedRecord.Should().NotBeNull();
        capturedRecord!.TenantId.Should().Be(tenantId);
        capturedRecord.Type.Should().Be(ResourceUsageType.ApiCalls);
        capturedRecord.UsageAmount.Should().Be(3);
    }

    [Fact]
    public async Task UsageService_IsWithinLimitsAsync_UsesHardLimit()
    {
        var tenantId = Guid.NewGuid();
        var quotaRepository = new Mock<IResourceQuotaRepository>();
        var usageRecordRepository = new Mock<IUsageRecordRepository>();

        quotaRepository
            .Setup(r => r.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceQuota
            {
                TenantId = tenantId,
                Type = ResourceUsageType.Storage,
                CurrentUsage = 90,
                HardLimit = 100,
                IsActive = true
            });

        var svc = new UsageService(usageRecordRepository.Object, quotaRepository.Object, NullLogger<UsageService>.Instance);

        (await svc.IsWithinLimitsAsync(tenantId, "Storage", 10)).Should().BeTrue();
        (await svc.IsWithinLimitsAsync(tenantId, "Storage", 11)).Should().BeFalse();
    }

    [Fact]
    public void QuotaManagementService_CanBeCreated()
    {
        var svc = new QuotaManagementService(
            Mock.Of<IResourceQuotaRepository>(),
            Mock.Of<IUsageRecordRepository>(),
            Mock.Of<GameGuild.CQRS.IPublisher>(),
            NullLogger<QuotaManagementService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void QuotaEnforcementService_CanBeCreated()
    {
        var svc = new QuotaEnforcementService(
            Mock.Of<IResourceQuotaRepository>(),
            Mock.Of<IQuotaManagementService>(),
            Mock.Of<GameGuild.CQRS.IPublisher>(),
            NullLogger<QuotaEnforcementService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void QuotaMaintenanceService_CanBeCreated()
    {
        var svc = new QuotaMaintenanceService(
            Mock.Of<IResourceQuotaRepository>(),
            Mock.Of<IUsageRecordRepository>(),
            Mock.Of<IQuotaManagementService>(),
            Mock.Of<GameGuild.CQRS.IPublisher>(),
            NullLogger<QuotaMaintenanceService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void ResourceQuotaService_CanBeCreated()
    {
        var svc = new ResourceQuotaService(
            Mock.Of<IQuotaManagementService>(),
            Mock.Of<IQuotaEnforcementService>(),
            Mock.Of<IQuotaMaintenanceService>());
        svc.Should().NotBeNull();
    }

    [Fact]
    public void CachedResourceQuotaService_CanBeCreated()
    {
        var svc = new CachedResourceQuotaService(
            Mock.Of<IResourceQuotaService>(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CachedResourceQuotaService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void ResourceThrottlingService_CanBeCreated()
    {
        var svc = new ResourceThrottlingService(
            Mock.Of<IResourceThrottlingPolicyRepository>(),
            Mock.Of<IResourceQuotaRepository>(),
            NullLogger<ResourceThrottlingService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void UsageRetentionService_CanBeCreated()
    {
        var svc = new UsageRetentionService(
            Mock.Of<IUsageRetentionPolicyRepository>(),
            Mock.Of<IUsageRecordRepository>(),
            NullLogger<UsageRetentionService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void UsageTrendAnalysisService_CanBeCreated()
    {
        var svc = new UsageTrendAnalysisService(
            Mock.Of<IResourceUsageTrendRepository>(),
            Mock.Of<IUsageRecordRepository>(),
            NullLogger<UsageTrendAnalysisService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void CostAllocationService_CanBeCreated()
    {
        var svc = new CostAllocationService(
            Mock.Of<ICostAllocationReportRepository>(),
            Mock.Of<IUsageRecordRepository>(),
            Mock.Of<IResourceQuotaRepository>(),
            Options.Create(new ResourcesOptions()),
            NullLogger<CostAllocationService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void SlaImpactAnalysisService_CanBeCreated()
    {
        var svc = new SlaImpactAnalysisService(
            Mock.Of<ISlaImpactAnalysisRepository>(),
            Mock.Of<IResourceQuotaRepository>(),
            Mock.Of<ISlaIncidentEscalationService>(),
            Mock.Of<IIncidentTicketProvider>(),
            NullLogger<SlaImpactAnalysisService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void SlaIncidentEscalationService_CanBeCreated()
    {
        var svc = new SlaIncidentEscalationService(
            Mock.Of<ISlaImpactAnalysisRepository>(),
            Mock.Of<IIncidentTicketProvider>(),
            Mock.Of<ISlaNotificationSender>(),
            NullLogger<SlaIncidentEscalationService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void LoggingSlaNotificationSender_CanBeCreated()
    {
        var svc = new LoggingSlaNotificationSender(
            NullLogger<LoggingSlaNotificationSender>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DefaultIncidentTicketProvider_CanBeCreated()
    {
        var svc = new DefaultIncidentTicketProvider();
        svc.Should().NotBeNull();
    }

    [Fact]
    public void QuotaExceededAlertHandler_CanBeCreated()
    {
        var h = new GameGuild.Resources.Handlers.QuotaExceededAlertHandler(
            NullLogger<GameGuild.Resources.Handlers.QuotaExceededAlertHandler>.Instance);
        h.Should().NotBeNull();
    }

    // ── DTOs and Result Records ─────────────────────────────────────────
    [Fact]
    public void ResourceUsageTypeInfo_CanBeCreated()
    {
        var info = new ResourceUsageTypeInfo
        {
            Id = 1,
            Key = "users",
            DisplayName = "Users",
            Description = "User count",
            Unit = "count",
            SupportsSoftLimit = true,
            IsBuiltIn = true,
            OwnerModule = "Identity"
        };
        info.Key.Should().Be("users");
    }

    [Fact]
    public void QuotaExceededEvent_CanBeCreated()
    {
        var evt = new QuotaExceededEvent(
            Guid.NewGuid(), ResourceUsageType.Users, 100, 10, 100,
            "test", Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.CurrentUsage.Should().Be(100);
    }

    [Fact]
    public void QuotaChangedEvent_CanBeCreated()
    {
        var evt = new QuotaChangedEvent(
            Guid.NewGuid(), ResourceUsageType.Storage,
            QuotaChangeType.UsageIncremented, 50, 60, 80, 100,
            "upload", Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.CurrentUsage.Should().Be(60);
    }

    [Fact]
    public void SlaEscalationResult_Factory_CanBeCreated()
    {
        var r = SlaEscalationResult.Success("INC-001", new List<Guid> { Guid.NewGuid() });
        r.WasEscalated.Should().BeTrue();
        r.IncidentId.Should().Be("INC-001");
    }

    [Fact]
    public void SlaEscalationResult_NotRequired()
    {
        var r = SlaEscalationResult.NotRequired();
        r.WasEscalated.Should().BeFalse();
    }

    [Fact]
    public void SlaEscalationResult_Failed()
    {
        var r = SlaEscalationResult.Failed("some error");
        r.WasEscalated.Should().BeFalse();
        r.ErrorMessage.Should().Be("some error");
    }

    [Fact]
    public void SlaEscalationConfig_CanBeCreated()
    {
        var cfg = new SlaEscalationConfig
        {
            TenantId = Guid.NewGuid(),
            AutoEscalationEnabled = true,
            MinimumEscalationSeverity = SlaViolationSeverity.High,
            AutoCreateIncidents = true,
            ExternalTicketingUrl = "http://tickets.test",
            WebhookUrl = "http://webhook.test",
            NotificationCooldownMinutes = 30
        };
        cfg.AutoEscalationEnabled.Should().BeTrue();
    }

    [Fact]
    public void ResourceQuotaEnforcementResult_CanBeCreated()
    {
        var r = new ResourceQuotaEnforcementResult
        {
            IsAllowed = false,
            IsSoftLimitExceeded = true,
            IsHardLimitExceeded = true,
            CurrentUsage = 110,
            SoftLimit = 80,
            HardLimit = 100,
            UsagePercentage = 110.0,
            ExcessAmount = 10,
            Message = "Exceeded"
        };
        r.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void QuotaExceededException_CanBeCreated()
    {
        var ex = new QuotaExceededException(
            ResourceUsageType.ApiCalls, 1500, 1000, Guid.NewGuid());
        ex.ResourceType.Should().Be(ResourceUsageType.ApiCalls);
        ex.CurrentUsage.Should().Be(1500);
        ex.Limit.Should().Be(1000);
    }

    [Fact]
    public void QuotaExceededException_WithMessage()
    {
        var ex = new QuotaExceededException(
            "Over limit", ResourceUsageType.Storage, 200, 100, Guid.NewGuid());
        ex.Message.Should().Be("Over limit");
    }

    [Fact]
    public void RequiresQuotaAttribute_CanBeCreated()
    {
        var attr = new RequiresQuotaAttribute(ResourceUsageType.Projects, 5);
        attr.ResourceType.Should().Be(ResourceUsageType.Projects);
        attr.Amount.Should().Be(5);
    }

    [Fact]
    public void ResourcesOptions_CanBeCreated()
    {
        var opts = new ResourcesOptions();
        opts.Should().NotBeNull();
    }

    // ── Enum Coverage ───────────────────────────────────────────────────
    [Fact]
    public void ResourceUsageType_AllValues()
    {
        var values = Enum.GetValues<ResourceUsageType>();
        values.Should().Contain(ResourceUsageType.Users);
        values.Should().Contain(ResourceUsageType.Storage);
        values.Length.Should().BeGreaterThan(5);
    }

    [Fact]
    public void ThrottlingStrategy_AllValues()
    {
        var values = Enum.GetValues<ThrottlingStrategy>();
        values.Should().Contain(ThrottlingStrategy.None);
        values.Should().Contain(ThrottlingStrategy.PriorityBased);
    }

    [Fact]
    public void SlaViolationType_AllValues()
    {
        var values = Enum.GetValues<SlaViolationType>();
        values.Should().Contain(SlaViolationType.None);
        values.Should().Contain(SlaViolationType.Other);
    }

    [Fact]
    public void SlaViolationSeverity_AllValues()
    {
        var values = Enum.GetValues<SlaViolationSeverity>();
        values.Should().Contain(SlaViolationSeverity.None);
        values.Should().Contain(SlaViolationSeverity.Critical);
    }

    // ── Controller Request DTOs ─────────────────────────────────────────
    [Fact]
    public void SetResourceSettingsRequest_CanBeCreated()
    {
        var req = new SetResourceSettingsRequest("myval", "defval", "string", "desc", "cat", true, 1, "required");
        req.Value.Should().Be("myval");
        req.DefaultValue.Should().Be("defval");
        req.DataType.Should().Be("string");
        req.Description.Should().Be("desc");
        req.Category.Should().Be("cat");
        req.AllowUserOverride.Should().BeTrue();
        req.DisplayOrder.Should().Be(1);
        req.ValidationRules.Should().Be("required");
    }

    [Fact]
    public void SetQuotaRequest_CanBeCreated()
    {
        var req = new SetQuotaRequest(100, 200, ResourceQuotaPeriod.Monthly, true, TimeSpan.FromHours(1));
        req.SoftLimit.Should().Be(100);
        req.HardLimit.Should().Be(200);
        req.Period.Should().Be(ResourceQuotaPeriod.Monthly);
        req.IsActive.Should().BeTrue();
        req.ResetTime.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void RecordTenantResourceUsageRequest_CanBeCreated()
    {
        var now = DateTime.UtcNow;
        var req = new RecordTenantResourceUsageRequest(ResourceUsageType.ApiCalls, 50, now.AddDays(-1), now);
        req.ResourceUsageType.Should().Be(ResourceUsageType.ApiCalls);
        req.Count.Should().Be(50);
    }

    [Fact]
    public void SetResourceMetadataRequest_CanBeCreated()
    {
        var req = new SetResourceMetadataRequest("val", "int", "A description", "metaCat", 5);
        req.Value.Should().Be("val");
        req.DataType.Should().Be("int");
        req.Description.Should().Be("A description");
        req.Category.Should().Be("metaCat");
        req.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public void RecordUserResourceUsageRequest_CanBeCreated()
    {
        var now = DateTime.UtcNow;
        var req = new RecordUserResourceUsageRequest(ResourceUsageType.ApiCalls, 1024, now.AddDays(-1), now);
        req.ResourceUsageType.Should().Be(ResourceUsageType.ApiCalls);
        req.Count.Should().Be(1024);
    }

    [Fact]
    public void UsageTrendsResult_CanBeCreated()
    {
        var now = DateTime.UtcNow;
        var dp = new UsageTrendDataPoint(now, 500, 3);
        dp.Period.Should().Be(now);
        dp.TotalUsage.Should().Be(500);
        dp.TenantCount.Should().Be(3);
        var result = new UsageTrendsResult(
            ResourceUsageType.ApiCalls, now.AddDays(-7), now,
            TrendGranularity.Daily, new List<UsageTrendDataPoint> { dp });
        result.Type.Should().Be(ResourceUsageType.ApiCalls);
        result.DataPoints.Should().HaveCount(1);
    }

    [Fact]
    public void GetResourceUsageTrendsQuery_CanBeCreated()
    {
        var now = DateTime.UtcNow;
        var q = new GetResourceUsageTrendsQuery(ResourceUsageType.ApiCalls, now.AddDays(-7), now, TrendGranularity.Daily);
        q.ResourceUsageType.Should().Be(ResourceUsageType.ApiCalls);
        q.Granularity.Should().Be(TrendGranularity.Daily);
    }

    [Fact]
    public void TrendGranularity_AllValues()
    {
        var values = Enum.GetValues<TrendGranularity>();
        values.Should().NotBeEmpty();
    }

    [Fact]
    public void ResourceQuotaPeriod_AllValues()
    {
        var values = Enum.GetValues<ResourceQuotaPeriod>();
        values.Should().NotBeEmpty();
    }
}
