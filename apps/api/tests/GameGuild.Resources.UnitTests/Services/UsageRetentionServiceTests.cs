using FluentAssertions;
using GameGuild.CQRS.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

public class UsageRetentionServiceTests
{
    private readonly Mock<IUsageRetentionPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IUsageRecordRepository> _usageRepositoryMock;
    private readonly Mock<ILogger<UsageRetentionService>> _loggerMock;
    private readonly UsageRetentionService _service;

    public UsageRetentionServiceTests()
    {
        _policyRepositoryMock = new Mock<IUsageRetentionPolicyRepository>();
        _usageRepositoryMock = new Mock<IUsageRecordRepository>();
        _loggerMock = new Mock<ILogger<UsageRetentionService>>();

        _service = new UsageRetentionService(
            _policyRepositoryMock.Object,
            _usageRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SetPolicyAsync_CreatesNewPolicy_WhenNotExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Storage;

        _policyRepositoryMock.Setup(r => r.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRetentionPolicy?)null);

        UsageRetentionPolicy? capturedPolicy = null;
        _policyRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .Callback<UsageRetentionPolicy, CancellationToken>((p, _) => capturedPolicy = p)
            .ReturnsAsync((UsageRetentionPolicy p, CancellationToken _) => p);

        // Act
        var result = await _service.SetPolicyAsync(tenantId, resourceType, 90, 30, true);

        // Assert
        result.Should().NotBeNull();
        capturedPolicy.Should().NotBeNull();
        capturedPolicy!.RetentionDays.Should().Be(90);
        capturedPolicy.ArchiveAfterDays.Should().Be(30);
        capturedPolicy.EnableCompaction.Should().BeTrue();
        capturedPolicy.IsActive.Should().BeTrue();
        capturedPolicy.ResourceType.Should().Be(resourceType);
    }

    [Fact]
    public async Task SetPolicyAsync_UpdatesExistingPolicy_WhenExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.ApiCalls;
        var existingPolicy = new UsageRetentionPolicy
        {
            Id = Guid.NewGuid(),
            RetentionDays = 60,
            ArchiveAfterDays = 20,
            EnableCompaction = false
        };

        _policyRepositoryMock.Setup(r => r.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPolicy);

