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

    #region Batch Query Tests - N+1 Fix Verification

    [Fact]
    public async Task CheckMultipleLimitsAsync_UsesSingleBatchQuery()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var requestedAmounts = new Dictionary<ResourceUsageType, long>
        {
            [ResourceUsageType.Users] = 1L,
            [ResourceUsageType.Projects] = 1L,
            [ResourceUsageType.Storage] = 1L
        };
        
        var quotas = new Dictionary<ResourceUsageType, ResourceQuota>
        {
            [ResourceUsageType.Users] = CreateQuota(tenantId, ResourceUsageType.Users, hardLimit: 100, currentUsage: 50),
            [ResourceUsageType.Projects] = CreateQuota(tenantId, ResourceUsageType.Projects, hardLimit: 10, currentUsage: 5),
            [ResourceUsageType.Storage] = CreateQuota(tenantId, ResourceUsageType.Storage, hardLimit: 1000, currentUsage: 200)
        };

        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypesAsync(tenantId, It.IsAny<IEnumerable<ResourceUsageType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        // Act
        var results = await _service.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

        // Assert - Should use batch query (single call), not N individual calls
        _quotaRepositoryMock.Verify(
            x => x.GetByTenantAndTypesAsync(tenantId, It.IsAny<IEnumerable<ResourceUsageType>>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should use batch query instead of N individual queries");
        
        // Should NOT call individual GetByTenantAndTypeAsync
        _quotaRepositoryMock.Verify(
            x => x.GetByTenantAndTypeAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Batch query should avoid N+1 individual queries");

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task CheckMultipleLimitsAsync_ReturnsResultsForAllTypes()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var requestedAmounts = new Dictionary<ResourceUsageType, long>
        {
            [ResourceUsageType.Users] = 1L,
            [ResourceUsageType.Projects] = 1L
        };
        
        var quotas = new Dictionary<ResourceUsageType, ResourceQuota>
        {
            [ResourceUsageType.Users] = CreateQuota(tenantId, ResourceUsageType.Users, hardLimit: 100, currentUsage: 50),
            [ResourceUsageType.Projects] = CreateQuota(tenantId, ResourceUsageType.Projects, hardLimit: 10, currentUsage: 5)
        };

        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypesAsync(tenantId, It.IsAny<IEnumerable<ResourceUsageType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        // Act
        var results = await _service.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

        // Assert
        results.Should().ContainKey(ResourceUsageType.Users);
        results.Should().ContainKey(ResourceUsageType.Projects);
        results[ResourceUsageType.Users].CanProceed.Should().BeTrue();
        results[ResourceUsageType.Projects].CanProceed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckMultipleLimitsAsync_HandlesPartialQuotas()
    {
        // Arrange - Only some resource types have quotas defined
        var tenantId = Guid.NewGuid();
        var requestedAmounts = new Dictionary<ResourceUsageType, long>
        {
            [ResourceUsageType.Users] = 1L,
            [ResourceUsageType.Projects] = 1L,
            [ResourceUsageType.ApiCalls] = 1L
        };
        
        // Only Users and Projects have quotas, ApiCalls does not
        var quotas = new Dictionary<ResourceUsageType, ResourceQuota>
        {
            [ResourceUsageType.Users] = CreateQuota(tenantId, ResourceUsageType.Users, hardLimit: 100, currentUsage: 50),
            [ResourceUsageType.Projects] = CreateQuota(tenantId, ResourceUsageType.Projects, hardLimit: 10, currentUsage: 5)
        };

        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypesAsync(tenantId, It.IsAny<IEnumerable<ResourceUsageType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        // Act
        var results = await _service.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

        // Assert - Should return results for all requested types
        results.Should().HaveCount(3);
        results[ResourceUsageType.Users].HardLimit.Should().Be(100);
        results[ResourceUsageType.Projects].HardLimit.Should().Be(10);
        results[ResourceUsageType.ApiCalls].HardLimit.Should().BeNull("no quota defined means unlimited");
        results[ResourceUsageType.ApiCalls].CanProceed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckMultipleLimitsAsync_EmptyTypes_ReturnsEmptyDictionary()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var requestedAmounts = new Dictionary<ResourceUsageType, long>();

        _quotaRepositoryMock
            .Setup(x => x.GetByTenantAndTypesAsync(tenantId, It.IsAny<IEnumerable<ResourceUsageType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ResourceUsageType, ResourceQuota>());

        // Act
        var results = await _service.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion
}
