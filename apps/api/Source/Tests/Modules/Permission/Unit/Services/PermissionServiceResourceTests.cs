using Xunit;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Comment.Models;


namespace GameGuild.Tests.Modules.Permission.Unit.Services;

/// <summary>
/// Unit tests for PermissionService - Layer 3 (Resource-specific permissions)
/// Using CommentPermission as concrete implementation
/// </summary>
public class PermissionServiceResourceTests : IDisposable {
  private readonly ApplicationDbContext _context;

  private readonly PermissionService _permissionService;

  public PermissionServiceResourceTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                  .Options;

    _context = new ApplicationDbContext(options);
    _permissionService = new PermissionService(_context);
  }

  public void Dispose() { _context.Dispose(); }

  #region Resource Permission Grant Tests

  [Fact]
  public async Task GrantResourcePermissionAsync_NewPermission_CreatesNewRecord() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read, PermissionType.Edit };

    // Act
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      permissions
    );

    // Assert
    CommentPermission? permission = await _context.Set<CommentPermission>()
                                                  .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.TenantId == tenantId && cp.ResourceId == resourceId);

    Assert.NotNull(permission);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Edit));
    Assert.False(permission.HasPermission(PermissionType.Delete));
  }

  [Fact]
  public async Task GrantResourcePermissionAsync_ExistingPermission_UpdatesRecord() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Grant initial permissions
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      [PermissionType.Read]
    );

    // Act - Grant additional permissions
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      [PermissionType.Edit]
    );

    // Assert
    CommentPermission? permission = await _context.Set<CommentPermission>()
                                                  .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.TenantId == tenantId && cp.ResourceId == resourceId);

    Assert.NotNull(permission);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Edit));

    // Verify only one record exists
    int count = await _context.Set<CommentPermission>()
                              .CountAsync(cp => cp.UserId == userId && cp.TenantId == tenantId && cp.ResourceId == resourceId);
    Assert.Equal(1, count);
  }

  [Fact]
  public async Task GrantResourcePermissionAsync_TenantDefault_CreatesTenantDefaultRecord() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read, PermissionType.Comment };

    // Act
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId: null,
      tenantId,
      resourceId,
      permissions
    );

    // Assert
    CommentPermission? permission = await _context.Set<CommentPermission>()
                                                  .FirstOrDefaultAsync(cp => cp.UserId == null && cp.TenantId == tenantId && cp.ResourceId == resourceId);

    Assert.NotNull(permission);
    Assert.Null(permission.UserId);
    Assert.Equal(tenantId, permission.TenantId);
    Assert.Equal(resourceId, permission.ResourceId);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Comment));
    Assert.True(permission.IsDefaultResourcePermission);
  }

  [Fact]
  public async Task GrantResourcePermissionAsync_GlobalDefault_CreatesGlobalDefaultRecord() {
    // Arrange
    var resourceId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read };

    // Act
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId: null,
      tenantId: null,
      resourceId,
      permissions
    );

    // Assert
    CommentPermission? permission = await _context.Set<CommentPermission>()
                                                  .FirstOrDefaultAsync(cp => cp.UserId == null && cp.TenantId == null && cp.ResourceId == resourceId);

    Assert.NotNull(permission);
    Assert.Null(permission.UserId);
    Assert.Null(permission.TenantId);
    Assert.Equal(resourceId, permission.ResourceId);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.IsGlobalResourceDefault);
  }

  #endregion

  #region Resource Permission Check Tests

  [Fact]
  public async Task HasResourcePermissionAsync_WithDirectPermission_ReturnsTrue() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      [PermissionType.Read]
    );

    // Act
    bool result =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task HasResourcePermissionAsync_WithoutPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Act
    bool result =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task HasResourcePermissionAsync_ExpiredPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Create permission with past expiration
    var permission = new CommentPermission { UserId = userId, TenantId = tenantId, ResourceId = resourceId, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
    permission.AddPermission(PermissionType.Read);

    _context.Set<CommentPermission>().Add(permission);
    await _context.SaveChangesAsync();

    // Act
    bool result =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task HasResourcePermissionAsync_DeletedPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Create and delete permission
    var permission = new CommentPermission { UserId = userId, TenantId = tenantId, ResourceId = resourceId };
    permission.AddPermission(PermissionType.Read);
    permission.SoftDelete();

    _context.Set<CommentPermission>().Add(permission);
    await _context.SaveChangesAsync();

    // Act
    bool result =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );

    // Assert
    Assert.False(result);
  }

  #endregion

  #region Get Resource Permissions Tests

  [Fact]
  public async Task GetResourcePermissionsAsync_WithPermissions_ReturnsCorrectList() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var expectedPermissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete };

    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      expectedPermissions
    );

    // Act
    var result =
      await _permissionService.GetResourcePermissionsAsync<CommentPermission, Comment>(userId, tenantId, resourceId);

    // Assert
    var resultArray = result.ToArray();
    // We expect 3 permissions since we granted Read, Edit, and Delete/SoftDelete (Delete and SoftDelete have same enum value)
    Assert.Equal(3, resultArray.Length);
    Assert.Contains(PermissionType.Read, resultArray);
    Assert.Contains(PermissionType.Edit, resultArray);
    Assert.Contains(PermissionType.Delete, resultArray);
    // No need to assert SoftDelete separately since it's the same enum value as Delete (25)
  }

  [Fact]
  public async Task GetResourcePermissionsAsync_NoPermissions_ReturnsEmpty() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Act
    var result =
      await _permissionService.GetResourcePermissionsAsync<CommentPermission, Comment>(userId, tenantId, resourceId);

    // Assert
    Assert.Empty(result);
  }

  #endregion

  #region Revoke Resource Permissions Tests

  [Fact]
  public async Task RevokeResourcePermissionAsync_ExistingPermissions_RemovesSpecified() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var grantedPermissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete };
    var revokePermissions = new[] { PermissionType.Edit };

    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      grantedPermissions
    );

    // Act
    await _permissionService.RevokeResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      revokePermissions
    );

    // Assert
    bool hasRead =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );
    bool hasEdit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Edit
      );
    bool hasDelete =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Delete
      );

    Assert.True(hasRead);
    Assert.False(hasEdit); // Should be revoked
    Assert.True(hasDelete);
  }

  [Fact]
  public async Task RevokeResourcePermissionAsync_NonExistentPermissions_DoesNotThrow() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var revokePermissions = new[] { PermissionType.Edit };

    // Act & Assert - Should not throw
    await _permissionService.RevokeResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resourceId,
      revokePermissions
    );
  }

  #endregion

  #region Bulk Operations Tests

  [Fact]
  public async Task GetBulkResourcePermissionsAsync_MultipleResources_ReturnsCorrectMappings() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resource1Id = Guid.NewGuid();
    var resource2Id = Guid.NewGuid();
    var resource3Id = Guid.NewGuid();

    // Grant different permissions to different resources
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resource1Id,
      [PermissionType.Read, PermissionType.Edit]
    );
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resource2Id,
      [PermissionType.Read, PermissionType.Delete]
    );
    // resource3Id has no permissions

    var resourceIds = new[] { resource1Id, resource2Id, resource3Id };

    // Act
    var result =
      await _permissionService.GetBulkResourcePermissionsAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceIds
      );

    // Assert
    Assert.Equal(2, result.Count); // Only resources with permissions should be returned

    // Resource 1 permissions
    Assert.True(result.ContainsKey(resource1Id));
    var resource1Permissions = result[resource1Id].ToArray();
    Assert.Contains(PermissionType.Read, resource1Permissions);
    Assert.Contains(PermissionType.Edit, resource1Permissions);

    // Resource 2 permissions
    Assert.True(result.ContainsKey(resource2Id));
    var resource2Permissions = result[resource2Id].ToArray();
    Assert.Contains(PermissionType.Read, resource2Permissions);
    Assert.Contains(PermissionType.Delete, resource2Permissions);

    // Resource 3 should not be in results
    Assert.False(result.ContainsKey(resource3Id));
  }

  [Fact]
  public async Task GetBulkResourcePermissionsAsync_EmptyResourceIds_ReturnsEmpty() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceIds = Array.Empty<Guid>();

    // Act
    var result =
      await _permissionService.GetBulkResourcePermissionsAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceIds
      );

    // Assert
    Assert.Empty(result);
  }

  #endregion

  #region Resource Sharing Tests

  [Fact]
  public async Task ShareResourceAsync_WithPermissions_GrantsToTargetUser() {
    // Arrange
    var sourceUserId = Guid.NewGuid();
    var targetUserId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read, PermissionType.Comment };

    // Act
    await _permissionService.ShareResourceAsync<CommentPermission, Comment>(
      resourceId,
      targetUserId,
      tenantId,
      permissions
    );

    // Assert
    bool hasRead =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        targetUserId,
        tenantId,
        resourceId,
        PermissionType.Read
      );
    bool hasComment =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        targetUserId,
        tenantId,
        resourceId,
        PermissionType.Comment
      );
    bool hasEdit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        targetUserId,
        tenantId,
        resourceId,
        PermissionType.Edit
      );

    Assert.True(hasRead);
    Assert.True(hasComment);
    Assert.False(hasEdit); // Not shared
  }

  [Fact]
  public async Task ShareResourceAsync_WithExpiration_SetsExpirationDate() {
    // Arrange
    var targetUserId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read };
    DateTime expiresAt = DateTime.UtcNow.AddDays(7);

    // Act
    await _permissionService.ShareResourceAsync<CommentPermission, Comment>(
      resourceId,
      targetUserId,
      tenantId,
      permissions,
      expiresAt
    );

    // Assert
    CommentPermission? permission = await _context.Set<CommentPermission>()
                                                  .FirstOrDefaultAsync(cp =>
                                                                         cp.UserId == targetUserId && cp.TenantId == tenantId && cp.ResourceId == resourceId
                                                  );

    Assert.NotNull(permission);
    Assert.NotNull(permission.ExpiresAt);
    Assert.True(permission.ExpiresAt <= expiresAt.AddSeconds(1)); // Allow small time difference
    Assert.True(permission.ExpiresAt >= expiresAt.AddSeconds(-1));
  }

  #endregion

  #region Resource Permission Isolation Tests

  [Fact]
  public async Task ResourcePermissions_MultipleResources_IsolatedCorrectly() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resource1Id = Guid.NewGuid();
    var resource2Id = Guid.NewGuid();

    // Grant different permissions to different resources
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resource1Id,
      [PermissionType.Read, PermissionType.Edit]
    );
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      userId,
      tenantId,
      resource2Id,
      [PermissionType.Read, PermissionType.Delete]
    );

    // Act & Assert
    bool hasResource1Edit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resource1Id,
        PermissionType.Edit
      );
    bool hasResource1Delete =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resource1Id,
        PermissionType.Delete
      );
    bool hasResource2Edit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resource2Id,
        PermissionType.Edit
      );
    bool hasResource2Delete =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resource2Id,
        PermissionType.Delete
      );

    Assert.True(hasResource1Edit); // Resource1 has Edit
    Assert.False(hasResource1Delete); // Resource1 doesn't have Delete
    Assert.False(hasResource2Edit); // Resource2 doesn't have Edit
    Assert.True(hasResource2Delete); // Resource2 has Delete
  }

  [Fact]
  public async Task ResourcePermissions_DifferentUsers_IsolatedCorrectly() {
    // Arrange
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Grant different permissions to different users for same resource
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      user1Id,
      tenantId,
      resourceId,
      [PermissionType.Read, PermissionType.Edit]
    );
    await _permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
      user2Id,
      tenantId,
      resourceId,
      [PermissionType.Read]
    );

    // Act & Assert
    bool user1HasEdit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        user1Id,
        tenantId,
        resourceId,
        PermissionType.Edit
      );
    bool user2HasEdit =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        user2Id,
        tenantId,
        resourceId,
        PermissionType.Edit
      );
    bool user1HasRead =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        user1Id,
        tenantId,
        resourceId,
        PermissionType.Read
      );
    bool user2HasRead =
      await _permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        user2Id,
        tenantId,
        resourceId,
        PermissionType.Read
      );

    Assert.True(user1HasEdit); // User1 has Edit
    Assert.False(user2HasEdit); // User2 doesn't have Edit
    Assert.True(user1HasRead); // Both users have Read
    Assert.True(user2HasRead);
  }

  #endregion
}
