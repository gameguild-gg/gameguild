using FluentAssertions;
using GameGuild.CQRS.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

public sealed class ResourcesIntegrationCloseoutTests
{
    [Fact]
    public void ResourcesIntegrationContracts_ExposeThrottleFinanceTrendAndRetentionExtensionPoints()
    {
        var assembly = typeof(IResourceThrottlingService).Assembly;

        assembly.GetType("GameGuild.Resources.IResourceThrottlingEnforcementSink").Should().NotBeNull();
        assembly.GetType("GameGuild.Resources.ThrottlingEnforcementResult").Should().NotBeNull();
        typeof(ThrottlingResult).GetProperty("EnforcementReference").Should().NotBeNull();
        typeof(ThrottlingResult).GetProperty("EnforcedAt").Should().NotBeNull();

        assembly.GetType("GameGuild.Resources.ICostCenterValidator").Should().NotBeNull();
        assembly.GetType("GameGuild.Resources.CostCenterValidationResult").Should().NotBeNull();
        typeof(CostAllocationReport).GetProperty("CostCenterValidationStatus").Should().NotBeNull();

        assembly.GetType("GameGuild.Resources.IUsagePatternRecognizer").Should().NotBeNull();
        assembly.GetType("GameGuild.Resources.UsagePatternRecognitionResult").Should().NotBeNull();

        assembly.GetType("GameGuild.Resources.IUsageRetentionArchiveSink").Should().NotBeNull();
        assembly.GetType("GameGuild.Resources.UsageArchiveManifest").Should().NotBeNull();
        typeof(RetentionExecutionResult).GetProperty("ArchiveReference").Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyThrottlingAsync_RecordsRuntimeEnforcementReference()
    {
        var tenantId = Guid.NewGuid();
        var policyRepository = new Mock<IResourceThrottlingPolicyRepository>();
        var quotaRepository = new Mock<IResourceQuotaRepository>();
        var enforcementSink = new Mock<IResourceThrottlingEnforcementSink>();

        policyRepository
            .Setup(repository => repository.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.ApiCalls, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceThrottlingPolicy
            {
                ResourceType = ResourceUsageType.ApiCalls,
                Strategy = ThrottlingStrategy.HardCutoff,
                ThrottlingThresholdPercent = 50,
                IsActive = true
            });
        quotaRepository
            .Setup(repository => repository.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.ApiCalls, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceQuota { CurrentUsage = 80 });
        enforcementSink
            .Setup(sink => sink.ApplyAsync(
                tenantId,
                ResourceUsageType.ApiCalls,
                1,
                It.Is<ThrottlingResult>(result => !result.IsAllowed),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThrottlingEnforcementResult(
                tenantId,
                ResourceUsageType.ApiCalls,
                true,
                "gateway-local-reference",
                SystemClock.UtcNow,
                0,
                "blocked"));

        var service = new ResourceThrottlingService(
            policyRepository.Object,
            quotaRepository.Object,
            NullLogger<ResourceThrottlingService>.Instance,
            enforcementSink.Object);

        var result = await service.ApplyThrottlingAsync(tenantId, ResourceUsageType.ApiCalls);

        result.IsAllowed.Should().BeFalse();
        result.EnforcementReference.Should().Be("gateway-local-reference");
        result.EnforcedAt.Should().NotBeNull();
        enforcementSink.VerifyAll();
    }

    [Fact]
    public async Task UpdateAllocationTagsAsync_StoresValidatedCostCenterStatus()
    {
        var reportId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var report = new CostAllocationReport { Id = reportId };
        report.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });

        var reportRepository = new Mock<ICostAllocationReportRepository>();
        reportRepository
            .Setup(repository => repository.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var validator = new Mock<ICostCenterValidator>();
        validator
            .Setup(service => service.ValidateAsync(tenantId, "ENG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostCenterValidationResult.Validated());

        var service = new CostAllocationService(
            reportRepository.Object,
            Mock.Of<IUsageRecordRepository>(),
            Mock.Of<IResourceQuotaRepository>(),
            Options.Create(new ResourcesOptions()),
            NullLogger<CostAllocationService>.Instance,
            sender: null,
            validator.Object);

        var updated = await service.UpdateAllocationTagsAsync(reportId, new Dictionary<string, string> { ["CostCenter"] = "ENG" });

        updated.Should().BeTrue();
        report.CostCenter.Should().Be("ENG");
        report.CostCenterValidationStatus.Should().Be("Validated");
        validator.VerifyAll();
    }

    [Fact]
    public async Task AnalyzeTrendAsync_UsesPatternRecognizerBeforeSavingTrend()
    {
        var tenantId = Guid.NewGuid();
        var start = SystemClock.UtcNow.AddDays(-3);
        var end = SystemClock.UtcNow;
        var records = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 10, PeriodStart = start },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 20, PeriodStart = start.AddDays(1) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 40, PeriodStart = start.AddDays(2) }
        };

        var usageRepository = new Mock<IUsageRecordRepository>();
        usageRepository
            .Setup(repository => repository.GetByTenantAsync(tenantId, ResourceUsageType.Storage, start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        var trendRepository = new Mock<IResourceUsageTrendRepository>();
        trendRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceUsageTrend trend, CancellationToken _) => trend);

        var recognizer = new Mock<IUsagePatternRecognizer>();
        recognizer
            .Setup(service => service.RecognizeAsync(
                It.IsAny<ResourceUsageTrend>(),
                It.Is<IReadOnlyList<UsageRecord>>(items => items.Count == records.Count),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsagePatternRecognitionResult("AI Recognized Growth", 0.88, """{"recognizer":"test"}"""));

        var service = new UsageTrendAnalysisService(
            trendRepository.Object,
            usageRepository.Object,
            NullLogger<UsageTrendAnalysisService>.Instance,
            recognizer.Object);

        var trend = await service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, start, end);

        trend.Pattern.Should().Be("AI Recognized Growth");
        trend.PatternConfidence.Should().Be(0.88);
        trend.Metadata.Should().Be("""{"recognizer":"test"}""");
        recognizer.VerifyAll();
    }

