using FluentAssertions;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

public class UsageTrendAnalysisServiceTests
{
    private readonly Mock<IResourceUsageTrendRepository> _trendRepositoryMock;
    private readonly Mock<IUsageRecordRepository> _usageRepositoryMock;
    private readonly Mock<ILogger<UsageTrendAnalysisService>> _loggerMock;
    private readonly UsageTrendAnalysisService _service;

    public UsageTrendAnalysisServiceTests()
    {
        _trendRepositoryMock = new Mock<IResourceUsageTrendRepository>();
        _usageRepositoryMock = new Mock<IUsageRecordRepository>();
        _loggerMock = new Mock<ILogger<UsageTrendAnalysisService>>();

        _service = new UsageTrendAnalysisService(
            _trendRepositoryMock.Object,
            _usageRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task AnalyzeTrendAsync_CreatesEmptyTrend_WhenNoUsageRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsageRecord>());

        ResourceUsageTrend? capturedTrend = null;
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceUsageTrend, CancellationToken>((t, _) => capturedTrend = t)
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        var result = await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);

        // Assert - when no records, should return empty trend with "Insufficient Data" pattern
        result.Should().NotBeNull();
        result.AverageUsage.Should().Be(0);
        result.MinUsage.Should().Be(0);
        result.MaxUsage.Should().Be(0);
        result.Pattern.Should().Be("Insufficient Data");
    }

    [Fact]
    public async Task AnalyzeTrendAsync_CalculatesStatistics_Correctly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 200, PeriodStart = periodStart.AddDays(10) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 150, PeriodStart = periodStart.AddDays(20) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        ResourceUsageTrend? capturedTrend = null;
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceUsageTrend, CancellationToken>((t, _) => capturedTrend = t)
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        var result = await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);

        // Assert
        result.Should().NotBeNull();
        capturedTrend.Should().NotBeNull();
        capturedTrend!.AverageUsage.Should().Be(150); // (100 + 200 + 150) / 3 = 150
        capturedTrend.MinUsage.Should().Be(100);
        capturedTrend.MaxUsage.Should().Be(200);
        capturedTrend.Type.Should().Be(ResourceUsageType.Storage);
        capturedTrend.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task AnalyzeTrendAsync_CalculatesStandardDeviation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart.AddDays(10) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart.AddDays(20) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        ResourceUsageTrend? capturedTrend = null;
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceUsageTrend, CancellationToken>((t, _) => capturedTrend = t)
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);

        // Assert - all values are same, so std dev should be 0
        capturedTrend.Should().NotBeNull();
        capturedTrend!.StandardDeviation.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeTrendAsync_DetectsAnomalies()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 110, PeriodStart = periodStart.AddDays(5) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 105, PeriodStart = periodStart.AddDays(10) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 1000, PeriodStart = periodStart.AddDays(15) }, // Anomaly
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart.AddDays(20) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        ResourceUsageTrend? capturedTrend = null;
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceUsageTrend, CancellationToken>((t, _) => capturedTrend = t)
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);

        // Assert - should detect at least one anomaly
        capturedTrend.Should().NotBeNull();
        capturedTrend!.AnomalyCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeTrendAsync_ShouldEmitAnomalyMetric_WhenAnomaliesAreDetected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;
        var anomalyMeasurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == UsageTrendAnalysisService.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "gameguild.resources.usage_trends.anomalies")
            {
                anomalyMeasurements.Add(measurement);
            }
        });
        listener.Start();

        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 110, PeriodStart = periodStart.AddDays(5) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 105, PeriodStart = periodStart.AddDays(10) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 1000, PeriodStart = periodStart.AddDays(15) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart.AddDays(20) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);
        listener.RecordObservableInstruments();

        // Assert
        anomalyMeasurements.Should().Contain(measurement => measurement > 0);
    }

    [Fact]
    public async Task AnalyzeTrendAsync_ClassifiesPatternAsStable_WhenLowGrowthAndLowVariance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 101, PeriodStart = periodStart.AddDays(10) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = periodStart.AddDays(20) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, periodStart, periodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        ResourceUsageTrend? capturedTrend = null;
        _trendRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ResourceUsageTrend>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceUsageTrend, CancellationToken>((t, _) => capturedTrend = t)
            .ReturnsAsync((ResourceUsageTrend t, CancellationToken _) => t);

        // Act
        await _service.AnalyzeTrendAsync(tenantId, ResourceUsageType.Storage, periodStart, periodEnd);

        // Assert
        capturedTrend.Should().NotBeNull();
        capturedTrend!.Pattern.Should().Be("Stable");
    }

    [Fact]
    public async Task GetTenantTrendsAsync_ReturnsTrendsFromRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var trends = new List<ResourceUsageTrend>
        {
            new() { Id = Guid.NewGuid(), Type = ResourceUsageType.Storage },
            new() { Id = Guid.NewGuid(), Type = ResourceUsageType.ApiCalls }
        };

        _trendRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        // Act
        var result = await _service.GetTenantTrendsAsync(tenantId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTenantTrendsAsync_PassesDateFilters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var fromDate = DateTime.UtcNow.AddDays(-60);
        var toDate = DateTime.UtcNow.AddDays(-30);

        _trendRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, null, fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceUsageTrend>());

        // Act
        await _service.GetTenantTrendsAsync(tenantId, fromDate, toDate);

        // Assert
        _trendRepositoryMock.Verify(
            r => r.GetByTenantAsync(tenantId, null, fromDate, toDate, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DetectAnomaliesAsync_FiltersOnlyAnomalousRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var trends = new List<ResourceUsageTrend>
        {
            new() { Id = Guid.NewGuid(), AnomalyCount = 0, AverageUsage = 100, StandardDeviation = 10 },
            new() { Id = Guid.NewGuid(), AnomalyCount = 3, AverageUsage = 200, StandardDeviation = 20 },
            new() { Id = Guid.NewGuid(), AnomalyCount = 0, AverageUsage = 150, StandardDeviation = 5 }
        };

        _trendRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, null, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        // Act
        var result = await _service.DetectAnomaliesAsync(tenantId);

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(1);
        list.First().AnomalyCount.Should().Be(3);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_UsesSpecifiedLookbackDays()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var lookbackDays = 60;

        _trendRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, null, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceUsageTrend>())
            .Callback<Guid, ResourceUsageType?, DateTime?, DateTime?, CancellationToken>((_, _, fromDate, _, _) =>
            {
                // Verify the fromDate is approximately 60 days ago
                fromDate.Should().NotBeNull();
                var expectedDate = DateTime.UtcNow.AddDays(-lookbackDays);
                fromDate!.Value.Should().BeCloseTo(expectedDate, TimeSpan.FromMinutes(1));
            });

        // Act
        await _service.DetectAnomaliesAsync(tenantId, lookbackDays: lookbackDays);

        // Assert
        _trendRepositoryMock.Verify(
            r => r.GetByTenantAsync(tenantId, null, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DetectAnomaliesAsync_FiltersByType_WhenProvided()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Storage;

        _trendRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, type, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceUsageTrend>());

        // Act
        await _service.DetectAnomaliesAsync(tenantId, type);

        // Assert
        _trendRepositoryMock.Verify(
            r => r.GetByTenantAsync(tenantId, type, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ForecastUsageAsync_ReturnsZero_WhenNoUsageRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var targetDate = DateTime.UtcNow.AddDays(30);

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsageRecord>());

        // Act
        var result = await _service.ForecastUsageAsync(tenantId, ResourceUsageType.Storage, targetDate);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ForecastUsageAsync_CalculatesLinearRegressionForecast()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var targetDate = DateTime.UtcNow.AddDays(30);
        var baseDate = DateTime.UtcNow.AddDays(-90);

        // Create usage records with a clear upward trend
        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = baseDate },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 150, PeriodStart = baseDate.AddDays(30) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 200, PeriodStart = baseDate.AddDays(60) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 250, PeriodStart = baseDate.AddDays(90) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        // Act
        var result = await _service.ForecastUsageAsync(tenantId, ResourceUsageType.Storage, targetDate);

        // Assert
        result.Should().BeGreaterThan(0);
        // With a clear upward trend, forecast should be higher than the last observed value
        result.Should().BeGreaterThan(250);
    }

    [Fact]
    public async Task ForecastUsageAsync_ReturnsNonNegativeValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var targetDate = DateTime.UtcNow.AddDays(30);
        var baseDate = DateTime.UtcNow.AddDays(-90);

        // Create usage records with a downward trend that would forecast negative
        var usageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 1000, PeriodStart = baseDate },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 10, PeriodStart = baseDate.AddDays(30) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 5, PeriodStart = baseDate.AddDays(60) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(
                tenantId, ResourceUsageType.Storage, It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageRecords);

        // Act
        var result = await _service.ForecastUsageAsync(tenantId, ResourceUsageType.Storage, targetDate);

        // Assert - should clamp negative forecast to 0
        result.Should().BeGreaterOrEqualTo(0);
    }
}
