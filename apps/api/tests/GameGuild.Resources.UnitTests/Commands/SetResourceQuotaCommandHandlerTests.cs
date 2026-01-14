using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Commands;

public class SetResourceQuotaCommandHandlerTests
{
    private readonly Mock<IResourceQuotaRepository> _resourceQuotaRepositoryMock;
    private readonly SetResourceQuotaCommandHandler _handler;

    public SetResourceQuotaCommandHandlerTests()
    {
        _resourceQuotaRepositoryMock = new Mock<IResourceQuotaRepository>();
        _handler = new SetResourceQuotaCommandHandler(_resourceQuotaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewQuota_ShouldCreateQuota()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new SetResourceQuotaCommand(
            tenantId,
            ResourceUsageType.Storage,
            SoftLimit: 800,
            HardLimit: 1000,
            ResourceQuotaPeriod.Monthly);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        _resourceQuotaRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota quota, CancellationToken _) => quota);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _resourceQuotaRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingQuota_ShouldUpdateQuota()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingQuota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 500,
            SoftLimit = 400,
            CurrentUsage = 100,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly
        };
        
        // Set TenantId using reflection - need NonPublic flag to access protected setter
        var tenantIdProperty = typeof(ResourceQuota).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(existingQuota, new object[] { tenantId });

        var command = new SetResourceQuotaCommand(
            tenantId,
            ResourceUsageType.Storage,
            SoftLimit: 800,
            HardLimit: 1000,
            ResourceQuotaPeriod.Monthly);

        _resourceQuotaRepositoryMock.Setup(x => x.GetByTenantAndTypeAsync(tenantId, ResourceUsageType.Storage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQuota);

        _resourceQuotaRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota quota, CancellationToken _) => quota);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _resourceQuotaRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ResourceQuota>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify the quota was updated with correct values
        existingQuota.HardLimit.Should().Be(1000);
        existingQuota.SoftLimit.Should().Be(800);
    }

    [Fact]
    public async Task Handle_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }
}
