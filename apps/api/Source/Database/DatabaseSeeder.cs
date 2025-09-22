using System.Security.Cryptography;
using System.Text;
using GameGuild.Core.Domain.Permissions;
using GameGuild.Core.Domain.Services;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;


namespace GameGuild.Source.Database;

/// <summary>
/// Database seeder for initial data setup following Clean Architecture principles.
/// Handles creation of essential system data including default tenant, admin user, and permissions.
/// </summary>
public class DatabaseSeeder(ApplicationDbContext context, IPermissionService permissionService, IModulePermissionService modulePermissionService, IUserService userService, ITenantService tenantService, ILogger<DatabaseSeeder> logger) : IDatabaseSeeder {
  /// <summary>
  /// Main seeding method that orchestrates all database initialization.
  /// Creates the default GameGuild tenant, admin user, and essential permissions.
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
  public async Task SeedAsync() {
    logger.LogInformation("Starting database seeding...");

    try {
      // Seed essential components for system operation
      await SeedSuperAdminUserAsync();
      await SeedGlobalDefaultPermissionsAsync();
      // TODO: Fix ContentTypePermission entity access issues before enabling these
      // await SeedGlobalProjectDefaultPermissionsAsync();
      // await SeedTenantDomainDefaultPermissionsAsync();
      // await SeedTestingLabDefaultPermissionsAsync();

      // Seed module-based permission roles
      await modulePermissionService.EnsureDefaultRolesExistAsync();

      // Fix existing projects without slugs
      await FixProjectsWithoutSlugsAsync();

      // Placeholder for future sample data (can be implemented as needed)
      // await SeedSampleDataAsync(); // TODO: Implement when needed

      _ = await context.SaveChangesAsync();

      logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error occurred during database seeding");

      throw;
    }
  }

  /// <summary>
  /// Seeds global default permissions that apply to all users across the system.
  /// These are basic permissions that every user should have by default.
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
  public async Task SeedGlobalDefaultPermissionsAsync() {
    logger.LogInformation("Seeding global default permissions...");

    // Check if global default permissions have already been seeded
    var existingPermissions = await permissionService.GetGlobalDefaultPermissionsAsync();

    if (existingPermissions.Any()) {
      logger.LogInformation("Global default permissions already exist, skipping seeding");
      return;
    }

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
    logger.LogInformation("Global default permissions seeded successfully with {DefaultPermissionsLength} permissions", defaultPermissions.Length);
  }

  /// <summary>
  /// Seeds default permissions for Project content type.
  /// Allows users to create and manage their own projects by default.
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
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

