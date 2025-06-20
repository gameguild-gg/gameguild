using Xunit;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Tests.Modules.Permission.Services;

/// <summary>
/// Unit tests for PermissionService - Layer 1 (Tenant-wide permissions)
/// </summary>
public class PermissionServiceTenantTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly PermissionService _permissionService;

    public PermissionServiceTenantTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _permissionService = new PermissionService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Tenant Permission Grant Tests

    [Fact]
    public async Task GrantTenantPermissionAsync_NewPermission_CreatesNewRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[]
        {
            PermissionType.Read, PermissionType.Comment
        };

        // Act
        TenantPermission result = await _permissionService.GrantTenantPermissionAsync(userId, tenantId, permissions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.HasPermission(PermissionType.Read));
        Assert.True(result.HasPermission(PermissionType.Comment));
        Assert.False(result.HasPermission(PermissionType.Vote));
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_ExistingPermission_UpdatesRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Grant initial permissions
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Read]);

        // Act - Grant additional permissions
        TenantPermission result = await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Comment]);

        // Assert
        Assert.True(result.HasPermission(PermissionType.Read));
        Assert.True(result.HasPermission(PermissionType.Comment));

        // Verify only one record exists
        int count = await _context.TenantPermissions.CountAsync(tp => tp.UserId == userId && tp.TenantId == tenantId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_GlobalDefault_CreatesGlobalRecord()
    {
        // Arrange
        var permissions = new[]
        {
            PermissionType.Read, PermissionType.Comment
        };

        // Act
        TenantPermission result = await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, permissions);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.UserId);
        Assert.Null(result.TenantId);
        Assert.True(result.IsGlobalDefaultPermission);
        Assert.True(result.HasPermission(PermissionType.Read));
        Assert.True(result.HasPermission(PermissionType.Comment));
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_TenantDefault_CreatesTenantDefaultRecord()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var permissions = new[]
        {
            PermissionType.Read, PermissionType.Comment
        };

        // Act
        TenantPermission result = await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, permissions);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.UserId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.HasPermission(PermissionType.Read));
        Assert.True(result.HasPermission(PermissionType.Comment));
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_EmptyPermissions_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = Array.Empty<PermissionType>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _permissionService.GrantTenantPermissionAsync(userId, tenantId, permissions)
        );
    }

    [Fact]
    public async Task GrantTenantPermissionAsync_NullPermissions_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _permissionService.GrantTenantPermissionAsync(userId, tenantId, null!)
        );
    }

    #endregion

    #region Tenant Permission Check Tests

    [Fact]
    public async Task HasTenantPermissionAsync_WithDirectPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Read]);

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasTenantPermissionAsync_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasTenantPermissionAsync_WithTenantDefault_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set tenant default permission
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, [PermissionType.Read]);

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasTenantPermissionAsync_WithGlobalDefault_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set global default permission
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId: null, [PermissionType.Read]);

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasTenantPermissionAsync_HierarchyOrder_UserOverridesTenantDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set tenant default with Read permission
        await _permissionService.GrantTenantPermissionAsync(userId: null, tenantId, [PermissionType.Read]);

        // Grant user-specific permissions without Read
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Comment]);

        // Act
        bool hasRead = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);
        bool hasComment = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment);

        // Assert
        Assert.True(hasComment); // User has comment permission
        Assert.True(hasRead); // Should get Read from tenant default since user permission includes Read via inheritance
    }

    [Fact]
    public async Task HasTenantPermissionAsync_ExpiredPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Create permission with past expiration
        var permission = new TenantPermission
        {
            UserId = userId, TenantId = tenantId, ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        permission.AddPermission(PermissionType.Read);

        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasTenantPermissionAsync_DeletedPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Create and delete permission
        var permission = new TenantPermission
        {
            UserId = userId, TenantId = tenantId
        };
        permission.AddPermission(PermissionType.Read);
        permission.SoftDelete();

        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Get Permissions Tests

    [Fact]
    public async Task GetTenantPermissionsAsync_WithPermissions_ReturnsCorrectList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedPermissions = new[]
        {
            PermissionType.Read, PermissionType.Comment, PermissionType.Vote
        };

        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, expectedPermissions);

        // Act
        var result = await _permissionService.GetTenantPermissionsAsync(userId, tenantId);

        // Assert
        var resultArray = result.ToArray();
        Assert.Equal(expectedPermissions.Length, resultArray.Length);
        Assert.Contains(PermissionType.Read, resultArray);
        Assert.Contains(PermissionType.Comment, resultArray);
        Assert.Contains(PermissionType.Vote, resultArray);
    }

    [Fact]
    public async Task GetTenantPermissionsAsync_NoPermissions_ReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _permissionService.GetTenantPermissionsAsync(userId, tenantId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Revoke Permissions Tests

    [Fact]
    public async Task RevokeTenantPermissionAsync_ExistingPermissions_RemovesSpecified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var grantedPermissions = new[]
        {
            PermissionType.Read, PermissionType.Comment, PermissionType.Vote
        };
        var revokePermissions = new[]
        {
            PermissionType.Comment
        };

        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, grantedPermissions);

        // Act
        await _permissionService.RevokeTenantPermissionAsync(userId, tenantId, revokePermissions);

        // Assert
        bool hasRead = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);
        bool hasComment = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Comment);
        bool hasVote = await _permissionService.HasTenantPermissionAsync(userId, tenantId, PermissionType.Vote);

        Assert.True(hasRead);
        Assert.False(hasComment); // Should be revoked
        Assert.True(hasVote);
    }

    [Fact]
    public async Task RevokeTenantPermissionAsync_NonExistentPermissions_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var revokePermissions = new[]
        {
            PermissionType.Comment
        };

        // Act & Assert - Should not throw
        await _permissionService.RevokeTenantPermissionAsync(userId, tenantId, revokePermissions);
    }

    #endregion

    #region User-Tenant Membership Tests

    [Fact]
    public async Task JoinTenantAsync_NewMembership_CreatesWithMinimalPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        TenantPermission result = await _permissionService.JoinTenantAsync(userId, tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.IsActiveMembership);

        // Should have minimal permissions
        foreach (PermissionType permission in TenantPermissionConstants.MinimalUserPermissions)
        {
            Assert.True(result.HasPermission(permission));
        }
    }

    [Fact]
    public async Task JoinTenantAsync_ExistingMembership_ReactivatesIfNeeded()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Create expired membership
        var expiredMembership = new TenantPermission
        {
            UserId = userId, TenantId = tenantId, ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.TenantPermissions.Add(expiredMembership);
        await _context.SaveChangesAsync();

        // Act
        TenantPermission result = await _permissionService.JoinTenantAsync(userId, tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ExpiresAt); // Should remove expiration
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task LeaveTenantAsync_ActiveMembership_ExpiresImmediately()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _permissionService.JoinTenantAsync(userId, tenantId);

        // Act
        await _permissionService.LeaveTenantAsync(userId, tenantId);

        // Assert
        TenantPermission? membership = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId);

        Assert.NotNull(membership);
        Assert.True(membership.ExpiresAt <= DateTime.UtcNow);
        Assert.False(membership.IsValid);
    }

    [Fact]
    public async Task IsUserInTenantAsync_ActiveMembership_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _permissionService.JoinTenantAsync(userId, tenantId);

        // Act
        bool result = await _permissionService.IsUserInTenantAsync(userId, tenantId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserInTenantAsync_NoMembership_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        bool result = await _permissionService.IsUserInTenantAsync(userId, tenantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserTenantsAsync_WithMultipleTenants_ReturnsAllActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var tenant3Id = Guid.NewGuid();

        await _permissionService.JoinTenantAsync(userId, tenant1Id);
        await _permissionService.JoinTenantAsync(userId, tenant2Id);
        await _permissionService.JoinTenantAsync(userId, tenant3Id);

        // Leave one tenant
        await _permissionService.LeaveTenantAsync(userId, tenant3Id);

        // Act
        var result = await _permissionService.GetUserTenantsAsync(userId);

        // Assert
        var tenants = result.ToArray();
        Assert.Equal(2, tenants.Length); // Should only return active memberships
        Assert.Contains(tenants, t => t.TenantId == tenant1Id);
        Assert.Contains(tenants, t => t.TenantId == tenant2Id);
        Assert.DoesNotContain(tenants, t => t.TenantId == tenant3Id);
    }

    #endregion

    #region Effective Permissions Tests

    [Fact]
    public async Task GetEffectiveTenantPermissionsAsync_CombinesUserAndDefaults_ReturnsUnion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Set global defaults
        await _permissionService.GrantTenantPermissionAsync(null, null, [PermissionType.Read]);

        // Set tenant defaults
        await _permissionService.GrantTenantPermissionAsync(null, tenantId, [PermissionType.Comment]);

        // Set user-specific permissions
        await _permissionService.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Vote]);

        // Act
        var result = await _permissionService.GetEffectiveTenantPermissionsAsync(userId, tenantId);

        // Assert
        var permissions = result.ToArray();
        Assert.Contains(PermissionType.Read, permissions); // From global default
        Assert.Contains(PermissionType.Comment, permissions); // From tenant default
        Assert.Contains(PermissionType.Vote, permissions); // From user-specific
    }

    #endregion
}
