using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class PermissionQueryServiceTests
{
    private readonly Mock<ITenantPermissionRepository> _repoMock = new();
    private readonly Mock<ITenantMembershipChecker> _membershipMock = new();
    private readonly Mock<IAuthorizationRolePermissionProvider> _rolePermissionsMock = new();
    private readonly PermissionQueryService _sut;

    public PermissionQueryServiceTests()
    {
        _sut = new PermissionQueryService(
            _repoMock.Object,
            _membershipMock.Object,
            NullLogger<PermissionQueryService>.Instance,
            [_rolePermissionsMock.Object]
        );
    }

    // ── HasTenantPermissionAsync ──────────────────────────────

    [Fact]
    public async Task HasTenantPermissionAsync_NoRecord_ReturnsFalse()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.HasTenantPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), "read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasTenantPermissionAsync_HasPermission_ReturnsTrue()
    {
        var permission = new TenantPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read", "write" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(permission.UserId, permission.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var result = await _sut.HasTenantPermissionAsync(permission.UserId, permission.TenantId, "read");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasTenantPermissionAsync_DoesNotHavePermission_ReturnsFalse()
    {
        var permission = new TenantPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(permission.UserId, permission.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var result = await _sut.HasTenantPermissionAsync(permission.UserId, permission.TenantId, "delete");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasTenantPermissionAsync_ExpiredPermission_ReturnsFalse()
    {
        var permission = new TenantPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read" },
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(permission.UserId, permission.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var result = await _sut.HasTenantPermissionAsync(permission.UserId, permission.TenantId, "read");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasTenantPermissionAsync_CustomRoleGrantsPermission_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _rolePermissionsMock
            .Setup(x => x.GetPermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["courses:update"]);

        var result = await _sut.HasTenantPermissionAsync(userId, tenantId, "courses:update");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_DirectDenyOverridesCustomRolePermission()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TenantPermission
                {
                    UserId = userId,
                    TenantId = tenantId,
                    DenyPermissions = ["courses:update"]
                }
            ]);
        _rolePermissionsMock
            .Setup(x => x.GetPermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["courses:read", "courses:update"]);

        var result = await _sut.GetEffectivePermissionsAsync(userId, tenantId);

        result.Should().Contain("courses:read").And.NotContain("courses:update");
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_IgnoresUniversalWildcardFromRoleProvider()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _rolePermissionsMock
            .Setup(x => x.GetPermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["admin:*", "courses:read"]);

        var result = await _sut.GetEffectivePermissionsAsync(userId, tenantId);

        result.Should().Equal("courses:read");
    }

    // ── GetTenantPermissionsAsync ─────────────────────────────

    [Fact]
    public async Task GetTenantPermissionsAsync_NoRecord_ReturnsEmptyList()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.GetTenantPermissionsAsync(Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenantPermissionsAsync_HasPermissions_ReturnsAll()
    {
        var permission = new TenantPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read", "write", "delete" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(permission.UserId, permission.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var result = await _sut.GetTenantPermissionsAsync(permission.UserId, permission.TenantId);
        result.Should().BeEquivalentTo(new[] { "read", "write", "delete" });
    }

    // ── GetEffectivePermissionsAsync ──────────────────────────

    [Fact]
    public async Task GetEffectivePermissionsAsync_NoTenantId_ReturnsEmpty_FailClosed()
    {
        var result = await _sut.GetEffectivePermissionsAsync(Guid.NewGuid(), null);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_CombinesAllLayers()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Global defaults
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission
            {
                Permissions = new[] { "global-perm" },
                DenyPermissions = Array.Empty<string>()
            });

        // Tenant defaults
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission
            {
                Permissions = new[] { "tenant-perm" },
                DenyPermissions = Array.Empty<string>()
            });

        // User direct grants
        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantPermission>
            {
                new()
                {
                    TenantId = tenantId,
                    Permissions = new[] { "user-perm" },
                    DenyPermissions = Array.Empty<string>()
                }
            });

        var result = await _sut.GetEffectivePermissionsAsync(userId, tenantId);

        result.Should().Contain("global-perm");
        result.Should().Contain("tenant-perm");
        result.Should().Contain("user-perm");
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_DenyWins()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Global grants "read" + "write"
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission
            {
                Permissions = new[] { "read", "write" },
                DenyPermissions = Array.Empty<string>()
            });

        // Tenant denies "write"
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission
            {
                Permissions = Array.Empty<string>(),
                DenyPermissions = new[] { "write" }
            });

        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantPermission>());

        var result = await _sut.GetEffectivePermissionsAsync(userId, tenantId);

        result.Should().Contain("read");
        result.Should().NotContain("write"); // denied
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_ExpiredPermissions_Excluded()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        _repoMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantPermission>
            {
                new()
                {
                    TenantId = tenantId,
                    Permissions = new[] { "expired-perm" },
                    DenyPermissions = Array.Empty<string>(),
                    ExpiresAt = DateTime.UtcNow.AddDays(-1) // expired
                }
            });

        var result = await _sut.GetEffectivePermissionsAsync(userId, tenantId);

        result.Should().NotContain("expired-perm");
    }

    // ── GetGlobalDefaultPermissionsAsync ──────────────────────

    [Fact]
    public async Task GetGlobalDefaultPermissionsAsync_NoDefaults_ReturnsEmpty()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.GetGlobalDefaultPermissionsAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGlobalDefaultPermissionsAsync_HasDefaults_ReturnsThem()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission { Permissions = new[] { "perm1", "perm2" } });

        var result = await _sut.GetGlobalDefaultPermissionsAsync();
        result.Should().BeEquivalentTo(new[] { "perm1", "perm2" });
    }

    // ── GetTenantDefaultPermissionsAsync ──────────────────────

    [Fact]
    public async Task GetTenantDefaultPermissionsAsync_NoDefaults_ReturnsEmpty()
    {
        var tenantId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.GetTenantDefaultPermissionsAsync(tenantId);
        result.Should().BeEmpty();
    }

    // ── IsUserInTenantAsync ──────────────────────────────────

    [Fact]
    public async Task IsUserInTenantAsync_DelegatesToMembershipChecker()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _membershipMock
            .Setup(x => x.IsUserMemberOfTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.IsUserInTenantAsync(userId, tenantId);

        result.Should().BeTrue();
        _membershipMock.Verify(x => x.IsUserMemberOfTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
