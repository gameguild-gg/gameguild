using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Repositories;

public class ResourceQuotaRepositoryTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly ResourceQuotaRepository _repository;
    private readonly List<ResourceQuota> _quotasData;

    public ResourceQuotaRepositoryTests()
    {
        _quotasData = new List<ResourceQuota>();
        _contextMock = new Mock<IApplicationDbContext>();
        
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        
        _repository = new ResourceQuotaRepository(_contextMock.Object);
    }

    private void SetupDbSet()
    {
        var mockDbSet = _quotasData.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(x => x.Set<ResourceQuota>()).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task TryIncrementUsage_ReturnsFalse_WhenWouldExceedLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 8);
        _quotasData.Add(quota);
        SetupDbSet();

        // Act
        var (success, returnedQuota) = await _repository.TryIncrementUsageAsync(
            tenantId,
            resourceType,
            amount: 5, // Would bring total to 13, exceeding limit of 10
            CancellationToken.None);

        // Assert
        success.Should().BeFalse("incrementing by 5 would exceed hard limit of 10");
        returnedQuota.Should().NotBeNull();
        returnedQuota!.CurrentUsage.Should().Be(8, "usage should not have been incremented");
    }

    [Fact]
    public async Task TryIncrementUsage_ReturnsTrue_WhenWithinLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 5);
        _quotasData.Add(quota);
        SetupDbSet();

        // Act
        var (success, returnedQuota) = await _repository.TryIncrementUsageAsync(
            tenantId,
            resourceType,
            amount: 3, // Would bring total to 8, within limit of 10
            CancellationToken.None);

        // Assert
        success.Should().BeTrue("incrementing by 3 stays within hard limit of 10");
        returnedQuota.Should().NotBeNull();
        returnedQuota!.CurrentUsage.Should().Be(8, "usage should have been incremented from 5 to 8");
    }

    [Fact]
    public async Task TryIncrementUsage_ReturnsTrue_WhenNoQuotaExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;
        // No quota exists = unlimited
        SetupDbSet();

        // Act
        var (success, returnedQuota) = await _repository.TryIncrementUsageAsync(
            tenantId,
            resourceType,
            amount: 1000,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue("no quota means unlimited");
        returnedQuota.Should().BeNull("quota doesn't exist");
    }

    [Fact]
    public async Task TryIncrementUsage_ReturnsTrue_WhenExactlyAtLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 9);
        _quotasData.Add(quota);
        SetupDbSet();

        // Act
        var (success, returnedQuota) = await _repository.TryIncrementUsageAsync(
            tenantId,
            resourceType,
            amount: 1, // Would bring total to exactly 10
            CancellationToken.None);

        // Assert
        success.Should().BeTrue("incrementing to exactly the limit should succeed");
        returnedQuota.Should().NotBeNull();
        returnedQuota!.CurrentUsage.Should().Be(10, "usage should be exactly at limit");
    }

    [Fact]
    public async Task DecrementUsage_SuccessfullyDecrementsUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 7);
        _quotasData.Add(quota);
        SetupDbSet();

        // Act
        var result = await _repository.DecrementUsageAsync(
            tenantId,
            resourceType,
            amount: 3,
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        quota.CurrentUsage.Should().Be(4, "usage should have been decremented from 7 to 4");
    }

    [Fact]
    public async Task DecrementUsage_NeverGoesNegative_WhenAmountExceedsUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 3);
        _quotasData.Add(quota);
        SetupDbSet();

        // Act
        var result = await _repository.DecrementUsageAsync(
            tenantId,
            resourceType,
            amount: 10, // More than current usage
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        quota.CurrentUsage.Should().Be(0, "usage should be clamped to 0, never negative");
    }

    [Fact]
    public async Task DecrementUsage_ReturnsFalse_WhenQuotaNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;
        // No quota exists
        SetupDbSet();

        // Act
        var result = await _repository.DecrementUsageAsync(
            tenantId,
            resourceType,
            amount: 5,
            CancellationToken.None);

        // Assert
        result.Should().BeFalse("quota doesn't exist");
    }

    private ResourceQuota CreateQuota(
        Guid tenantId,
        ResourceUsageType type,
        long? hardLimit = null,
        long currentUsage = 0)
    {
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = type,
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