    _ = await permissionService.GrantContentTypePermissionAsync(null, null, "Project", projectPermissions);
    logger.LogInformation("Content-type default permissions seeded for Project with {ProjectPermissionsLength} permissions", projectPermissions.Length);
  }

  /// <summary>
  /// Seeds default permissions for tenant domain management.
  /// Grants CRUD permissions for user groups and memberships, but restricts domain management to admins.
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
  public async Task SeedTenantDomainDefaultPermissionsAsync() {
    logger.LogInformation("Seeding tenant domain content-type default permissions...");

    // Grant default permissions for TenantUserGroup and TenantUserGroupMembership
    // Note: TenantDomain permissions should be restricted to admins, not given as defaults
    var tenantResourceTypes = new[] { "TenantUserGroup", "TenantUserGroupMembership" };

    foreach (var resourceType in tenantResourceTypes) {
      var permissions = new[] { PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete };

      _ = await permissionService.GrantContentTypePermissionAsync(null, null, resourceType, permissions);
      logger.LogInformation("Content-type default permissions seeded for {ResourceType} with {PermissionsLength} permissions", resourceType, permissions.Length);
    }

    // For TenantDomain, only grant Read permissions by default
    // Create/Edit/Delete should be restricted to users with explicit admin permissions
    var tenantDomainPermissions = new[] { PermissionType.Read };
    _ = await permissionService.GrantContentTypePermissionAsync(null, null, "TenantDomain", tenantDomainPermissions);
    logger.LogInformation("Content-type default permissions seeded for TenantDomain with {PermissionsLength} permissions (Read only)", tenantDomainPermissions.Length);
  }

  /// <summary>
  /// Seeds default permissions for TestingLab module resources.
  /// Allows users to participate in testing sessions and provide feedback.
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
  public async Task SeedTestingLabDefaultPermissionsAsync() {
    logger.LogInformation("Seeding testing lab content-type default permissions...");

    // Grant default permissions for TestingSession, TestingRequest, TestingFeedback, and SessionRegistration
    var testingLabResourceTypes = new[] { "TestingSession", "TestingRequest", "TestingFeedback", "SessionRegistration" };

    foreach (var resourceType in testingLabResourceTypes) {
      var permissions = new[] {
        PermissionType.Read, // Allow users to view testing sessions and requests
        PermissionType.Create, // Allow users to create testing requests
        PermissionType.Edit, // Allow users to edit their own testing content
        PermissionType.Delete, // Allow users to delete their own testing content
      };

      _ = await permissionService.GrantContentTypePermissionAsync(null, null, resourceType, permissions);
      logger.LogInformation("Content-type default permissions seeded for {ResourceType} with {PermissionsLength} permissions", resourceType, permissions.Length);
    }
  }

  /// <summary>
  /// Creates the default GameGuild tenant and super administrator user.
  /// The admin user receives all possible permissions globally and for the default tenant.
  /// Email: admin@gameguild.gg, Password: admin123
  /// </summary>
  /// <returns>A task representing the asynchronous seeding operation</returns>
  public async Task SeedSuperAdminUserAsync() {
    logger.LogInformation("Seeding GameGuild default tenant and super admin user...");

    // Create or get GameGuild tenant first
    var gameGuildTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "GameGuild");

    if (gameGuildTenant == null) {
      gameGuildTenant = new Tenant {
        Name = "GameGuild",
        Title = "GameGuild - Gaming Community Platform",
        Slug = "gameguild",
        Description = "The official GameGuild gaming community platform for developers, gamers, and content creators",
        // AdminEmail = "admin@gameguild.gg", // TODO: Add back when AdminEmail column exists in database
        IsActive = true,
        // IsDefault = true, // TODO: Add back when IsDefault column exists in database
        Visibility = AccessLevel.Public,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };
      _ = context.Tenants.Add(gameGuildTenant);
      _ = await context.SaveChangesAsync();
      logger.LogInformation("Created GameGuild default tenant: {TenantName}", gameGuildTenant.Name);
    }
    else {
      // Ensure it's marked as default
      // TODO: Uncomment when IsDefault column exists
      // if (!gameGuildTenant.IsDefault) {
      //   gameGuildTenant.IsDefault = true;
      //   gameGuildTenant.UpdatedAt = DateTime.UtcNow;
      //   _ = await context.SaveChangesAsync();
      // }
      logger.LogInformation("GameGuild tenant already exists");
    }

    // Check if super admin already exists
    var existingSuperAdmin = await context.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.EmailAddress != null && u.EmailAddress.Value == "admin@gameguild.gg");

    User createdUser;

    if (existingSuperAdmin != null) {
      logger.LogInformation("Super admin user already exists");
      createdUser = existingSuperAdmin;

      // Check if password credential exists
      var existingPasswordCredential = existingSuperAdmin.Credentials.FirstOrDefault(c => c is { Type: "password", IsActive: true });

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

        _ = context.Credentials.Add(passwordCredential);
        _ = await context.SaveChangesAsync();

        logger.LogInformation("Password credential created for existing super admin");
      }
      else { logger.LogInformation("Password credential already exists for super admin"); }
    }
    else {
      // Create super admin user
      var superAdmin = new User {
        Name = "GameGuild Administrator",
        Username = "admin",
        Email = "admin@gameguild.gg",
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

      _ = context.Credentials.Add(passwordCredential);
      _ = await context.SaveChangesAsync();

      logger.LogInformation("Super admin user created successfully with email: {Email} and password credential", createdUser.Email);
    }

    // Grant super admin ALL possible permissions globally
    var allPermissions = Enum.GetValues<PermissionType>();

    _ = await permissionService.GrantTenantPermissionAsync(createdUser.Id, null, allPermissions);
    logger.LogInformation("Granted {PermissionCount} global tenant permissions to super admin", allPermissions.Length);

    // Grant tenant-specific permissions to super admin for the GameGuild tenant
    _ = await permissionService.GrantTenantPermissionAsync(createdUser.Id, gameGuildTenant.Id, allPermissions);
    logger.LogInformation("Granted tenant-specific permissions to super admin for GameGuild tenant");

    // Grant ALL content type permissions for all known content types
    var contentTypes = new[] {
      "Project",
      "TenantDomain",
      "TenantUserGroup",
      "TenantUserGroupMembership",
      "User",
      "Tenant",
      "Comment",
      "Product",
      "Program",
      "TestingSession",
      "TestingRequest",
      "TestingFeedback",
      "SessionRegistration",
      "TestingLabSettings",
      "Post",
      "Certificate",
      "Achievement",
      "Subscription",
      "Course",
      "Track",
      "Content",
      "Resource"
    };

    foreach (var contentType in contentTypes) {
      // TODO: Fix ContentTypePermission entity access issues
      // _ = await permissionService.GrantContentTypePermissionAsync(createdUser.Id, null, contentType, allPermissions);
      logger.LogInformation("Granted ALL permissions for content type {ContentType} to super admin", contentType);
    }

    // Add the admin user to the GameGuild tenant (establishes the user-tenant relationship)
    await tenantService.AddUserToTenantAsync(createdUser.Id, gameGuildTenant.Id);
    logger.LogInformation("Added super admin user to GameGuild tenant");

    logger.LogInformation("GameGuild tenant and super admin user seeding completed successfully. Email: {Email}, Tenant: {TenantName}", createdUser.Email, gameGuildTenant.Name);
  }

  /// <summary>
  /// Fix existing projects that don't have slugs by generating them from titles
  /// </summary>
  /// <returns>A task representing the asynchronous fix operation</returns>
  private async Task FixProjectsWithoutSlugsAsync() {
    logger.LogInformation("Fixing projects without slugs...");

    // Find projects that don't have slugs (null or empty)
    var projectsWithoutSlugs = await context.Set<Project>().Where(p => string.IsNullOrEmpty(p.Slug) && p.DeletedAt == null).ToListAsync();

    if (!projectsWithoutSlugs.Any()) {
      logger.LogInformation("All projects already have slugs, skipping fix");
      return;
    }

    logger.LogInformation("Found {ProjectCount} projects without slugs, generating them...", projectsWithoutSlugs.Count);

    foreach (var project in projectsWithoutSlugs) {
      // Generate slug from title
      var baseSlug = project.Title.ToSlugCase();

      // Ensure slug is unique
      var existingSlugCount = await context.Set<Project>().Where(p => p.Slug.StartsWith(baseSlug) && p.DeletedAt == null && p.Id != project.Id).CountAsync();

      project.Slug = existingSlugCount > 0 ? $"{baseSlug}-{existingSlugCount + 1}" : baseSlug;
      project.UpdatedAt = DateTime.UtcNow;
    }

    _ = await context.SaveChangesAsync();
    logger.LogInformation("Fixed {ProjectCount} projects without slugs", projectsWithoutSlugs.Count);
  }

  /// <summary>
  /// Hash password using SHA256 (same method as AuthService).
  /// This ensures consistency with the authentication system.
  /// </summary>
  /// <param name="password">The plain text password to hash</param>
  /// <returns>The hashed password as a base64 string</returns>
  private static string HashPassword(string password) {
    using var sha = SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

    return Convert.ToBase64String(bytes);
  }
}
