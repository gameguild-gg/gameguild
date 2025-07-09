using System.Collections.ObjectModel;
using GameGuild.Modules.Permissions.Models;

namespace GameGuild.API.Tests.Source.Tests.Modules.Permissions.Unit.Services;

/// <summary>
/// Unit tests for WithPermissions base class and PermissionType enum
/// </summary>
public class WithPermissionTests {
  private class TestPermissionEntity : WithPermissions {
    public TestPermissionEntity() {
      // Initialize with default empty permissions
    }
  }

  [Fact]
  public void WithPermissions_Constructor_InitializesWithNoPermissions() {
    // Arrange & Act
    var entity = new TestPermissionEntity();

    // Assert
    Assert.Equal(0UL, entity.PermissionFlags1);
    Assert.Equal(0UL, entity.PermissionFlags2);
    Assert.True(entity.IsGlobalDefaultPermission); // A new entity with null UserId and TenantId is a global default
    Assert.False(entity.IsUserPermission);
  }

  [Fact]
  public void HasPermission_WithGrantedPermission_ReturnsTrue() {
    // Arrange
    var entity = new TestPermissionEntity();
    entity.AddPermission(PermissionType.Read);

    // Act & Assert
    Assert.True(entity.HasPermission(PermissionType.Read));
  }

  [Fact]
  public void HasPermission_WithoutGrantedPermission_ReturnsFalse() {
    // Arrange
    var entity = new TestPermissionEntity();

    // Act & Assert
    Assert.False(entity.HasPermission(PermissionType.Read));
  }

  [Fact]
  public void AddPermission_SinglePermission_SetsCorrectFlag() {
    // Arrange
    var entity = new TestPermissionEntity();

    // Act
    entity.AddPermission(PermissionType.Read);

    // Assert
    Assert.True(entity.HasPermission(PermissionType.Read));
    Assert.NotEqual(0UL, entity.PermissionFlags1);
  }

  [Fact]
  public void AddPermission_HighBitPermission_SetsCorrectFlag() {
    // Arrange
    var entity = new TestPermissionEntity();

    // Act - Test permission in second 64-bit flag
    entity.AddPermission(PermissionType.Sms); // SMS = 70, should be in PermissionFlags2

    // Assert
    Assert.True(entity.HasPermission(PermissionType.Sms));
    Assert.Equal(0UL, entity.PermissionFlags1); // Should remain 0
    Assert.NotEqual(0UL, entity.PermissionFlags2); // Should be set
  }

  [Fact]
  public void RemovePermission_GrantedPermission_RemovesFlag() {
    // Arrange
    var entity = new TestPermissionEntity();
    entity.AddPermission(PermissionType.Read);
    Assert.True(entity.HasPermission(PermissionType.Read));

    // Act
    entity.RemovePermission(PermissionType.Read);

    // Assert
    Assert.False(entity.HasPermission(PermissionType.Read));
    Assert.Equal(0UL, entity.PermissionFlags1);
  }

  [Fact]
  public void HasAllPermissions_WithAllGranted_ReturnsTrue() {
    // Arrange
    var entity = new TestPermissionEntity();
    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    foreach (var permission in permissions) entity.AddPermission(permission);

    // Act & Assert
    Assert.True(entity.HasAllPermissions(permissions));
  }

  [Fact]
  public void HasAllPermissions_WithSomeMissing_ReturnsFalse() {
    // Arrange
    var entity = new TestPermissionEntity();
    entity.AddPermission(PermissionType.Read);
    entity.AddPermission(PermissionType.Comment);
    // Missing Vote permission

    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    // Act & Assert
    Assert.False(entity.HasAllPermissions(permissions));
  }

  [Fact]
  public void HasAnyPermission_WithOneGranted_ReturnsTrue() {
    // Arrange
    var entity = new TestPermissionEntity();
    entity.AddPermission(PermissionType.Read);

    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    // Act & Assert
    Assert.True(entity.HasAnyPermission(permissions));
  }

  [Fact]
  public void HasAnyPermission_WithNoneGranted_ReturnsFalse() {
    // Arrange
    var entity = new TestPermissionEntity();

    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    // Act & Assert
    Assert.False(entity.HasAnyPermission(permissions));
  }

  [Fact]
  public void AddPermissions_MultiplePermissions_AddsAllCorrectly() {
    // Arrange
    var entity = new TestPermissionEntity();
    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    // Act
    foreach (var permission in permissions) entity.AddPermission(permission);

    // Assert
    Assert.True(entity.HasAllPermissions(permissions));
  }

  [Fact]
  public void RemovePermissions_MultiplePermissions_RemovesAllCorrectly() {
    // Arrange
    var entity = new TestPermissionEntity();
    var permissions = new Collection<PermissionType> { PermissionType.Read, PermissionType.Comment, PermissionType.Vote };

    foreach (var permission in permissions) entity.AddPermission(permission);

    Assert.True(entity.HasAllPermissions(permissions));

    // Act
    entity.RemovePermissions(permissions);

    // Assert
    Assert.False(entity.HasAnyPermission(permissions));
  }

  [Fact]
  public void IsGlobalDefaultPermission_WithNoUserAndTenant_ReturnsTrue() {
    // Arrange
    var entity = new TestPermissionEntity { UserId = null, TenantId = null };

    // Act & Assert
    Assert.True(entity.IsGlobalDefaultPermission);
  }

  [Fact]
  public void IsUserPermission_WithUserId_ReturnsTrue() {
    // Arrange
    var entity = new TestPermissionEntity { UserId = Guid.NewGuid(), TenantId = Guid.NewGuid() };

    // Act & Assert
    Assert.True(entity.IsUserPermission);
  }

  [Theory]
  [InlineData(PermissionType.Read, 1)]
  [InlineData(PermissionType.Comment, 2)]
  [InlineData(PermissionType.Vote, 4)]
  [InlineData(PermissionType.Share, 5)]
  public void PermissionType_EnumValues_HaveCorrectValues(PermissionType permission, int expectedValue) {
    // Act & Assert
    Assert.Equal(expectedValue, (int)permission);
  }

  [Fact]
  public void Permission_CrossBoundary_HandledCorrectly() {
    // Arrange
    var entity = new TestPermissionEntity();

    // Act - Add permissions across both flag boundaries
    entity.AddPermission(PermissionType.Read); // Low bit (1)
    entity.AddPermission(PermissionType.Sms); // High bit (70)

    // Assert
    Assert.True(entity.HasPermission(PermissionType.Read));
    Assert.True(entity.HasPermission(PermissionType.Sms));
    Assert.NotEqual(0UL, entity.PermissionFlags1);
    Assert.NotEqual(0UL, entity.PermissionFlags2);
  }

  [Fact]
  public void Permission_InvalidBitPosition_HandledGracefully() {
    // Arrange
    var entity = new TestPermissionEntity();
    var invalidPermission = (PermissionType)200; // Beyond 128-bit range

    // Act & Assert - Should not throw
    entity.AddPermission(invalidPermission);
    Assert.False(entity.HasPermission(invalidPermission));
  }
}
