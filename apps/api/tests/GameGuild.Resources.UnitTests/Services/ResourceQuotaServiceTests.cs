using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

public class ResourceQuotaServiceTests
{
    private readonly Mock<IResourceQuotaRepository> _quotaRepositoryMock;
    private readonly Mock<IUsageRecordRepository> _usageRepositoryMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<ILogger<ResourceQuotaService>> _loggerMock;
    private readonly ResourceQuotaService _service;

    public ResourceQuotaServiceTests()
    {
        _quotaRepositoryMock = new Mock<IResourceQuotaRepository>();
        _usageRepositoryMock = new Mock<IUsageRecordRepository>();
        _publisherMock = new Mock<IPublisher>();
        _loggerMock = new Mock<ILogger<ResourceQuotaService>>();
        _service = new ResourceQuotaService(
            _quotaRepositoryMock.Object,
            _usageRepositoryMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CheckLimitsAsync_ReturnsCanProceedFalse_WhenHardLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var requestedAmount = 5L;

        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 10);
        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _service.CheckLimitsAsync(tenantId, resourceType, requestedAmount);

        // Assert
        result.Should().NotBeNull();
        result.CanProceed.Should().BeFalse();
        result.CurrentUsage.Should().Be(10);
        result.HardLimit.Should().Be(10);
    }

    [Fact]
    public async Task CheckLimitsAsync_ReturnsCanProceedTrue_WhenWithinLimits()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var requestedAmount = 3L;

        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 5);
        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _service.CheckLimitsAsync(tenantId, resourceType, requestedAmount);

        // Assert
        result.Should().NotBeNull();
        result.CanProceed.Should().BeTrue();
        result.CurrentUsage.Should().Be(5);
        result.HardLimit.Should().Be(10);
    }

    [Fact]
    public async Task CheckLimitsAsync_ReturnsCanProceedTrue_WhenNoQuotaExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var requestedAmount = 100L;

        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        // Act
        var result = await _service.CheckLimitsAsync(tenantId, resourceType, requestedAmount);

        // Assert
        result.Should().NotBeNull();
        result.CanProceed.Should().BeTrue("no quota means unlimited");
        result.CurrentUsage.Should().Be(0);
        result.HardLimit.Should().BeNull();
    }

    [Fact]
    public async Task CheckLimitsAsync_ReturnsSoftLimitWarning_WhenApproachingLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;
        var requestedAmount = 100L;

        var quota = CreateQuota(tenantId, resourceType, softLimit: 800, hardLimit: 1000, currentUsage: 750);
        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _service.CheckLimitsAsync(tenantId, resourceType, requestedAmount);

        // Assert
        result.Should().NotBeNull();
        result.CanProceed.Should().BeTrue();
        result.CurrentUsage.Should().Be(750);
        result.SoftLimit.Should().Be(800);
    }

    private ResourceQuota CreateQuota(
        Guid tenantId,
        ResourceUsageType type,
        long? softLimit = null,
        long? hardLimit = null,
        long currentUsage = 0)
    {
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = type,
            SoftLimit = softLimit,
            HardLimit = hardLimit,
            CurrentUsage = currentUsage,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            LastReset = DateTime.UtcNow.AddDays(-1)
        };

        quota.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });
        return quota;
    }
}