    [Fact]
    public async Task ExecuteRetentionAsync_AddsArchiveReferenceFromArchiveSink()
    {
        var tenantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var policy = new UsageRetentionPolicy
        {
            Id = policyId,
            Name = "Storage retention",
            ResourceType = ResourceUsageType.Storage,
            RetentionDays = 365,
            ArchiveAfterDays = 30,
            EnableCompaction = false,
            IsActive = true
        };
        policy.SetTenantId(tenantId);

        var policyRepository = new Mock<IUsageRetentionPolicyRepository>();
        policyRepository
            .Setup(repository => repository.GetByIdAsync(policyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        policyRepository
            .Setup(repository => repository.UpdateAsync(policy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var usageRepository = new Mock<IUsageRecordRepository>();
        usageRepository
            .Setup(repository => repository.ArchiveOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        usageRepository
            .Setup(repository => repository.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var archiveSink = new Mock<IUsageRetentionArchiveSink>();
        archiveSink
            .Setup(sink => sink.ArchiveAsync(tenantId, ResourceUsageType.Storage, It.IsAny<DateTime>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageArchiveManifest(
                tenantId,
                ResourceUsageType.Storage,
                SystemClock.UtcNow.AddDays(-30),
                3,
                "storage-ref",
                "backup-ref",
                SystemClock.UtcNow));

        var service = new UsageRetentionService(
            policyRepository.Object,
            usageRepository.Object,
            NullLogger<UsageRetentionService>.Instance,
            archiveSink.Object);

        var result = await service.ExecuteRetentionAsync(policyId);

        result.RecordsArchived.Should().Be(3);
        result.ArchiveReference.Should().Be("storage-ref|backup-ref");
        archiveSink.VerifyAll();
    }
}