        _policyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRetentionPolicy p, CancellationToken _) => p);

        // Act
        var result = await _service.SetPolicyAsync(tenantId, resourceType, 120, 45, true);

        // Assert
        result.Should().NotBeNull();
        result.RetentionDays.Should().Be(120);
        result.ArchiveAfterDays.Should().Be(45);
        result.EnableCompaction.Should().BeTrue();
        _policyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPolicyAsync_CreatesGlobalPolicy_WhenNoTenantSpecified()
    {
        // Arrange
        _policyRepositoryMock.Setup(r => r.GetByTenantAndTypeAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRetentionPolicy?)null);

        UsageRetentionPolicy? capturedPolicy = null;
        _policyRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .Callback<UsageRetentionPolicy, CancellationToken>((p, _) => capturedPolicy = p)
            .ReturnsAsync((UsageRetentionPolicy p, CancellationToken _) => p);

        // Act
        var result = await _service.SetPolicyAsync(null, null, 365, 180);

        // Assert
        result.Should().NotBeNull();
        capturedPolicy.Should().NotBeNull();
        capturedPolicy!.Name.Should().Contain("Global");
        capturedPolicy.RetentionDays.Should().Be(365);
        capturedPolicy.ArchiveAfterDays.Should().Be(180);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsPolicy_WhenExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceType = ResourceUsageType.Projects;
        var policy = new UsageRetentionPolicy { Id = Guid.NewGuid(), RetentionDays = 90 };

        _policyRepositoryMock.Setup(r => r.GetByTenantAndTypeAsync(tenantId, resourceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        // Act
        var result = await _service.GetPolicyAsync(tenantId, resourceType);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(policy.Id);
        result.RetentionDays.Should().Be(90);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _policyRepositoryMock.Setup(r => r.GetByTenantAndTypeAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRetentionPolicy?)null);

        // Act
        var result = await _service.GetPolicyAsync(tenantId, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActivePoliciesAsync_ReturnsActivePolicies()
    {
        // Arrange
        var policies = new List<UsageRetentionPolicy>
        {
            new() { Id = Guid.NewGuid(), IsActive = true },
            new() { Id = Guid.NewGuid(), IsActive = true }
        };

        _policyRepositoryMock.Setup(r => r.GetActivePoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policies);

        // Act
        var result = await _service.GetActivePoliciesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ExecuteRetentionAsync_ReturnsEmptyResult_WhenPolicyNotFound()
    {
        // Arrange
        var policyId = Guid.NewGuid();

        _policyRepositoryMock.Setup(r => r.GetByIdAsync(policyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRetentionPolicy?)null);

        // Act
        var result = await _service.ExecuteRetentionAsync(policyId);

        // Assert
        result.Should().NotBeNull();
        result.RecordsArchived.Should().Be(0);
        result.RecordsDeleted.Should().Be(0);
        result.RecordsCompacted.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteRetentionAsync_ReturnsEmptyResult_WhenPolicyInactive()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policy = new UsageRetentionPolicy { Id = policyId, IsActive = false };

        _policyRepositoryMock.Setup(r => r.GetByIdAsync(policyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        // Act
        var result = await _service.ExecuteRetentionAsync(policyId);

        // Assert
        result.Should().NotBeNull();
        result.RecordsArchived.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteRetentionAsync_ArchivesAndDeletesRecords_WhenPolicyActive()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policy = new UsageRetentionPolicy
        {
            Id = policyId,
            IsActive = true,
            RetentionDays = 90,
            ArchiveAfterDays = 30,
            EnableCompaction = false
        };

        _policyRepositoryMock.Setup(r => r.GetByIdAsync(policyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        _usageRepositoryMock.Setup(r => r.ArchiveOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(150);

        _usageRepositoryMock.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _policyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        // Act
        var result = await _service.ExecuteRetentionAsync(policyId);

        // Assert
        result.Should().NotBeNull();
        result.RecordsArchived.Should().Be(150);
        result.RecordsDeleted.Should().Be(1);
        result.RecordsCompacted.Should().Be(0);
        _policyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteRetentionAsync_CompactsRecords_WhenCompactionEnabled()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var policy = new UsageRetentionPolicy
        {
            Id = policyId,
            IsActive = true,
            RetentionDays = 90,
            ArchiveAfterDays = 30,
            EnableCompaction = true,
            ResourceType = ResourceUsageType.Storage
        };
        policy.SetTenantId(tenantId);

        _policyRepositoryMock.Setup(r => r.GetByIdAsync(policyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        _usageRepositoryMock.Setup(r => r.ArchiveOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        _usageRepositoryMock.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var oldRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 1000, PeriodStart = DateTime.UtcNow.AddDays(-45) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 1200, PeriodStart = DateTime.UtcNow.AddDays(-40) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 800, PeriodStart = DateTime.UtcNow.AddDays(-35) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(tenantId, ResourceUsageType.Storage, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldRecords);

        _usageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRecord r, CancellationToken _) => r);

        _policyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UsageRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        // Act
        var result = await _service.ExecuteRetentionAsync(policyId);

        // Assert
        result.Should().NotBeNull();
        result.RecordsCompacted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompactUsageRecordsAsync_ReturnsZero_WhenNoTenantId()
    {
        // Act
        var result = await _service.CompactUsageRecordsAsync(Guid.Empty);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CompactUsageRecordsAsync_ReturnsZero_WhenNoRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsageRecord>());

        // Act
        var result = await _service.CompactUsageRecordsAsync(tenantId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CompactUsageRecordsAsync_CreatesMonthlyAggregates_WhenRecordsExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var baseDate = new DateTime(2025, 1, 15);

        var records = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = baseDate },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 150, PeriodStart = baseDate.AddDays(5) },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 200, PeriodStart = baseDate.AddDays(10) },
            new() { Type = ResourceUsageType.ApiCalls, UsageAmount = 500, PeriodStart = baseDate }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        _usageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRecord r, CancellationToken _) => r);

        _usageRepositoryMock.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CompactUsageRecordsAsync(tenantId);

        // Assert
        result.Should().Be(2); // 2 monthly aggregates (Storage and ApiCalls for same month)
        _usageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CompactUsageRecordsAsync_FiltersbyType_WhenTypeSpecified()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var baseDate = new DateTime(2025, 1, 10);

        var storageRecords = new List<UsageRecord>
        {
            new() { Type = ResourceUsageType.Storage, UsageAmount = 100, PeriodStart = baseDate },
            new() { Type = ResourceUsageType.Storage, UsageAmount = 150, PeriodStart = baseDate.AddDays(5) }
        };

        _usageRepositoryMock.Setup(r => r.GetByTenantAsync(tenantId, ResourceUsageType.Storage, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageRecords);

        _usageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRecord r, CancellationToken _) => r);

        _usageRepositoryMock.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CompactUsageRecordsAsync(tenantId, ResourceUsageType.Storage);

        // Assert
        result.Should().Be(1); // 1 monthly aggregate for Storage
        _usageRepositoryMock.Verify(r => r.GetByTenantAsync(tenantId, ResourceUsageType.Storage, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
