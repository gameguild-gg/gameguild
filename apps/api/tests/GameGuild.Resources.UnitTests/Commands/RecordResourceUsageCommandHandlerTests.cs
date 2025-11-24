using FluentAssertions;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Commands;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Commands;

public class RecordResourceUsageCommandHandlerTests
{
    private readonly Mock<IUsageRecordRepository> _usageRecordRepositoryMock;
    private readonly Mock<IResourceQuotaRepository> _resourceQuotaRepositoryMock;
    private readonly RecordResourceUsageCommandHandler _handler;

    public RecordResourceUsageCommandHandlerTests()
    {
        _usageRecordRepositoryMock = new Mock<IUsageRecordRepository>();
        _resourceQuotaRepositoryMock = new Mock<IResourceQuotaRepository>();
        _handler = new RecordResourceUsageCommandHandler(
            _usageRecordRepositoryMock.Object,
            _resourceQuotaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUsageRecord()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new RecordResourceUsageCommand(
            tenantId,
            ResourceUsageType.Storage,
            100,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            null);

        _usageRecordRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRecord record, CancellationToken _) => record);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _usageRecordRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingQuota_ShouldUpdateQuota()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new RecordResourceUsageCommand(
            tenantId,
            ResourceUsageType.Storage,
            100,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            null);

        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 50,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            LastReset = DateTime.UtcNow.AddDays(-1) // Prevent automatic reset
        };
        
        // Set TenantId using reflection - need NonPublic flag to access protected setter
        var tenantIdProperty = typeof(ResourceQuota).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { tenantId });

        _usageRecordRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageRecord record, CancellationToken _) => record);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quota);

        _resourceQuotaRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota q, CancellationToken _) => q);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _resourceQuotaRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify the quota usage was updated correctly
        quota.CurrentUsage.Should().Be(150);
    }

    [Fact]
    public async Task Handle_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var metadata = "{\"source\": \"api\"}";
        var command = new RecordResourceUsageCommand(
            tenantId,
            ResourceUsageType.ApiCalls,
            10,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow,
            metadata);

        UsageRecord? capturedRecord = null;
        _usageRecordRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<UsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback<UsageRecord, CancellationToken>((record, _) => capturedRecord = record)
            .ReturnsAsync((UsageRecord record, CancellationToken _) => record);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.ApiCalls, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRecord.Should().NotBeNull();
        capturedRecord!.Metadata.Should().Be(metadata);
        capturedRecord.Type.Should().Be(ResourceUsageType.ApiCalls);
        capturedRecord.Count.Should().Be(10);
    }
}
