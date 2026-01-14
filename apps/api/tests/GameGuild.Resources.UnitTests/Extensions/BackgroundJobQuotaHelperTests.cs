using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Extensions;

/// <summary>
/// Unit tests for BackgroundJobQuotaHelper
/// </summary>
public class BackgroundJobQuotaHelperTests
{
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public BackgroundJobQuotaHelperTests()
    {
        _quotaServiceMock = new Mock<IResourceQuotaService>();
    }

    #region WithQuotaEnforcementAsync (void action)

    [Fact]
    public async Task WithQuotaEnforcementAsync_ExecutesAction_WhenQuotaAllowed()
    {
        // Arrange
        var actionExecuted = false;
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5L, 10L));

        // Act
        await _quotaServiceMock.Object.WithQuotaEnforcementAsync(
            _tenantId,
            ResourceUsageType.Users,
            1,
            () => { actionExecuted = true; return Task.CompletedTask; },
            "TestSource");

        // Assert
        actionExecuted.Should().BeTrue();
        _quotaServiceMock.Verify(
            x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()),
            Times.Once);
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Users, 1, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WithQuotaEnforcementAsync_ThrowsQuotaExceeded_WhenQuotaBlocked()
    {
        // Arrange
        var actionExecuted = false;
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 10L, 10L));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<QuotaExceededException>(
            () => _quotaServiceMock.Object.WithQuotaEnforcementAsync(
                _tenantId,
                ResourceUsageType.Users,
                1,
                () => { actionExecuted = true; return Task.CompletedTask; }));

        actionExecuted.Should().BeFalse("action should not execute when quota is exceeded");
        exception.ResourceType.Should().Be(ResourceUsageType.Users);
        exception.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task WithQuotaEnforcementAsync_RollsBackQuota_WhenActionFails()
    {
        // Arrange
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5L, 10L));

        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Users, 5, null, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _quotaServiceMock.Object.WithQuotaEnforcementAsync(
                _tenantId,
                ResourceUsageType.Users,
                5,
                () => throw new InvalidOperationException("Simulated failure"),
                "TestSource"));

        // Verify quota was rolled back
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Users, 5, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WithQuotaEnforcementAsync_ThrowsArgumentException_ForZeroAmount()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _quotaServiceMock.Object.WithQuotaEnforcementAsync(
                _tenantId,
                ResourceUsageType.Users,
                0,
                () => Task.CompletedTask));
    }

    #endregion

    #region WithQuotaEnforcementAsync<T> (returning action)

    [Fact]
    public async Task WithQuotaEnforcementAsync_Generic_ReturnsResult_WhenQuotaAllowed()
    {
        // Arrange
        var expectedResult = new { Id = Guid.NewGuid(), Name = "Test" };
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Projects, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 1L, 10L));

        // Act
        var result = await _quotaServiceMock.Object.WithQuotaEnforcementAsync(
            _tenantId,
            ResourceUsageType.Projects,
            1,
            () => Task.FromResult(expectedResult));

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task WithQuotaEnforcementAsync_Generic_RollsBackQuota_WhenActionFails()
    {
        // Arrange
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Projects, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 3L, 10L));

        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Projects, 3, null, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _quotaServiceMock.Object.WithQuotaEnforcementAsync(
                _tenantId,
                ResourceUsageType.Projects,
                3,
                () => Task.FromException<object>(new InvalidOperationException("Simulated failure"))));

        // Verify quota was rolled back
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Projects, 3, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region WithBatchQuotaEnforcementAsync

    [Fact]
    public async Task WithBatchQuotaEnforcementAsync_ProcessesAllItems_WhenQuotaAllowed()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 3L, 10L));

        // Act
        var (successful, failed) = await _quotaServiceMock.Object.WithBatchQuotaEnforcementAsync(
            _tenantId,
            ResourceUsageType.Users,
            items,
            item => Task.FromResult<(bool, string?)>((true, $"processed-{item}")));

        // Assert
        successful.Should().HaveCount(3);
        failed.Should().BeEmpty();
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "no decrement when all items succeed");
    }

    [Fact]
    public async Task WithBatchQuotaEnforcementAsync_ReleasesQuota_ForFailedItems()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3", "item4", "item5" };
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5L, 10L));

        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Users, 2, null, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: 3 succeed, 2 fail
        var (successful, failed) = await _quotaServiceMock.Object.WithBatchQuotaEnforcementAsync(
            _tenantId,
            ResourceUsageType.Users,
            items,
            item => Task.FromResult(item.EndsWith("4") || item.EndsWith("5") 
                ? (false, (string?)null) 
                : (true, $"processed-{item}")));

        // Assert
        successful.Should().HaveCount(3);
        failed.Should().HaveCount(2);
        
        // Verify quota was decremented for the 2 failed items
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(_tenantId, ResourceUsageType.Users, 2, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WithBatchQuotaEnforcementAsync_ThrowsQuotaExceeded_WhenQuotaBlocked()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };
        _quotaServiceMock
            .Setup(x => x.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.Users, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 8L, 10L));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<QuotaExceededException>(
            () => _quotaServiceMock.Object.WithBatchQuotaEnforcementAsync(
                _tenantId,
                ResourceUsageType.Users,
                items,
                item => Task.FromResult<(bool, string?)>((true, $"processed-{item}"))));

        exception.Message.Should().Contain("batch of 3");
    }

    [Fact]
    public async Task WithBatchQuotaEnforcementAsync_ReturnsEmpty_ForEmptyInput()
    {
        // Arrange
        var items = Array.Empty<string>();

        // Act
        var (successful, failed) = await _quotaServiceMock.Object.WithBatchQuotaEnforcementAsync(
            _tenantId,
            ResourceUsageType.Users,
            items,
            item => Task.FromResult<(bool, string?)>((true, $"processed-{item}")));

        // Assert
        successful.Should().BeEmpty();
        failed.Should().BeEmpty();
        _quotaServiceMock.Verify(
            x => x.TryAtomicConsumeAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "no quota consumed for empty batch");
    }

    #endregion
}
