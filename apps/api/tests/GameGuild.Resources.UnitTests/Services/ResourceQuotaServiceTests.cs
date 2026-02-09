using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Services;

/// <summary>
///     Unit tests for the <see cref="ResourceQuotaService"/> facade.
///     Verifies that every method delegates to the correct sub-service.
/// </summary>
public class ResourceQuotaServiceTests
{
    private readonly Mock<IQuotaManagementService> _managementMock;
    private readonly Mock<IQuotaEnforcementService> _enforcementMock;
    private readonly Mock<IQuotaMaintenanceService> _maintenanceMock;
    private readonly ResourceQuotaService _service;

    public ResourceQuotaServiceTests()
    {
        _managementMock = new Mock<IQuotaManagementService>();
        _enforcementMock = new Mock<IQuotaEnforcementService>();
        _maintenanceMock = new Mock<IQuotaMaintenanceService>();
        _service = new ResourceQuotaService(
            _managementMock.Object,
            _enforcementMock.Object,
            _maintenanceMock.Object);
    }

    #region IResourceQuotaReader delegation (management)

    [Fact]
    public async Task GetQuotaAsync_DelegatesToManagement()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;
        var expected = new ResourceQuota { Id = Guid.NewGuid(), Type = type };

        _managementMock
            .Setup(x => x.GetQuotaAsync(tenantId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetQuotaAsync(tenantId, type);

        // Assert
        result.Should().BeSameAs(expected);
        _managementMock.Verify(x => x.GetQuotaAsync(tenantId, type, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenantQuotasAsync_DelegatesToManagement()
    {
        var tenantId = Guid.NewGuid();
        var expected = new[] { new ResourceQuota() };

        _managementMock
            .Setup(x => x.GetTenantQuotasAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetTenantQuotasAsync(tenantId);

        result.Should().BeSameAs(expected);
        _managementMock.Verify(x => x.GetTenantQuotasAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUsageAsync_DelegatesToManagement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Storage;

        _managementMock
            .Setup(x => x.GetCurrentUsageAsync(tenantId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        var result = await _service.GetCurrentUsageAsync(tenantId, type);

        result.Should().Be(42L);
        _managementMock.Verify(x => x.GetCurrentUsageAsync(tenantId, type, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUsageHistoryAsync_DelegatesToManagement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.ApiCalls;
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var expected = new[] { new UsageRecord() };

        _managementMock
            .Setup(x => x.GetUsageHistoryAsync(tenantId, type, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetUsageHistoryAsync(tenantId, type, from, to);

        result.Should().BeSameAs(expected);
        _managementMock.Verify(x => x.GetUsageHistoryAsync(tenantId, type, from, to, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IResourceQuotaWriter delegation (management)

    [Fact]
    public async Task SetQuotaAsync_DelegatesToManagement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Projects;
        var expected = new ResourceQuota { Id = Guid.NewGuid(), Type = type };

        _managementMock
            .Setup(x => x.SetQuotaAsync(tenantId, type, 80, 100, ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.SetQuotaAsync(tenantId, type, 80, 100);

        result.Should().BeSameAs(expected);
        _managementMock.Verify(
            x => x.SetQuotaAsync(tenantId, type, 80, 100, ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteQuotaAsync_DelegatesToManagement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;

        _managementMock
            .Setup(x => x.DeleteQuotaAsync(tenantId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteQuotaAsync(tenantId, type);

        result.Should().BeTrue();
        _managementMock.Verify(x => x.DeleteQuotaAsync(tenantId, type, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IResourceQuotaEnforcer delegation (enforcement)

    [Fact]
    public async Task CheckLimitsAsync_DelegatesToEnforcement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;
        var expected = new ResourceLimitCheckResponse
        {
            CanProceed = false,
            CurrentUsage = 10,
            HardLimit = 10
        };

        _enforcementMock
            .Setup(x => x.CheckLimitsAsync(tenantId, type, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.CheckLimitsAsync(tenantId, type, 5);

        result.Should().BeSameAs(expected);
        _enforcementMock.Verify(x => x.CheckLimitsAsync(tenantId, type, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckMultipleLimitsAsync_DelegatesToEnforcement()
    {
        var tenantId = Guid.NewGuid();
        var requestedAmounts = new Dictionary<ResourceUsageType, long>
        {
            [ResourceUsageType.Users] = 1L,
            [ResourceUsageType.Projects] = 1L
        };
        var expected = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>
        {
            [ResourceUsageType.Users] = new() { CanProceed = true },
            [ResourceUsageType.Projects] = new() { CanProceed = true }
        };

        _enforcementMock
            .Setup(x => x.CheckMultipleLimitsAsync(tenantId, requestedAmounts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

        result.Should().BeSameAs(expected);
        _enforcementMock.Verify(
            x => x.CheckMultipleLimitsAsync(tenantId, requestedAmounts, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryConsumeResourceAsync_DelegatesToEnforcement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Storage;
        var userId = Guid.NewGuid();
        var expected = new ResourceLimitCheckResponse { CanProceed = true, CurrentUsage = 5 };

        _enforcementMock
            .Setup(x => x.TryConsumeResourceAsync(tenantId, type, 1, userId, "test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.TryConsumeResourceAsync(tenantId, type, 1, userId, "test");

        result.Should().BeSameAs(expected);
        _enforcementMock.Verify(
            x => x.TryConsumeResourceAsync(tenantId, type, 1, userId, "test", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryAtomicConsumeAsync_DelegatesToEnforcement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;

        _enforcementMock
            .Setup(x => x.TryAtomicConsumeAsync(tenantId, type, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5L, (long?)10L));

        var (success, usage, limit) = await _service.TryAtomicConsumeAsync(tenantId, type, 1);

        success.Should().BeTrue();
        usage.Should().Be(5L);
        limit.Should().Be(10L);
        _enforcementMock.Verify(
            x => x.TryAtomicConsumeAsync(tenantId, type, 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecrementUsageAsync_DelegatesToEnforcement()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;
        var userId = Guid.NewGuid();

        _enforcementMock
            .Setup(x => x.DecrementUsageAsync(tenantId, type, 1, userId, "delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DecrementUsageAsync(tenantId, type, 1, userId, "delete");

        result.Should().BeTrue();
        _enforcementMock.Verify(
            x => x.DecrementUsageAsync(tenantId, type, 1, userId, "delete", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region IResourceQuotaAnalytics + IResourceQuotaMaintenance delegation (maintenance)

    [Fact]
    public async Task GetResourceUsageDetailsAsync_DelegatesToMaintenance()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Users;
        var expected = new ResourceUsageResponse();

        _maintenanceMock
            .Setup(x => x.GetResourceUsageDetailsAsync(tenantId, type, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetResourceUsageDetailsAsync(tenantId, type);

        result.Should().BeSameAs(expected);
        _maintenanceMock.Verify(
            x => x.GetResourceUsageDetailsAsync(tenantId, type, 30, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTenantsExceedingLimitsAsync_DelegatesToMaintenance()
    {
        var type = ResourceUsageType.Users;
        var expected = new[] { Guid.NewGuid() }.AsEnumerable();

        _maintenanceMock
            .Setup(x => x.GetTenantsExceedingLimitsAsync(type, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetTenantsExceedingLimitsAsync(type, true);

        result.Should().BeSameAs(expected);
        _maintenanceMock.Verify(
            x => x.GetTenantsExceedingLimitsAsync(type, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetExpiredQuotasAsync_DelegatesToMaintenance()
    {
        _maintenanceMock
            .Setup(x => x.ResetExpiredQuotasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _service.ResetExpiredQuotasAsync();

        result.Should().Be(3);
        _maintenanceMock.Verify(x => x.ResetExpiredQuotasAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupOldUsageRecordsAsync_DelegatesToMaintenance()
    {
        var olderThan = DateTime.UtcNow.AddDays(-90);

        _maintenanceMock
            .Setup(x => x.CleanupOldUsageRecordsAsync(olderThan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _service.CleanupOldUsageRecordsAsync(olderThan);

        result.Should().Be(42);
        _maintenanceMock.Verify(
            x => x.CleanupOldUsageRecordsAsync(olderThan, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecalculateUsageAsync_DelegatesToMaintenance()
    {
        var tenantId = Guid.NewGuid();
        var type = ResourceUsageType.Storage;

        _maintenanceMock
            .Setup(x => x.RecalculateUsageAsync(tenantId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.RecalculateUsageAsync(tenantId, type);

        result.Should().BeTrue();
        _maintenanceMock.Verify(
            x => x.RecalculateUsageAsync(tenantId, type, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Cross-cutting: no sub-service receives calls meant for another

    [Fact]
    public async Task EnforcementCalls_DoNotTouchManagementOrMaintenance()
    {
        var tenantId = Guid.NewGuid();
        _enforcementMock
            .Setup(x => x.CheckLimitsAsync(tenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse { CanProceed = true });

        await _service.CheckLimitsAsync(tenantId, ResourceUsageType.Users);

        _managementMock.VerifyNoOtherCalls();
        _maintenanceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManagementCalls_DoNotTouchEnforcementOrMaintenance()
    {
        var tenantId = Guid.NewGuid();
        _managementMock
            .Setup(x => x.GetQuotaAsync(tenantId, ResourceUsageType.Users, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceQuota?)null);

        await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);

        _enforcementMock.VerifyNoOtherCalls();
        _maintenanceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MaintenanceCalls_DoNotTouchManagementOrEnforcement()
    {
        _maintenanceMock
            .Setup(x => x.ResetExpiredQuotasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _service.ResetExpiredQuotasAsync();

        _managementMock.VerifyNoOtherCalls();
        _enforcementMock.VerifyNoOtherCalls();
    }

    #endregion
}
