using FluentAssertions;
using GameGuild.Resources;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Queries;

public class GetResourceQuotaQueryHandlerTests
{
    private readonly Mock<IResourceQuotaRepository> _resourceQuotaRepositoryMock;
    private readonly GetResourceQuotaQueryHandler _handler;

    public GetResourceQuotaQueryHandlerTests()
    {
        _resourceQuotaRepositoryMock = new Mock<IResourceQuotaRepository>();
        _handler = new GetResourceQuotaQueryHandler(_resourceQuotaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingQuota_ShouldReturnQuotaResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 500,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            LastReset = DateTime.UtcNow.AddDays(-15)
        };
        
        // Set TenantId using reflection - need NonPublic flag to access protected setter
        var tenantIdProperty = typeof(ResourceQuota).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { tenantId });

        var query = new GetResourceQuotaQuery(tenantId, ResourceUsageType.Storage);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(quota.Id);
        result.TenantId.Should().Be(tenantId);
        result.Type.Should().Be(ResourceUsageType.Storage);
        result.Limit.Should().Be(1000);
        result.CurrentUsage.Should().Be(500);
        result.RemainingQuota.Should().Be(500);
        result.UsagePercentage.Should().Be(50m);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistingQuota_ShouldReturnNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetResourceQuotaQuery(tenantId, ResourceUsageType.Storage);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNullQuery_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithExceededSoftLimit_ShouldIndicateSoftLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.ApiCalls,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 850,
            IsActive = true,
            Period = ResourceQuotaPeriod.Daily
        };

        var query = new GetResourceQuotaQuery(tenantId, ResourceUsageType.ApiCalls);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.ApiCalls, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsSoftLimitExceeded.Should().BeTrue();
        result.IsHardLimitExceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExceededHardLimit_ShouldIndicateHardLimitExceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 1050,
            IsActive = true,
            Period = ResourceQuotaPeriod.Daily
        };

        var query = new GetResourceQuotaQuery(tenantId, ResourceUsageType.Storage);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsHardLimitExceeded.Should().BeTrue();
        result.IsSoftLimitExceeded.Should().BeTrue();
    }
}
