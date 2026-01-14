using FluentAssertions;
using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Repositories;

public class ResourceQuotaRepositoryTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<DbSet<ResourceQuota>> _quotaDbSetMock;
    private readonly ResourceQuotaRepository _repository;
    private readonly List<ResourceQuota> _quotasData;

    public ResourceQuotaRepositoryTests()
    {
        _quotasData = new List<ResourceQuota>();
        _contextMock = new Mock<IApplicationDbContext>();
        _quotaDbSetMock = CreateDbSetMock(_quotasData);
        
        _contextMock.Setup(x => x.Set<ResourceQuota>()).Returns(_quotaDbSetMock.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        
        _repository = new ResourceQuotaRepository(_contextMock.Object);
    }

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task TryIncrementUsage_ReturnsFalse_WhenWouldExceedLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 8);
        _quotasData.Add(quota);

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task TryIncrementUsage_ReturnsTrue_WhenWithinLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 5);
        _quotasData.Add(quota);

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task TryIncrementUsage_ReturnsTrue_WhenNoQuotaExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;
        // No quota exists = unlimited

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task TryIncrementUsage_ReturnsTrue_WhenExactlyAtLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 9);
        _quotasData.Add(quota);

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task DecrementUsage_SuccessfullyDecrementsUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 7);
        _quotasData.Add(quota);

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task DecrementUsage_NeverGoesNegative_WhenAmountExceedsUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Users;
        var quota = CreateQuota(tenantId, resourceType, hardLimit: 10, currentUsage: 3);
        _quotasData.Add(quota);

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

    [Fact(Skip = "Requires MockQueryable.EntityFrameworkCore package or should be converted to integration test with real DbContext")]
    public async Task DecrementUsage_ReturnsFalse_WhenQuotaNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;
        // No quota exists

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

    private Mock<DbSet<T>> CreateDbSetMock<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        
        return mockSet;
    }
}
