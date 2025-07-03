using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using GameGuild.Data;
using GameGuild.Modules.Project.Models;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;


namespace GameGuild.Tests.Modules.Project.E2E.API;

/// <summary>
/// Integration tests for Project DAC (Data Access Control) permission system
/// Tests the 3-layer hierarchical permission checking for Project operations
/// </summary>
public class ProjectDACIntegrationTests : IDisposable {
  private readonly ApplicationDbContext _context;

  private readonly Mock<IPermissionService> _mockPermissionService;

  public ProjectDACIntegrationTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase($"DACTestDb_{Guid.NewGuid()}")
                  .Options;

    _context = new ApplicationDbContext(options);
    _mockPermissionService = new Mock<IPermissionService>();
  }

  [Fact]
  public async Task ProjectPermission_ResourceLevel_ShouldGrantAccessToSpecificProject() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    var project = new GameGuild.Modules.Project.Models.Project { Id = projectId, Title = "Test Project", Description = "Test project for DAC", CreatedById = userId };

    var projectPermission = new ProjectPermission { UserId = userId, TenantId = tenantId, ResourceId = projectId, ExpiresAt = DateTime.UtcNow.AddDays(30) };
    projectPermission.AddPermission(PermissionType.Edit);

    _context.Projects.Add(project);
    _context.ProjectPermissions.Add(projectPermission);
    await _context.SaveChangesAsync();

    // Mock the permission service to return true for resource permission
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(true);

    // Act
    var hasPermission = await CheckProjectPermissionHierarchy(
                          _mockPermissionService.Object,
                          userId,
                          tenantId,
                          projectId,
                          PermissionType.Edit
                        );

    // Assert
    Assert.True(hasPermission); // "User should have edit permission at resource level"

    // Verify the permission exists in database
    var dbPermission =
      await _context.ProjectPermissions.FirstOrDefaultAsync(p => p.UserId == userId && p.ResourceId == projectId);

    Assert.NotNull(dbPermission);
    Assert.True(dbPermission.CanEdit);
  }

  [Fact]
  public async Task ProjectPermission_ContentTypeLevel_ShouldGrantAccessWhenResourceLevelDenied() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    // Mock the permission service hierarchy
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(false); // No resource-level permission

    _mockPermissionService.Setup(x => x.HasContentTypePermissionAsync(userId, tenantId, "Project", PermissionType.Edit))
                          .ReturnsAsync(true); // Has content-type permission

    // Act
    var hasPermission = await CheckProjectPermissionHierarchy(
                          _mockPermissionService.Object,
                          userId,
                          tenantId,
                          projectId,
                          PermissionType.Edit
                        );

    // Assert
    Assert.True(hasPermission); // "User should have edit permission at content type level"
  }

  [Fact]
  public async Task ProjectPermission_TenantLevel_ShouldGrantAccessWhenLowerLevelsDenied() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    // Mock the permission service hierarchy
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(false); // No resource-level permission

    _mockPermissionService.Setup(x => x.HasContentTypePermissionAsync(userId, tenantId, "Project", PermissionType.Edit))
                          .ReturnsAsync(false); // No content-type permission

    _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit))
                          .ReturnsAsync(true); // Has tenant-level permission

    // Act
    var hasPermission = await CheckProjectPermissionHierarchy(
                          _mockPermissionService.Object,
                          userId,
                          tenantId,
                          projectId,
                          PermissionType.Edit
                        );

    // Assert
    Assert.True(hasPermission); // "User should have edit permission at tenant level"
  }

  [Fact]
  public async Task ProjectPermission_AllLevelsDenied_ShouldDenyAccess() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    // Mock all permission levels to return false
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(false);

    _mockPermissionService.Setup(x => x.HasContentTypePermissionAsync(userId, tenantId, "Project", PermissionType.Edit))
                          .ReturnsAsync(false);

    _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit))
                          .ReturnsAsync(false);

    // Act
    var hasPermission = await CheckProjectPermissionHierarchy(
                          _mockPermissionService.Object,
                          userId,
                          tenantId,
                          projectId,
                          PermissionType.Edit
                        );

    // Assert
    Assert.False(hasPermission); // "User should not have edit permission at any level"
  }

  [Fact]
  public async Task ProjectPermission_ExpiredResourcePermission_ShouldFallbackToHigherLevel() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    var expiredPermission = new ProjectPermission {
      UserId = userId, TenantId = tenantId, ResourceId = projectId, ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
    };

    expiredPermission.AddPermission(PermissionType.Edit);

    _context.ProjectPermissions.Add(expiredPermission);
    await _context.SaveChangesAsync();

    // Mock the permission service
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(false); // Expired resource permission

    _mockPermissionService.Setup(x => x.HasContentTypePermissionAsync(userId, tenantId, "Project", PermissionType.Edit))
                          .ReturnsAsync(true); // Valid content-type permission

    // Act
    var hasPermission = await CheckProjectPermissionHierarchy(
                          _mockPermissionService.Object,
                          userId,
                          tenantId,
                          projectId,
                          PermissionType.Edit
                        );

    // Assert
    Assert.True(hasPermission); // "Should fallback to content-type permission when resource permission is expired"

    // Verify the expired permission is not valid
    var dbPermission =
      await _context.ProjectPermissions.FirstOrDefaultAsync(p => p.UserId == userId && p.ResourceId == projectId);

    Assert.NotNull(dbPermission);
    Assert.False(dbPermission.IsValid); // "Expired permission should not be valid"
  }

  [Theory]
  [InlineData(PermissionType.Read)]
  [InlineData(PermissionType.Edit)]
  [InlineData(PermissionType.Delete)]
  [InlineData(PermissionType.Publish)]
  [InlineData(PermissionType.Share)]
  public async Task ProjectPermission_SpecificPermissionTypes_ShouldCheckCorrectly(PermissionType permissionType) {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var projectId = Guid.NewGuid();

    var projectPermission = new ProjectPermission { UserId = userId, TenantId = tenantId, ResourceId = projectId, ExpiresAt = DateTime.UtcNow.AddDays(30) };
    projectPermission.AddPermission(permissionType);

    _context.ProjectPermissions.Add(projectPermission);
    await _context.SaveChangesAsync();

    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               userId,
               tenantId,
               projectId,
               permissionType
             )
      )
      .ReturnsAsync(true);

    // Act
    var hasPermission =
      await CheckProjectPermissionHierarchy(_mockPermissionService.Object, userId, tenantId, projectId, permissionType);

    // Assert
    Assert.True(hasPermission); // $"User should have {permissionType} permission"

    // Verify specific permission properties
    var dbPermission =
      await _context.ProjectPermissions.FirstOrDefaultAsync(p => p.UserId == userId && p.ResourceId == projectId);

    Assert.NotNull(dbPermission);

    switch (permissionType) {
      case PermissionType.Read: Assert.True(dbPermission.CanDownload); break;
      case PermissionType.Edit: Assert.True(dbPermission.CanEdit); break;
      case PermissionType.Delete: Assert.True(dbPermission.CanDelete); break;
      case PermissionType.Publish: Assert.True(dbPermission.CanPublish); break;
      case PermissionType.Share: Assert.True(dbPermission.CanManageCollaborators); break;
    }
  }

  [Fact]
  public async Task ProjectPermission_MultipleUsersAndProjects_ShouldIsolateCorrectly() {
    // Arrange
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var project1Id = Guid.NewGuid();
    var project2Id = Guid.NewGuid();

    // User1 has edit permission on Project1
    var permission1 = new ProjectPermission { UserId = user1Id, TenantId = tenantId, ResourceId = project1Id, ExpiresAt = DateTime.UtcNow.AddDays(30) };
    permission1.AddPermission(PermissionType.Edit);

    // User2 has read permission on Project2
    var permission2 = new ProjectPermission { UserId = user2Id, TenantId = tenantId, ResourceId = project2Id, ExpiresAt = DateTime.UtcNow.AddDays(30) };
    permission2.AddPermission(PermissionType.Read);

    _context.ProjectPermissions.AddRange(permission1, permission2);
    await _context.SaveChangesAsync();

    // Mock permission service responses
    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               user1Id,
               tenantId,
               project1Id,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(true);

    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               user1Id,
               tenantId,
               project2Id,
               PermissionType.Edit
             )
      )
      .ReturnsAsync(false);

    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               user2Id,
               tenantId,
               project2Id,
               PermissionType.Read
             )
      )
      .ReturnsAsync(true);

    _mockPermissionService
      .Setup(x => x.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
               user2Id,
               tenantId,
               project1Id,
               PermissionType.Read
             )
      )
      .ReturnsAsync(false);

    // Act & Assert
    var user1CanEditProject1 = await CheckProjectPermissionHierarchy(
                                 _mockPermissionService.Object,
                                 user1Id,
                                 tenantId,
                                 project1Id,
                                 PermissionType.Edit
                               );
    Assert.True(user1CanEditProject1); // "User1 should be able to edit Project1"

    var user1CanEditProject2 = await CheckProjectPermissionHierarchy(
                                 _mockPermissionService.Object,
                                 user1Id,
                                 tenantId,
                                 project2Id,
                                 PermissionType.Edit
                               );
    Assert.False(user1CanEditProject2); // "User1 should not be able to edit Project2"

    var user2CanReadProject2 = await CheckProjectPermissionHierarchy(
                                 _mockPermissionService.Object,
                                 user2Id,
                                 tenantId,
                                 project2Id,
                                 PermissionType.Read
                               );
    Assert.True(user2CanReadProject2); // "User2 should be able to read Project2"

    var user2CanReadProject1 = await CheckProjectPermissionHierarchy(
                                 _mockPermissionService.Object,
                                 user2Id,
                                 tenantId,
                                 project1Id,
                                 PermissionType.Read
                               );
    Assert.False(user2CanReadProject1); // "User2 should not be able to read Project1"
  }

  #region Helper Methods

  /// <summary>
  /// Simulates the DAC permission hierarchy checking logic
  /// Layer 1: Resource-specific permissions (highest priority)
  /// Layer 2: Content-type permissions 
  /// Layer 3: Tenant permissions (lowest priority)
  /// </summary>
  private static async Task<bool> CheckProjectPermissionHierarchy(
    IPermissionService permissionService, Guid userId,
    Guid tenantId, Guid projectId, PermissionType permissionType
  ) {
    // Layer 1: Check resource-specific permissions
    if (await permissionService.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(
          userId,
          tenantId,
          projectId,
          permissionType
        ))
      return true;

    // Layer 2: Check content-type permissions
    if (await permissionService.HasContentTypePermissionAsync(userId, tenantId, "Project", permissionType)) return true;

    // Layer 3: Check tenant-level permissions
    return await permissionService.HasTenantPermissionAsync(userId, tenantId, permissionType);
  }

  #endregion

  public void Dispose() { _context.Dispose(); }
}
