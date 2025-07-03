using GameGuild.Data;
using GameGuild.Common.Entities;
using GameGuild.Modules.User.Models;
using GameGuild.Modules.User.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;


namespace GameGuild.Common.Services;

/// <summary>
/// Database seeder for initial data setup
/// </summary>
public class DatabaseSeeder(
  ApplicationDbContext context,
  IPermissionService permissionService,
  IUserService userService,
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

      await SeedSuperAdminUserAsync();
      await SeedGlobalDefaultPermissionsAsync();
      await SeedGlobalProjectDefaultPermissionsAsync();
      await SeedTenantDomainDefaultPermissionsAsync();

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

  public async Task SeedTenantDomainDefaultPermissionsAsync() {
    logger.LogInformation("Seeding tenant domain content-type default permissions...");

    // Grant default permissions for TenantDomain, TenantUserGroup, and TenantUserGroupMembership
    var tenantResourceTypes = new[] {
      "TenantDomain",
      "TenantUserGroup", 
      "TenantUserGroupMembership"
    };

    foreach (var resourceType in tenantResourceTypes) {
      var permissions = new[] {
        PermissionType.Read,
        PermissionType.Create,
        PermissionType.Edit,
        PermissionType.Delete,
      };

      await permissionService.GrantContentTypePermissionAsync(null, null, resourceType, permissions);
      logger.LogInformation(
        "Content-type default permissions seeded for {ResourceType} with {PermissionsLength} permissions",
        resourceType, permissions.Length
      );
    }
  }

  public async Task SeedSuperAdminUserAsync() {
    logger.LogInformation("Seeding super admin user...");

    // Check if super admin already exists
    var existingSuperAdmin = await context.Users
      .Include(u => u.Credentials)
      .FirstOrDefaultAsync(u => u.Email == "admin@gameguild.local");

    User createdUser;

    if (existingSuperAdmin != null) {
      logger.LogInformation("Super admin user already exists");
      createdUser = existingSuperAdmin;
      
      // Check if password credential exists
      var existingPasswordCredential = existingSuperAdmin.Credentials
        .FirstOrDefault(c => c is { Type: "password", IsActive: true });
        
      if (existingPasswordCredential == null) {
        logger.LogInformation("Creating password credential for existing super admin");
        
        // Create password credential for existing super admin
        var passwordCredential = new Credential {
          UserId = existingSuperAdmin.Id,
          Type = "password",
          Value = HashPassword("admin123"), // Default password for super admin
          IsActive = true,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        };

        context.Credentials.Add(passwordCredential);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Password credential created for existing super admin");
      } else {
        logger.LogInformation("Password credential already exists for super admin");
      }
    } else {
      // Create super admin user
      var superAdmin = new User {
        Name = "Super Admin",
        Email = "admin@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      createdUser = await userService.CreateUserAsync(superAdmin);

      // Create password credential for super admin
      var passwordCredential = new Credential {
        UserId = createdUser.Id,
        Type = "password",
        Value = HashPassword("admin123"), // Default password for super admin
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      context.Credentials.Add(passwordCredential);
      await context.SaveChangesAsync();
      
      logger.LogInformation("Super admin user created successfully with email: {Email} and password credential", createdUser.Email);
    }

    // Grant super admin essential permissions globally
    var globalPermissions = new PermissionType[] {
      PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete,
      PermissionType.Publish, PermissionType.Approve, PermissionType.Review
    };
    
    await permissionService.GrantTenantPermissionAsync(createdUser.Id, null, globalPermissions);
    logger.LogInformation("Granted {PermissionCount} global tenant permissions to super admin", globalPermissions.Length);

    // Grant essential content type permissions
    var contentTypes = new[] {
      "Project", "TenantDomain", "TenantUserGroup", "TenantUserGroupMembership",
      "User", "Tenant", "Comment", "Product", "Program"
    };

    var contentPermissions = new PermissionType[] {
      PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete
    };

    foreach (var contentType in contentTypes) {
      await permissionService.GrantContentTypePermissionAsync(
        createdUser.Id, null, contentType, contentPermissions);
      logger.LogInformation("Granted permissions for content type {ContentType} to super admin", contentType);
    }

    logger.LogInformation("Super admin user seeding completed successfully with email: {Email}", createdUser.Email);
  }

  /// <summary>
  /// Hash password using SHA256 (same method as AuthService)
  /// </summary>
  private static string HashPassword(string password) {
    using var sha = SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(bytes);
  }
}
