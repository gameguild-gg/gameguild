using GameGuild.Common.Domain.Entities;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Projects.Models;

namespace GameGuild.API.Tests.Modules.Projects.Unit.Services;

/// <summary>
/// Unit tests for ProjectPermission model
/// Tests permission checking logic and computed properties
/// </summary>
public class ProjectPermissionTests {
  [Fact]
  public void ProjectPermission_CanEdit_ShouldReturnTrueWhenHasEditPermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Edit);

    // Act & Assert
    Assert.True(permission.CanEdit);
  }

  [Fact]
  public void ProjectPermission_CanEdit_ShouldReturnFalseWhenInvalid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Edit);
    permission.SoftDelete(); // This makes IsValid false

    // Act & Assert
    Assert.False(permission.CanEdit);
  }

  [Fact]
  public void ProjectPermission_CanDelete_ShouldReturnTrueWhenHasDeletePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Delete);

    // Act & Assert
    Assert.True(permission.CanDelete);
  }

  [Fact]
  public void ProjectPermission_CanPublish_ShouldReturnTrueWhenHasPublishPermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Publish);

    // Act & Assert
    Assert.True(permission.CanPublish);
  }

  [Fact]
  public void ProjectPermission_CanManageCollaborators_ShouldReturnTrueWhenHasSharePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Share);

    // Act & Assert
    Assert.True(permission.CanManageCollaborators);
  }

  [Fact]
  public void ProjectPermission_CanCreateReleases_ShouldReturnTrueWhenHasCreatePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Create);

    // Act & Assert
    Assert.True(permission.CanCreateReleases);
  }

  [Fact]
  public void ProjectPermission_CanViewAnalytics_ShouldReturnTrueWhenHasAnalyticsPermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Analytics);

    // Act & Assert
    Assert.True(permission.CanViewAnalytics);
  }

  [Fact]
  public void ProjectPermission_CanModerate_ShouldReturnTrueWhenHasReviewPermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Review);

    // Act & Assert
    Assert.True(permission.CanModerate);
  }

  [Fact]
  public void ProjectPermission_CanArchive_ShouldReturnTrueWhenHasArchivePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Archive);

    // Act & Assert
    Assert.True(permission.CanArchive);
  }

  [Fact]
  public void ProjectPermission_CanTransferOwnership_ShouldReturnTrueWhenHasLicensePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.License);

    // Act & Assert
    Assert.True(permission.CanTransferOwnership);
  }

  [Fact]
  public void ProjectPermission_CanDownload_ShouldReturnTrueWhenHasReadPermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Read);

    // Act & Assert
    Assert.True(permission.CanDownload);
  }

  [Fact]
  public void ProjectPermission_CanFork_ShouldReturnTrueWhenHasClonePermissionAndValid() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Clone);

    // Act & Assert
    Assert.True(permission.CanFork);
  }

  [Theory]
  [InlineData(PermissionType.Edit)]
  [InlineData(PermissionType.Delete)]
  [InlineData(PermissionType.Publish)]
  [InlineData(PermissionType.Read)]
  public void ProjectPermission_AllPermissions_ShouldReturnFalseWhenNotValid(PermissionType permissionType) {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(permissionType);
    permission.SoftDelete(); // This makes IsValid false

    // Act & Assert
    switch (permissionType) {
      case PermissionType.Edit: Assert.False(permission.CanEdit); break;
      case PermissionType.Delete: Assert.False(permission.CanDelete); break;
      case PermissionType.Publish: Assert.False(permission.CanPublish); break;
      case PermissionType.Read: Assert.False(permission.CanDownload); break;
    }
  }

  [Fact]
  public void ProjectPermission_ShouldInheritFromResourcePermission() {
    // Arrange & Act
    var permission = new ProjectPermission();

    // Assert
    Assert.IsAssignableFrom<ResourcePermission<Project>>(permission);
  }

  [Fact]
  public void ProjectPermission_ShouldHaveCorrectTableConfiguration() {
    // Arrange
    var permission = new ProjectPermission();

    // Act & Assert
    var tableAttribute = typeof(ProjectPermission)
                         .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false)
                         .Cast<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()
                         .FirstOrDefault();

    Assert.NotNull(tableAttribute);
    Assert.Equal("ProjectPermissions", tableAttribute.Name);
  }

  [Fact]
  public void ProjectPermission_WhenNoPermissions_ShouldReturnFalseForAllProperties() {
    // Arrange
    var permission = new ProjectPermission();

    // Act & Assert
    Assert.False(permission.CanEdit);
    Assert.False(permission.CanDelete);
    Assert.False(permission.CanPublish);
    Assert.False(permission.CanManageCollaborators);
    Assert.False(permission.CanCreateReleases);
    Assert.False(permission.CanViewAnalytics);
    Assert.False(permission.CanModerate);
    Assert.False(permission.CanArchive);
    Assert.False(permission.CanTransferOwnership);
    Assert.False(permission.CanDownload);
    Assert.False(permission.CanFork);
  }

  [Fact]
  public void ProjectPermission_WithMultiplePermissions_ShouldReturnTrueForAll() {
    // Arrange
    var permission = new ProjectPermission();
    permission.AddPermission(PermissionType.Edit);
    permission.AddPermission(PermissionType.Delete);
    permission.AddPermission(PermissionType.Publish);
    permission.AddPermission(PermissionType.Share);
    permission.AddPermission(PermissionType.Create);
    permission.AddPermission(PermissionType.Analytics);
    permission.AddPermission(PermissionType.Review);
    permission.AddPermission(PermissionType.Archive);
    permission.AddPermission(PermissionType.License);
    permission.AddPermission(PermissionType.Read);
    permission.AddPermission(PermissionType.Clone);

    // Act & Assert
    Assert.True(permission.CanEdit);
    Assert.True(permission.CanDelete);
    Assert.True(permission.CanPublish);
    Assert.True(permission.CanManageCollaborators);
    Assert.True(permission.CanCreateReleases);
    Assert.True(permission.CanViewAnalytics);
    Assert.True(permission.CanModerate);
    Assert.True(permission.CanArchive);
    Assert.True(permission.CanTransferOwnership);
    Assert.True(permission.CanDownload);
    Assert.True(permission.CanFork);
  }
}
