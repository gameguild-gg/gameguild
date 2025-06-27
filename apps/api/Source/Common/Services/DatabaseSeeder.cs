using GameGuild.Data;
using GameGuild.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Common.Services;

/// <summary>
/// Database seeder for initial data setup
/// </summary>
public class DatabaseSeeder : IDatabaseSeeder {
  private readonly ApplicationDbContext _context;
  private readonly IPermissionService _permissionService;
  private readonly ILogger<DatabaseSeeder> _logger;

  public DatabaseSeeder(ApplicationDbContext context, IPermissionService permissionService, ILogger<DatabaseSeeder> logger) {
    _context = context;
    _permissionService = permissionService;
    _logger = logger;
  }

  public async Task SeedAsync() {
    _logger.LogInformation("Starting database seeding...");

    try {
      // Check if global default permissions have already been seeded
      var existingPermissions = await _permissionService.GetGlobalDefaultPermissionsAsync();

      if (existingPermissions.Any()) {
        _logger.LogInformation("Global default permissions already exist, skipping seeding");

        return;
      }

      await SeedGlobalDefaultPermissionsAsync();
      await SeedContentTypeDefaultPermissionsAsync();

      await _context.SaveChangesAsync();
      _logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred during database seeding");

      throw;
    }
  }

  public async Task SeedGlobalDefaultPermissionsAsync() {
    _logger.LogInformation("Seeding global default permissions...");

    // Define basic permissions that every user should have by default
    // Note: CREATE permission for tenants should be explicitly granted, not a global default
    var defaultPermissions = new[] {
      PermissionType.Read, // Allow users to read public content
      PermissionType.Comment, // Allow commenting on content
      PermissionType.Vote, // Allow voting on content
      PermissionType.Share, // Allow sharing content
      PermissionType.Follow, // Allow following other users
      PermissionType.Bookmark // Allow bookmarking content
    };

    await _permissionService.SetGlobalDefaultPermissionsAsync(defaultPermissions);
    _logger.LogInformation($"Global default permissions seeded successfully with {defaultPermissions.Length} permissions");
  }

  public async Task SeedContentTypeDefaultPermissionsAsync() {
    _logger.LogInformation("Seeding content-type default permissions...");

    // Grant default permissions for Projects so users can create and manage their own projects
    // Setting userId and tenantId to null makes these global defaults for the content type
    var projectPermissions = new[] {
      PermissionType.Read,
      PermissionType.Create, // Allow users to create projects
      PermissionType.Edit, // Allow users to edit their own projects
      PermissionType.Delete // Allow users to delete their own projects
    };

    await _permissionService.GrantContentTypePermissionAsync(null, null, "Project", projectPermissions);
    _logger.LogInformation($"Content-type default permissions seeded for Project with {projectPermissions.Length} permissions");
  }
}
