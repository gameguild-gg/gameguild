using GameGuild.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Users.Models;
using GameGuild.Modules.Users.Services;


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
      // Seed each component independently
      await SeedSuperAdminUserAsync();
      await SeedGlobalDefaultPermissionsAsync();
      await SeedGlobalProjectDefaultPermissionsAsync();
      await SeedTenantDomainDefaultPermissionsAsync();
      await SeedSampleCoursesAsync();
      await SeedSampleTracksAsync();

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
    var tenantResourceTypes = new[] { "TenantDomain", "TenantUserGroup", "TenantUserGroupMembership" };

    foreach (var resourceType in tenantResourceTypes) {
      var permissions = new[] { PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete, };

      await permissionService.GrantContentTypePermissionAsync(null, null, resourceType, permissions);
      logger.LogInformation(
        "Content-type default permissions seeded for {ResourceType} with {PermissionsLength} permissions",
        resourceType,
        permissions.Length
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
      }
      else { logger.LogInformation("Password credential already exists for super admin"); }
    }
    else {
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
    var globalPermissions = new PermissionType[] { PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Publish, PermissionType.Approve, PermissionType.Review };

    await permissionService.GrantTenantPermissionAsync(createdUser.Id, null, globalPermissions);
    logger.LogInformation("Granted {PermissionCount} global tenant permissions to super admin", globalPermissions.Length);

    // Grant essential content type permissions
    var contentTypes = new[] { "Project", "TenantDomain", "TenantUserGroup", "TenantUserGroupMembership", "User", "Tenant", "Comment", "Product", "Program" };

    var contentPermissions = new PermissionType[] { PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete };

    foreach (var contentType in contentTypes) {
      await permissionService.GrantContentTypePermissionAsync(
        createdUser.Id,
        null,
        contentType,
        contentPermissions
      );
      logger.LogInformation("Granted permissions for content type {ContentType} to super admin", contentType);
    }

    logger.LogInformation("Super admin user seeding completed successfully with email: {Email}", createdUser.Email);
  }

  public async Task SeedSampleCoursesAsync() {
    logger.LogInformation("Seeding sample courses...");

    // Check if programs already exist
    var existingPrograms = await context.Set<GameGuild.Modules.Programs.Models.Program>().AnyAsync();
    if (existingPrograms) {
      logger.LogInformation("Programs already exist, skipping seeding");
      return;
    }

    var samplePrograms = new[] {
      // Programming Courses (Beginner)
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Introduction to Game Programming",
        Description = "Learn the fundamentals of game programming with C# and Unity. Build your first game from scratch and understand core concepts like game loops, physics, and user input.",
        Slug = "intro-game-programming",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Python for Game Development",
        Description = "Discover game development using Python and Pygame. Perfect for beginners who want to learn programming through game creation.",
        Slug = "python-game-development",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "JavaScript Game Development",
        Description = "Create browser-based games using HTML5 Canvas and JavaScript. Learn modern web game development techniques.",
        Slug = "javascript-game-development",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Programming Courses (Intermediate)
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Unity 3D Game Development",
        Description = "Master 3D game development with Unity engine. Learn advanced scripting, physics, and 3D mathematics for game development.",
        Slug = "unity-3d-game-development",
        EstimatedHours = 60,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Unreal Engine C++ Programming",
        Description = "Learn advanced game programming with Unreal Engine and C++. Build AAA-quality games with professional tools.",
        Slug = "unreal-cpp-programming",
        EstimatedHours = 70,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Mobile Game Development with Flutter",
        Description = "Create cross-platform mobile games using Flutter and Dart. Learn to publish games on both iOS and Android.",
        Slug = "flutter-mobile-games",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.MobileDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Programming Courses (Advanced)
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Engine Architecture",
        Description = "Build your own game engine from scratch. Learn about rendering pipelines, memory management, and performance optimization.",
        Slug = "game-engine-architecture",
        EstimatedHours = 120,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Multiplayer Networking in Games",
        Description = "Master multiplayer game development with authoritative servers, client prediction, and lag compensation techniques.",
        Slug = "multiplayer-networking",
        EstimatedHours = 80,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Art and Design Courses (Beginner)
      new GameGuild.Modules.Programs.Models.Program {
        Title = "2D Game Art Creation",
        Description = "Master the art of creating 2D sprites, animations, and game assets using Photoshop and Aseprite. Learn industry-standard techniques for pixel art and digital illustration.",
        Slug = "2d-game-art",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Pixel Art Fundamentals",
        Description = "Learn the basics of pixel art creation for indie games. Understand color theory, animation principles, and pixel-perfect techniques.",
        Slug = "pixel-art-fundamentals",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "UI/UX Design for Games",
        Description = "Design intuitive and beautiful game interfaces. Learn about user experience principles specific to game development.",
        Slug = "game-ui-ux-design",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Design,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Art and Design Courses (Intermediate)
      new GameGuild.Modules.Programs.Models.Program {
        Title = "3D Modeling for Games",
        Description = "Create game-ready 3D models using Blender. Learn low-poly modeling, UV mapping, and optimization techniques for real-time rendering.",
        Slug = "3d-modeling-games",
        EstimatedHours = 55,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Character Animation for Games",
        Description = "Bring game characters to life with professional animation techniques. Learn rigging, keyframe animation, and state machines.",
        Slug = "character-animation",
        EstimatedHours = 65,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Environment Art and Level Design",
        Description = "Create stunning game environments and design engaging levels. Learn composition, lighting, and world-building techniques.",
        Slug = "environment-art-level-design",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Game Design Courses
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Design Fundamentals",
        Description = "Learn the core principles of game design including mechanics, dynamics, and aesthetics. Understand what makes games fun and engaging.",
        Slug = "game-design-fundamentals",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Design,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Advanced Game Design Principles",
        Description = "Explore advanced concepts in game design, including player psychology, narrative design, and game mechanics. Learn to create engaging and memorable gaming experiences.",
        Slug = "advanced-game-design",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Design,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Narrative Design for Games",
        Description = "Craft compelling stories and characters for games. Learn about branching narratives, dialogue systems, and environmental storytelling.",
        Slug = "narrative-design",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Design,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Analytics and Monetization",
        Description = "Learn to analyze player behavior and implement effective monetization strategies for free-to-play and premium games.",
        Slug = "game-analytics-monetization",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Business,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Audio Courses
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Audio Design",
        Description = "Create immersive soundscapes and effects for games. Learn about dynamic audio, spatial sound, and interactive music systems.",
        Slug = "game-audio-design",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Music Composition for Games",
        Description = "Compose adaptive and interactive music for video games. Learn about looping, layering, and emotional storytelling through music.",
        Slug = "music-composition-games",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // VR/AR Courses
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Virtual Reality Game Development",
        Description = "Build immersive VR experiences using Unity and VR SDKs. Learn about VR interaction design and optimization for VR hardware.",
        Slug = "vr-game-development",
        EstimatedHours = 60,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Augmented Reality Games",
        Description = "Create AR games for mobile devices using ARCore and ARKit. Learn about marker-based and markerless AR development.",
        Slug = "ar-game-development",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Specialized Courses
      new GameGuild.Modules.Programs.Models.Program {
        Title = "AI for Game Development",
        Description = "Implement intelligent behaviors in games using AI techniques like state machines, behavior trees, and pathfinding algorithms.",
        Slug = "ai-game-development",
        EstimatedHours = 55,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.AI,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Procedural Generation in Games",
        Description = "Learn to create infinite worlds and content using procedural generation algorithms. Master noise functions, cellular automata, and more.",
        Slug = "procedural-generation",
        EstimatedHours = 65,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Physics Programming",
        Description = "Implement realistic physics systems in games. Learn about collision detection, rigid body dynamics, and particle systems.",
        Slug = "game-physics-programming",
        EstimatedHours = 70,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Programming,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Security and Anti-Cheat",
        Description = "Protect your games from cheating and exploitation. Learn about client-server validation, encryption, and anti-cheat systems.",
        Slug = "game-security-anti-cheat",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Cybersecurity,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Expert,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Business and Production
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Indie Game Marketing",
        Description = "Learn to market your indie game effectively. Understand social media marketing, community building, and launch strategies.",
        Slug = "indie-game-marketing",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Marketing,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Project Management",
        Description = "Master agile development methodologies for game projects. Learn about scrum, sprint planning, and team coordination.",
        Slug = "game-project-management",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.ProjectManagement,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Publishing and Distribution",
        Description = "Navigate the complex world of game publishing. Learn about platform requirements, legal considerations, and distribution strategies.",
        Slug = "game-publishing-distribution",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.Business,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      
      // Retro and Indie Focus
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Retro Game Development",
        Description = "Create games inspired by classic 8-bit and 16-bit consoles. Learn about limitations-driven design and authentic retro aesthetics.",
        Slug = "retro-game-development",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Game Jam Survival Guide",
        Description = "Master the art of rapid game development in game jams. Learn time management, scope control, and prototyping techniques.",
        Slug = "game-jam-survival",
        EstimatedHours = 15,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new GameGuild.Modules.Programs.Models.Program {
        Title = "Building Your Game Development Portfolio",
        Description = "Create a compelling portfolio that showcases your game development skills. Learn what recruiters and studios are looking for.",
        Slug = "game-dev-portfolio",
        EstimatedHours = 20,
        Thumbnail = "/placeholder.svg",
        Category = GameGuild.Common.Enums.ProgramCategory.PersonalDevelopment,
        Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
        Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      }
    };

    await context.Set<GameGuild.Modules.Programs.Models.Program>().AddRangeAsync(samplePrograms);
    logger.LogInformation("Sample programs seeded successfully with {ProgramCount} programs", samplePrograms.Length);
  }

  public async Task SeedSampleTracksAsync() {
    logger.LogInformation("Seeding sample tracks...");

    // Check if track programs already exist (using slug pattern to identify tracks)
    var existingTracks = await context.Set<GameGuild.Modules.Programs.Models.Program>()
      .Where(p => p.Slug.Contains("-track-") || p.Title.Contains("Track"))
      .AnyAsync();
    
    logger.LogInformation("Found {ExistingTracksCount} existing tracks", existingTracks ? "some" : "no");
    
    // Temporarily force track seeding for debugging
    // if (existingTracks) {
    //   logger.LogInformation("Track programs already exist, skipping seeding");
    //   return;
    // }

    // Get existing course programs to link to tracks
    var coursePrograms = await context.Set<GameGuild.Modules.Programs.Models.Program>()
      .Where(p => !p.Slug.Contains("-track-") && !p.Title.Contains("Track"))
      .ToListAsync();

    logger.LogInformation("Found {CourseCount} course programs for track creation", coursePrograms.Count);

    if (!coursePrograms.Any()) {
      logger.LogWarning("No course programs found. Make sure to seed courses before tracks.");
      return;
    }

    var trackPrograms = new List<GameGuild.Modules.Programs.Models.Program>();
    var trackContentList = new List<GameGuild.Modules.Programs.Models.ProgramContent>();

    // Beginner Track - Game Development Fundamentals
    var beginnerTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Beginner Track - Game Development Fundamentals",
      Description = "Kickstart your game development journey. Learn the basics of game design, programming, and art. This comprehensive track will take you from zero to your first complete game.",
      Slug = "beginner-track-game-dev-fundamentals",
      EstimatedHours = 100,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Beginner,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add related courses to beginner track as ProgramContent
    var beginnerCourses = coursePrograms
      .Where(c => c.Difficulty == GameGuild.Common.Enums.ProgramDifficulty.Beginner && 
                  (c.Category == GameGuild.Common.Enums.ProgramCategory.Programming ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.GameDevelopment ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.Design))
      .Take(5)
      .ToList();

    for (int i = 0; i < beginnerCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = beginnerTrack.Id,
        Title = beginnerCourses[i].Title,
        Description = beginnerCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page, // Using Page type to reference other programs
        Body = $"{{\"programId\": \"{beginnerCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = true,
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // Intermediate Track - 2D Game Development
    var intermediateTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Intermediate Track - 2D Game Development",
      Description = "Enhance your skills in 2D game development. Focus on Unity, C#, and pixel art. Build more complex games and learn advanced 2D techniques.",
      Slug = "intermediate-track-2d-game-dev",
      EstimatedHours = 120,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add related courses to intermediate track
    var intermediateCourses = coursePrograms
      .Where(c => c.Difficulty == GameGuild.Common.Enums.ProgramDifficulty.Intermediate && 
                  (c.Category == GameGuild.Common.Enums.ProgramCategory.Programming ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.GameDevelopment || 
                   c.Category == GameGuild.Common.Enums.ProgramCategory.CreativeArts ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.Design ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.MobileDevelopment))
      .Take(6)
      .ToList();

    for (int i = 0; i < intermediateCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = intermediateTrack.Id,
        Title = intermediateCourses[i].Title,
        Description = intermediateCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page,
        Body = $"{{\"programId\": \"{intermediateCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = i < 4, // First 4 courses are required
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // Advanced Track - Multiplayer Game Development
    var advancedTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Advanced Track - Multiplayer Game Development",
      Description = "Master advanced topics in game development. Learn about networking, multiplayer game mechanics, and server management. Build scalable multiplayer games.",
      Slug = "advanced-track-multiplayer-game-dev",
      EstimatedHours = 150,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.GameDevelopment,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add related courses to advanced track
    var advancedCourses = coursePrograms
      .Where(c => c.Difficulty == GameGuild.Common.Enums.ProgramDifficulty.Advanced && 
                  (c.Category == GameGuild.Common.Enums.ProgramCategory.GameDevelopment ||
                   c.Category == GameGuild.Common.Enums.ProgramCategory.Programming))
      .Take(7)
      .ToList();

    for (int i = 0; i < advancedCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = advancedTrack.Id,
        Title = advancedCourses[i].Title,
        Description = advancedCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page,
        Body = $"{{\"programId\": \"{advancedCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = i < 5, // First 5 courses are required
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // AI Specialization Track
    var aiTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Specialization Track - AI in Games",
      Description = "Dive deep into AI programming for games. Explore pathfinding, decision trees, behavior trees, and machine learning in game contexts.",
      Slug = "specialization-track-ai-in-games",
      EstimatedHours = 110,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.AI,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Advanced,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add AI-related courses
    var aiCourses = coursePrograms
      .Where(c => c.Category == GameGuild.Common.Enums.ProgramCategory.AI ||
                  (c.Category == GameGuild.Common.Enums.ProgramCategory.Programming && 
                   (c.Title.Contains("AI") || c.Title.Contains("Algorithm") || c.Title.Contains("Procedural") || 
                    c.Title.Contains("Physics") || c.Difficulty == GameGuild.Common.Enums.ProgramDifficulty.Advanced)))
      .Take(5)
      .ToList();

    for (int i = 0; i < aiCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = aiTrack.Id,
        Title = aiCourses[i].Title,
        Description = aiCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page,
        Body = $"{{\"programId\": \"{aiCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = true,
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // Creative Arts Track
    var creativeTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Creative Track - Game Art and Animation",
      Description = "Unleash your creativity in game art and animation. Learn 2D/3D art, animation principles, and asset creation for games.",
      Slug = "creative-track-game-art-animation",
      EstimatedHours = 130,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.CreativeArts,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add creative arts courses
    var creativeCourses = coursePrograms
      .Where(c => c.Category == GameGuild.Common.Enums.ProgramCategory.CreativeArts)
      .Take(6)
      .ToList();

    for (int i = 0; i < creativeCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = creativeTrack.Id,
        Title = creativeCourses[i].Title,
        Description = creativeCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page,
        Body = $"{{\"programId\": \"{creativeCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = i < 4, // First 4 courses are required
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // Business Track
    var businessTrack = new GameGuild.Modules.Programs.Models.Program {
      Id = Guid.NewGuid(),
      Title = "Business Track - Game Marketing and Monetization",
      Description = "Learn the business side of games. Focus on marketing, monetization strategies, project management, and building sustainable game businesses.",
      Slug = "business-track-game-marketing-monetization",
      EstimatedHours = 90,
      Thumbnail = "/placeholder.svg",
      Category = GameGuild.Common.Enums.ProgramCategory.Business,
      Difficulty = GameGuild.Common.Enums.ProgramDifficulty.Intermediate,
      Visibility = GameGuild.Modules.Contents.Models.AccessLevel.Public,
      Status = GameGuild.Modules.Contents.Models.ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    // Add business courses
    var businessCourses = coursePrograms
      .Where(c => c.Category == GameGuild.Common.Enums.ProgramCategory.Business ||
                  c.Category == GameGuild.Common.Enums.ProgramCategory.Marketing ||
                  c.Category == GameGuild.Common.Enums.ProgramCategory.ProjectManagement ||
                  (c.Category == GameGuild.Common.Enums.ProgramCategory.PersonalDevelopment))
      .Take(4)
      .ToList();

    for (int i = 0; i < businessCourses.Count; i++) {
      trackContentList.Add(new GameGuild.Modules.Programs.Models.ProgramContent {
        Id = Guid.NewGuid(),
        ProgramId = businessTrack.Id,
        Title = businessCourses[i].Title,
        Description = businessCourses[i].Description ?? "",
        Type = GameGuild.Common.Enums.ProgramContentType.Page,
        Body = $"{{\"programId\": \"{businessCourses[i].Id}\", \"type\": \"program_reference\"}}",
        SortOrder = i + 1,
        IsRequired = true,
        Visibility = GameGuild.Common.Enums.Visibility.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
    }

    // Add all tracks to the list
    trackPrograms.AddRange(new[] {
      beginnerTrack,
      intermediateTrack,
      advancedTrack,
      aiTrack,
      creativeTrack,
      businessTrack
    });

    // Save track programs and their content
    logger.LogInformation("Saving {TrackCount} tracks and {ContentCount} track content items to database", 
      trackPrograms.Count, trackContentList.Count);
    
    await context.Set<GameGuild.Modules.Programs.Models.Program>().AddRangeAsync(trackPrograms);
    await context.Set<GameGuild.Modules.Programs.Models.ProgramContent>().AddRangeAsync(trackContentList);

    logger.LogInformation("Sample tracks seeded successfully with {TrackCount} tracks and {ContentCount} track content items", 
      trackPrograms.Count, trackContentList.Count);
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
