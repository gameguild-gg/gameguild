using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class PermissionGrantServiceTests
{
    private readonly Mock<ITenantPermissionRepository> _repoMock = new();
    private readonly Mock<IPermissionAuditService> _auditMock = new();
    private readonly Mock<ITenantSecurityVersionStore> _versionStoreMock = new();
    private readonly Mock<IActorContextAccessor> _actorAccessorMock = new();
    private readonly PermissionGrantService _sut;

    public PermissionGrantServiceTests()
    {
        // Default: unauthenticated actor (bypasses global default auth check)
        var actor = ActorContextBuilder.Create().Build();
        _actorAccessorMock.Setup(x => x.ActorContext).Returns(actor);

        _versionStoreMock
            .Setup(x => x.IncrementVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _auditMock
            .Setup(x => x.LogPermissionChangeAsync(
                It.IsAny<PermissionOperationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionAuditLog());

        _sut = new PermissionGrantService(
            _repoMock.Object,
            _auditMock.Object,
            _versionStoreMock.Object,
            _actorAccessorMock.Object,
            NullLogger<PermissionGrantService>.Instance
        );
    }

    // ── GrantTenantPermissionAsync ────────────────────────────

    [Fact]
    public async Task GrantTenantPermissionAsync_CreatesPermissionAndReturns()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permissions = new[] { "read", "write" };

        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        var result = await _sut.GrantTenantPermissionAsync(userId, tenantId, permissions);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        result.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_InvalidatesTenantCache()
    {
        var tenantId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.GrantTenantPermissionAsync(Guid.NewGuid(), tenantId, new[] { "read" });

        _versionStoreMock.Verify(
            x => x.IncrementVersionAsync(tenantId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_LogsAuditEvent()
    {
        var tenantId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.GrantTenantPermissionAsync(Guid.NewGuid(), tenantId, new[] { "read" });

        _auditMock.Verify(
            x => x.LogPermissionChangeAsync(
                PermissionOperationType.Grant,
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                true,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── RevokeTenantPermissionAsync ───────────────────────────

    [Fact]
    public async Task RevokeTenantPermissionAsync_NoExistingPermission_ReturnsFalse()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.RevokeTenantPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), new[] { "read" });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTenantPermissionAsync_AllPermissionsRemoved_DeletesRecord()
    {
        var existing = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(existing.UserId, existing.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.RevokeTenantPermissionAsync(existing.UserId, existing.TenantId, new[] { "read" });

        result.Should().BeTrue();
        _repoMock.Verify(x => x.DeleteAsync(existing.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeTenantPermissionAsync_SomePermissionsRemain_UpdatesRecord()
    {
        var existing = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { "read", "write", "delete" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(existing.UserId, existing.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        var result = await _sut.RevokeTenantPermissionAsync(existing.UserId, existing.TenantId, new[] { "read" });

        result.Should().BeTrue();
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── SetGlobalDefaultPermissionsAsync ──────────────────────

    [Fact]
    public async Task SetGlobalDefaultPermissionsAsync_ExistingDefaults_Updates()
    {
        var existing = new TenantPermission
        {
            UserId = null,
            TenantId = null,
            Permissions = new[] { "old-perm" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.SetGlobalDefaultPermissionsAsync(new[] { "new-perm" });

        existing.Permissions.Should().BeEquivalentTo(new[] { "new-perm" });
        _repoMock.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetGlobalDefaultPermissionsAsync_NoExistingDefaults_Creates()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.SetGlobalDefaultPermissionsAsync(new[] { "perm1" });

        _repoMock.Verify(x => x.CreateAsync(
            It.Is<TenantPermission>(p => p.UserId == null && p.TenantId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetGlobalDefaultPermissionsAsync_SystemAdministrator_IsAuthorized()
    {
        _actorAccessorMock.Setup(instance => instance.ActorContext)
            .Returns(ActorContextBuilder.ForSystem("authorization-tests").Build());
        _repoMock.Setup(instance => instance.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock.Setup(instance => instance.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission permission, CancellationToken _) => permission);

        var action = () => _sut.SetGlobalDefaultPermissionsAsync(["read"]);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetGlobalDefaultPermissionsAsync_ExplicitPermission_IsAuthorized()
    {
        _actorAccessorMock.Setup(instance => instance.ActorContext)
            .Returns(ActorContextBuilder.ForUser(Guid.NewGuid())
                .WithPermission(SystemPermission.Keys.ManageGlobalDefaults)
                .Build());
        _repoMock.Setup(instance => instance.GetByUserAndTenantAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock.Setup(instance => instance.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission permission, CancellationToken _) => permission);

        var action = () => _sut.SetGlobalDefaultPermissionsAsync(["read"]);

        await action.Should().NotThrowAsync();
    }

    // ── SetTenantDefaultPermissionsAsync ──────────────────────

    [Fact]
    public async Task SetTenantDefaultPermissionsAsync_ExistingDefaults_Updates()
    {
        var tenantId = Guid.NewGuid();
        var existing = new TenantPermission
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = new[] { "old" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.SetTenantDefaultPermissionsAsync(tenantId, new[] { "new" });

        existing.Permissions.Should().BeEquivalentTo(new[] { "new" });
    }

    [Fact]
    public async Task SetTenantDefaultPermissionsAsync_NoExisting_Creates()
    {
        var tenantId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        await _sut.SetTenantDefaultPermissionsAsync(tenantId, new[] { "perm" });

        _repoMock.Verify(x => x.CreateAsync(
            It.Is<TenantPermission>(p => p.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DenyTenantPermissionAsync ─────────────────────────────

    [Fact]
    public async Task DenyTenantPermissionAsync_ExistingRecord_AddsDenyPermissions()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existing = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { "read" },
            DenyPermissions = Array.Empty<string>()
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        var result = await _sut.DenyTenantPermissionAsync(userId, tenantId, new[] { "write" });

        result.DenyPermissions.Should().Contain("write");
    }

    [Fact]
    public async Task DenyTenantPermissionAsync_NoExistingRecord_CreatesNew()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);
        _repoMock
            .Setup(x => x.CreateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        var result = await _sut.DenyTenantPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), new[] { "delete" });

        result.DenyPermissions.Should().Contain("delete");
        result.Permissions.Should().BeEmpty();
    }

    // ── RemoveDenyPermissionsAsync ────────────────────────────

    [Fact]
    public async Task RemoveDenyPermissionsAsync_NoExistingRecord_ReturnsFalse()
    {
        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission?)null);

        var result = await _sut.RemoveDenyPermissionsAsync(Guid.NewGuid(), Guid.NewGuid(), new[] { "perm" });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveDenyPermissionsAsync_ExistingRecord_RemovesDenies()
    {
        var existing = new TenantPermission
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            DenyPermissions = new[] { "write", "delete" }
        };

        _repoMock
            .Setup(x => x.GetByUserAndTenantAsync(existing.UserId, existing.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);

        var result = await _sut.RemoveDenyPermissionsAsync(existing.UserId, existing.TenantId, new[] { "write" });

        result.Should().BeTrue();
        existing.DenyPermissions.Should().NotContain("write");
        existing.DenyPermissions.Should().Contain("delete");
    }
}
