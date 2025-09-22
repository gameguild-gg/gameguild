using GameGuild;
using GameGuild.Core.Infrastructure.Permissions;
using GameGuild.Database;
using GameGuild.Modules.Comments;
using GameGuild.Modules.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Manual;

/// <summary>
/// Manual validation of permission system functionality
/// </summary>
public class PermissionSystemValidator {
  
  public static async Task<bool> ValidatePermissionSystemAsync() {
    // Set up in-memory database context
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    using var context = new ApplicationDbContext(options);
    var logger = new LoggerFactory().CreateLogger<PermissionService>();
    var permissionService = new PermissionService(context, logger);

    try {
      // Test basic resource permission functionality
      var userId = Guid.NewGuid();
      var tenantId = Guid.NewGuid();
      var resourceId = Guid.NewGuid();
      var permissions = new[] { PermissionType.Read, PermissionType.Edit };

      // Grant permissions
      await permissionService.GrantResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        permissions
      );

      // Check if permissions were granted correctly
      var hasRead = await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Read
      );

      var hasEdit = await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Edit
      );

      var hasDelete = await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        PermissionType.Delete
      );

      // Get all permissions for the resource
      var allPermissions = await permissionService.GetResourcePermissionsAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId
      );

      Console.WriteLine($"Validation Results:");
      Console.WriteLine($"Has Read: {hasRead}");
      Console.WriteLine($"Has Edit: {hasEdit}");
      Console.WriteLine($"Has Delete: {hasDelete}");
      Console.WriteLine($"All permissions count: {allPermissions.Count()}");
      Console.WriteLine($"All permissions: {string.Join(", ", allPermissions)}");

      // Expected results:
      // - Should have Read: true
      // - Should have Edit: true  
      // - Should have Delete: false
      // - Should have 2 permissions total (Read, Edit)

      bool isValid = hasRead && hasEdit && !hasDelete && allPermissions.Count() == 2;
      Console.WriteLine($"Overall validation: {(isValid ? "PASSED" : "FAILED")}");
      
      return isValid;
    }
    catch (Exception ex) {
      Console.WriteLine($"Validation failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
  }
}
