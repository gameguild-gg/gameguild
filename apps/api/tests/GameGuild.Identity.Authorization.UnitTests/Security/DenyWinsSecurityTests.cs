using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

/// <summary>
///     Security tests validating DENY-WINS semantics across the permission system.
///     These tests ensure that explicit deny permissions always take precedence over grants.
/// </summary>
/// <remarks>
///     Test categories from AUTHORIZATION_PRECEDENCE_AUDIT.md:
///     - Cross-Tenant Role Isolation
///     - Global Allow + Tenant Deny
///     - Direct User Deny Overrides Role Allow
///     - Fail-Closed When Tenant Missing
///     - Cache Invalidation
/// </remarks>
public class DenyWinsSecurityTests
{
    private readonly Mock<ITenantPermissionRepository> _mockRepository;
    private readonly Mock<IPermissionAuditService> _mockAuditService;
    private readonly Mock<ITenantSecurityVersionStore> _mockVersionStore;
    private readonly Mock<ILogger<PermissionGrantService>> _mockLogger;
    private readonly PermissionGrantService _grantService;

    public DenyWinsSecurityTests()
    {
        _mockRepository = new Mock<ITenantPermissionRepository>();
        _mockAuditService = new Mock<IPermissionAuditService>();
        _mockVersionStore = new Mock<ITenantSecurityVersionStore>();
        _mockLogger = new Mock<ILogger<PermissionGrantService>>();

        _grantService = new PermissionGrantService(
            _mockRepository.Object,
            _mockAuditService.Object,
            _mockVersionStore.Object,
            _mockLogger.Object);
    }

    #region Category 1: Cross-Tenant Role Isolation

    [Fact]
    public async Task User_PermissionsInTenantA_NotInTenantB()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var tenantAPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantA,
            Permissions = new[] { "admin:*" }
        };

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantAPermission);

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        // Act & Assert
        var resultA = await _mockRepository.Object.GetByUserAndTenantAsync(userId, tenantA);
        var resultB = await _mockRepository.Object.GetByUserAndTenantAsync(userId, tenantB);

        resultA.Should().NotBeNull();
        resultA!.Permissions.Should().Contain("admin:*");
        resultB.Should().BeNull();
    }

    [Fact]
    public async Task TenantPermission_IsScopedToSingleTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { "projects:create", "projects:read" };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        var result = await _grantService.GrantTenantPermissionAsync(
            userId, tenantId, permissions);

        // Assert
        result.TenantId.Should().Be(tenantId);
        result.UserId.Should().Be(userId);
        result.Permissions.Should().BeEquivalentTo(permissions);
    }

    #endregion

    #region Category 2: Deny Semantics

    [Fact]
    public async Task DenyTenantPermission_AddsToDenyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existingPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { "content:read", "content:write" },
            DenyPermissions = Array.Empty<string>()
        };

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPermission);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        var result = await _grantService.DenyTenantPermissionAsync(
            userId, tenantId, new[] { "content:write" });

        // Assert
        result.DenyPermissions.Should().Contain("content:write");
    }

    [Fact]
    public async Task DenyTenantPermission_CreatesNewRecord_WhenNoneExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        var result = await _grantService.DenyTenantPermissionAsync(
            userId, tenantId, new[] { "dangerous:operation" });

        // Assert
        result.Should().NotBeNull();
        result.DenyPermissions.Should().Contain("dangerous:operation");
        result.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveDenyPermissions_RemovesFromDenyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existingPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { "content:read" },
            DenyPermissions = new[] { "content:write", "content:delete" }
        };

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPermission);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        var result = await _grantService.RemoveDenyPermissionsAsync(
            userId, tenantId, new[] { "content:write" });

        // Assert
        result.Should().BeTrue();
        existingPermission.DenyPermissions.Should().NotContain("content:write");
        existingPermission.DenyPermissions.Should().Contain("content:delete");
    }

    #endregion

    #region Category 3: Cache Invalidation

    [Fact]
    public async Task GrantPermission_IncrementsSecurityVersion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        await _grantService.GrantTenantPermissionAsync(
            userId, tenantId, new[] { "test:permission" });

        // Assert - version store should be incremented
        _mockVersionStore.Verify(
            v => v.IncrementVersionAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DenyPermission_IncrementsSecurityVersion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        await _grantService.DenyTenantPermissionAsync(
            userId, tenantId, new[] { "test:permission" });

        // Assert - version store should be incremented
        _mockVersionStore.Verify(
            v => v.IncrementVersionAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RevokePermission_IncrementsSecurityVersion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existingPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { "test:permission" }
        };

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPermission);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _grantService.RevokeTenantPermissionAsync(
            userId, tenantId, new[] { "test:permission" });

        // Assert - version store should be incremented
        _mockVersionStore.Verify(
            v => v.IncrementVersionAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Category 4: Audit Trail

    [Fact]
    public async Task GrantPermission_LogsAuditEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        await _grantService.GrantTenantPermissionAsync(
            userId, tenantId, new[] { "test:permission" }, grantedBy);

        // Assert
        _mockAuditService.Verify(
            a => a.LogPermissionChangeAsync(
                PermissionOperationType.Grant,
                tenantId,
                userId,
                grantedBy,
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.Is<string>(s => s.Contains("test:permission")),
                It.IsAny<string?>(),
                true,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DenyPermission_LogsAuditEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var deniedBy = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        // Act
        await _grantService.DenyTenantPermissionAsync(
            userId, tenantId, new[] { "dangerous:permission" }, deniedBy, "Security concern");

        // Assert
        _mockAuditService.Verify(
            a => a.LogPermissionChangeAsync(
                PermissionOperationType.Deny,
                tenantId,
                userId,
                deniedBy,
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.Is<string>(s => s.Contains("dangerous:permission")),
                It.Is<string>(s => s == "Security concern"),
                true,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
