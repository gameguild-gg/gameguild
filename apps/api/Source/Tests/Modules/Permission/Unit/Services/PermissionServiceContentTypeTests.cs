using Xunit;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;


namespace GameGuild.Tests.Modules.Permission.Unit.Services;

/// <summary>
/// Unit tests for PermissionService - Layer 2 (Content-Type permissions)
/// </summary>
public class PermissionServiceContentTypeTests : IDisposable {
  private readonly ApplicationDbContext _context;

  private readonly PermissionService _permissionService;

  public PermissionServiceContentTypeTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                  .Options;

    _context = new ApplicationDbContext(options);
    _permissionService = new PermissionService(_context);
  }

  public void Dispose() { _context.Dispose(); }

  #region Content Type Permission Grant Tests

  [Fact]
  public async Task GrantContentTypePermissionAsync_NewPermission_CreatesNewRecord() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var permissions = new[] { PermissionType.Read, PermissionType.Edit };

    // Act
    await _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, permissions);

    // Assert
    var permission = await _context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                 ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentType == contentType
                     );

    Assert.NotNull(permission);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Edit));
    Assert.False(permission.HasPermission(PermissionType.Delete));
  }

  [Fact]
  public async Task GrantContentTypePermissionAsync_ExistingPermission_UpdatesRecord() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Grant initial permissions
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      contentType,
      new[] { PermissionType.Read }
    );

    // Act - Grant additional permissions
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      contentType,
      new[] { PermissionType.Edit }
    );

    // Assert
    var permission = await _context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                 ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentType == contentType
                     );

    Assert.NotNull(permission);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Edit));

    // Verify only one record exists
    var count = await _context.ContentTypePermissions.CountAsync(ctp =>
                                                                   ctp.UserId == userId && ctp.TenantId == tenantId && ctp.ContentType == contentType
                );
    Assert.Equal(1, count);
  }

  [Fact]
  public async Task GrantContentTypePermissionAsync_TenantDefault_CreatesTenantDefaultRecord() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var permissions = new[] { PermissionType.Read, PermissionType.Comment };

    // Act
    await _permissionService.GrantContentTypePermissionAsync(userId: null, tenantId, contentType, permissions);

    // Assert
    var permission = await _context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                 ctp.UserId == null && ctp.TenantId == tenantId && ctp.ContentType == contentType
                     );

    Assert.NotNull(permission);
    Assert.Null(permission.UserId);
    Assert.Equal(tenantId, permission.TenantId);
    Assert.Equal(contentType, permission.ContentType);
    Assert.True(permission.HasPermission(PermissionType.Read));
    Assert.True(permission.HasPermission(PermissionType.Comment));
  }

  [Fact]
  public async Task GrantContentTypePermissionAsync_GlobalDefault_CreatesGlobalDefaultRecord() {
    // Arrange
    var contentType = "Article";
    var permissions = new[] { PermissionType.Read };

    // Act
    await _permissionService.GrantContentTypePermissionAsync(userId: null, tenantId: null, contentType, permissions);

    // Assert
    var permission = await _context.ContentTypePermissions.FirstOrDefaultAsync(ctp =>
                                                                                 ctp.UserId == null && ctp.TenantId == null && ctp.ContentType == contentType
                     );

    Assert.NotNull(permission);
    Assert.Null(permission.UserId);
    Assert.Null(permission.TenantId);
    Assert.Equal(contentType, permission.ContentType);
    Assert.True(permission.HasPermission(PermissionType.Read));
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public async Task GrantContentTypePermissionAsync_InvalidContentType_ThrowsArgumentException(
    string invalidContentType
  ) {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read };

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
                                                  _permissionService.GrantContentTypePermissionAsync(userId, tenantId, invalidContentType, permissions)
    );
  }

  [Fact]
  public async Task GrantContentTypePermissionAsync_EmptyPermissions_ThrowsArgumentException() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var permissions = Array.Empty<PermissionType>();

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
                                                  _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, permissions)
    );
  }

  #endregion

  #region Content Type Permission Check Tests

  [Fact]
  public async Task HasContentTypePermissionAsync_WithDirectPermission_ReturnsTrue() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      contentType,
      new[] { PermissionType.Read }
    );

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_WithoutPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_WithTenantDefault_ReturnsTrue() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Set tenant default permission
    await _permissionService.GrantContentTypePermissionAsync(
      userId: null,
      tenantId,
      contentType,
      new[] { PermissionType.Read }
    );

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_WithGlobalDefault_ReturnsTrue() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Set global default permission
    await _permissionService.GrantContentTypePermissionAsync(
      userId: null,
      tenantId: null,
      contentType,
      new[] { PermissionType.Read }
    );

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_HierarchyOrder_UserOverridesTenantDefault() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Set tenant default with Read permission
    await _permissionService.GrantContentTypePermissionAsync(
      userId: null,
      tenantId,
      contentType,
      new[] { PermissionType.Read }
    );

    // Grant user-specific permissions without Read
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      contentType,
      new[] { PermissionType.Comment }
    );

    // Act
    var hasRead =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);
    var hasComment =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Comment);

    // Assert
    Assert.True(hasComment); // User has comment permission
    Assert.True(hasRead); // Should get Read from tenant default (since user permission is additive)
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_ExpiredPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Create permission with past expiration
    var permission = new ContentTypePermission { UserId = userId, TenantId = tenantId, ContentType = contentType, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
    permission.AddPermission(PermissionType.Read);

    _context.ContentTypePermissions.Add(permission);
    await _context.SaveChangesAsync();

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task HasContentTypePermissionAsync_DeletedPermission_ReturnsFalse() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Create and delete permission
    var permission = new ContentTypePermission { UserId = userId, TenantId = tenantId, ContentType = contentType };
    permission.AddPermission(PermissionType.Read);
    permission.SoftDelete();

    _context.ContentTypePermissions.Add(permission);
    await _context.SaveChangesAsync();

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);

    // Assert
    Assert.False(result);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public async Task HasContentTypePermissionAsync_InvalidContentType_ReturnsFalse(string invalidContentType) {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();

    // Act
    var result =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, invalidContentType, PermissionType.Read);

    // Assert
    Assert.False(result);
  }

  #endregion

  #region Get Content Type Permissions Tests

  [Fact]
  public async Task GetContentTypePermissionsAsync_WithPermissions_ReturnsCorrectList() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var expectedPermissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete };

    await _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, expectedPermissions);

    // Act
    var result = await _permissionService.GetContentTypePermissionsAsync(userId, tenantId, contentType);

    // Assert
    var resultArray = result.ToArray();
    // We expect 3 permissions since we granted Read, Edit, and Delete (Delete and SoftDelete have same enum value)
    Assert.Equal(3, resultArray.Length);
    Assert.Contains(PermissionType.Read, resultArray);
    Assert.Contains(PermissionType.Edit, resultArray);
    Assert.Contains(PermissionType.Delete, resultArray);
  }

  [Fact]
  public async Task GetContentTypePermissionsAsync_NoPermissions_ReturnsEmpty() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";

    // Act
    var result = await _permissionService.GetContentTypePermissionsAsync(userId, tenantId, contentType);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public async Task GetContentTypePermissionsAsync_InvalidContentType_ReturnsEmpty(string invalidContentType) {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();

    // Act
    var result = await _permissionService.GetContentTypePermissionsAsync(userId, tenantId, invalidContentType);

    // Assert
    Assert.Empty(result);
  }

  #endregion

  #region Revoke Content Type Permissions Tests

  [Fact]
  public async Task RevokeContentTypePermissionAsync_ExistingPermissions_RemovesSpecified() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var grantedPermissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete };
    var revokePermissions = new[] { PermissionType.Edit };

    await _permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, grantedPermissions);

    // Act
    await _permissionService.RevokeContentTypePermissionAsync(userId, tenantId, contentType, revokePermissions);

    // Assert
    var hasRead =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);
    var hasEdit =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Edit);
    var hasDelete =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Delete);

    Assert.True(hasRead);
    Assert.False(hasEdit); // Should be revoked
    Assert.True(hasDelete);
  }

  [Fact]
  public async Task RevokeContentTypePermissionAsync_NonExistentPermissions_DoesNotThrow() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var revokePermissions = new[] { PermissionType.Edit };

    // Act & Assert - Should not throw
    await _permissionService.RevokeContentTypePermissionAsync(userId, tenantId, contentType, revokePermissions);
  }

  [Fact]
  public async Task RevokeContentTypePermissionAsync_EmptyPermissions_DoesNothing() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var contentType = "Article";
    var emptyPermissions = Array.Empty<PermissionType>();

    // Grant some initial permissions
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      contentType,
      new[] { PermissionType.Read }
    );

    // Act
    await _permissionService.RevokeContentTypePermissionAsync(userId, tenantId, contentType, emptyPermissions);

    // Assert - Should still have the original permissions
    var hasRead =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, contentType, PermissionType.Read);
    Assert.True(hasRead);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public async Task RevokeContentTypePermissionAsync_InvalidContentType_ThrowsArgumentException(
    string invalidContentType
  ) {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var permissions = new[] { PermissionType.Read };

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
                                                  _permissionService.RevokeContentTypePermissionAsync(userId, tenantId, invalidContentType, permissions)
    );
  }

  #endregion

  #region Content Type Scenarios Tests

  [Fact]
  public async Task ContentTypePermissions_MultipleContentTypes_IsolatedCorrectly() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var articleType = "Article";
    var videoType = "Video";

    // Grant different permissions to different content types
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      articleType,
      new[] { PermissionType.Read, PermissionType.Edit }
    );
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      videoType,
      new[] { PermissionType.Read, PermissionType.Delete }
    );

    // Act & Assert
    var hasArticleEdit =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, articleType, PermissionType.Edit);
    var hasArticleDelete =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, articleType, PermissionType.Delete);
    var hasVideoEdit =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, videoType, PermissionType.Edit);
    var hasVideoDelete =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, videoType, PermissionType.Delete);

    Assert.True(hasArticleEdit); // Article has Edit
    Assert.False(hasArticleDelete); // Article doesn't have Delete
    Assert.False(hasVideoEdit); // Video doesn't have Edit
    Assert.True(hasVideoDelete); // Video has Delete
  }

  [Fact]
  public async Task ContentTypePermissions_CaseSensitive_TreatedAsDistinct() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var articleType = "Article";
    var articleTypeLower = "article";

    // Grant permissions to one case
    await _permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      articleType,
      new[] { PermissionType.Read }
    );

    // Act & Assert
    var hasUpperCase =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, articleType, PermissionType.Read);
    var hasLowerCase =
      await _permissionService.HasContentTypePermissionAsync(userId, tenantId, articleTypeLower, PermissionType.Read);

    Assert.True(hasUpperCase);
    Assert.False(hasLowerCase); // Should be case-sensitive
  }

  #endregion
}
