using GameGuild.Data;
using GameGuild.Common.Entities;


namespace GameGuild.Common.Services;

/// <summary>
/// Database seeder for initial data setup
/// </summary>
public class DatabaseSeeder(
  ApplicationDbContext context,
  IPermissionService permissionService,
  ILogger<DatabaseSeeder> logger
) : IDatabaseSeeder {
  public async Task SeedAsync() {
    logger.LogInformation("Starting database seeding...");

    try {
      // Check if global default permissions have already been seeded
      var existingPermissions = await permissionService.GetGlobalDefaultPermissionsAsync();

      if (existingPermissions.Any()) {
        logger.LogInformation("Global default permissions already exist, skipping seeding");

        return;
      }

      await SeedGlobalDefaultPermissionsAsync();
      await SeedGlobalProjectDefaultPermissionsAsync();

      await context.SaveChangesAsync();

      logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error occurred during database seeding");

      throw;
    }
  }

  public async Task SeedGlobalDefaultPermissionsAsync() {
    logger.LogInformation("Seeding global default permissions...");

    // Define basic permissions that every user should have by default
    // Note: CREATE permission for tenants should be explicitly granted, not a global default
    var defaultPermissions = new[] {
      PermissionType.Read, // Allow users to read public content
      PermissionType.Comment, // Allow commenting on content
      PermissionType.Vote, // Allow voting on content
      PermissionType.Share, // Allow sharing content
      PermissionType.Follow, // Allow following other users
      PermissionType.Bookmark, // Allow bookmarking content
    };

    await permissionService.SetGlobalDefaultPermissionsAsync(defaultPermissions);
    logger.LogInformation(
      "Global default permissions seeded successfully with {DefaultPermissionsLength} permissions",
      defaultPermissions.Length
    );
  }

  public async Task SeedGlobalProjectDefaultPermissionsAsync() {
    logger.LogInformation("Seeding content-type default permissions...");

    // Grant default permissions for Projects so users can create and manage their own projects
    // Setting userId and tenantId to null makes these global defaults for the content type
    var projectPermissions = new[] {
      PermissionType.Read,
      PermissionType.Create, // Allow users to create projects
      PermissionType.Edit, // Allow users to edit their own projects
      PermissionType.Delete, // Allow users to delete their own projects
    };

    await permissionService.GrantContentTypePermissionAsync(null, null, "Project", projectPermissions);
    logger.LogInformation(
      "Content-type default permissions seeded for Project with {ProjectPermissionsLength} permissions",
      projectPermissions.Length
    );
  }
}
