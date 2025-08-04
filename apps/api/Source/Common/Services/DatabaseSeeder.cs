using System.Security.Cryptography;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Projects;
using GameGuild.Modules.TestingLab;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common;

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
      await SeedTestingLabDefaultPermissionsAsync();
      
      // Check if mock data seeding should be skipped (e.g., during tests)
      var skipMockData = Environment.GetEnvironmentVariable("SKIP_MOCK_DATA_SEEDING") == "true";
      
      if (!skipMockData) {
        await SeedSampleCoursesAsync();
        await SeedSampleTracksAsync();
        await SeedMockUsersAsync();
        await SeedMockProjectsAsync();
        await SeedMockTestingLocationsAsync();
        await SeedMockTestingRequestsAsync();
        await SeedMockTestingSessionsAsync();
      }
      else {
        logger.LogInformation("Skipping sample/mock data seeding due to SKIP_MOCK_DATA_SEEDING environment variable");
      }

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

    // Grant default permissions for TenantUserGroup and TenantUserGroupMembership
    // Note: TenantDomain permissions should be restricted to admins, not given as defaults
    var tenantResourceTypes = new[] { "TenantUserGroup", "TenantUserGroupMembership" };

    foreach (var resourceType in tenantResourceTypes) {
      var permissions = new[] { PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete, };

      await permissionService.GrantContentTypePermissionAsync(null, null, resourceType, permissions);
      logger.LogInformation(
        "Content-type default permissions seeded for {ResourceType} with {PermissionsLength} permissions",
        resourceType,
        permissions.Length
      );
    }

    // For TenantDomain, only grant Read permissions by default
    // Create/Edit/Delete should be restricted to users with explicit admin permissions
    var tenantDomainPermissions = new[] { PermissionType.Read };
    await permissionService.GrantContentTypePermissionAsync(null, null, "TenantDomain", tenantDomainPermissions);
    logger.LogInformation(
      "Content-type default permissions seeded for TenantDomain with {PermissionsLength} permissions (Read only)",
      tenantDomainPermissions.Length
    );
  }

  public async Task SeedTestingLabDefaultPermissionsAsync() {
    logger.LogInformation("Seeding testing lab content-type default permissions...");

    // Grant default permissions for TestingSession, TestingRequest, TestingFeedback, and SessionRegistration
    var testingLabResourceTypes = new[] { "TestingSession", "TestingRequest", "TestingFeedback", "SessionRegistration" };

    foreach (var resourceType in testingLabResourceTypes) {
      var permissions = new[] { 
        PermissionType.Read,    // Allow users to view testing sessions and requests
        PermissionType.Create,  // Allow users to create testing requests
        PermissionType.Edit,    // Allow users to edit their own testing content
        PermissionType.Delete, // Allow users to delete their own testing content
      };

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
        UpdatedAt = DateTime.UtcNow,
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
    var contentTypes = new[] { "Project", "TenantDomain", "TenantUserGroup", "TenantUserGroupMembership", "User", "Tenant", "Comment", "Product", "Program", "TestingSession", "TestingRequest", "TestingFeedback", "SessionRegistration" };

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
    var existingPrograms = await context.Set<Modules.Programs.Program>().AnyAsync();

    if (existingPrograms) {
      logger.LogInformation("Programs already exist, skipping seeding");

      return;
    }

    var samplePrograms = new[] {
      // Programming Courses (Beginner)
      new Modules.Programs.Program {
        Title = "Introduction to Game Programming",
        Description = "Learn the fundamentals of game programming with C# and Unity. Build your first game from scratch and understand core concepts like game loops, physics, and user input.",
        Slug = "intro-game-programming",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Python for Game Development",
        Description = "Discover game development using Python and Pygame. Perfect for beginners who want to learn programming through game creation.",
        Slug = "python-game-development",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "JavaScript Game Development",
        Description = "Create browser-based games using HTML5 Canvas and JavaScript. Learn modern web game development techniques.",
        Slug = "javascript-game-development",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Programming Courses (Intermediate)
      new Modules.Programs.Program {
        Title = "Unity 3D Game Development",
        Description = "Master 3D game development with Unity engine. Learn advanced scripting, physics, and 3D mathematics for game development.",
        Slug = "unity-3d-game-development",
        EstimatedHours = 60,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Unreal Engine C++ Programming",
        Description = "Learn advanced game programming with Unreal Engine and C++. Build AAA-quality games with professional tools.",
        Slug = "unreal-cpp-programming",
        EstimatedHours = 70,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Mobile Game Development with Flutter",
        Description = "Create cross-platform mobile games using Flutter and Dart. Learn to publish games on both iOS and Android.",
        Slug = "flutter-mobile-games",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.MobileDevelopment,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Programming Courses (Advanced)
      new Modules.Programs.Program {
        Title = "Game Engine Architecture",
        Description = "Build your own game engine from scratch. Learn about rendering pipelines, memory management, and performance optimization.",
        Slug = "game-engine-architecture",
        EstimatedHours = 120,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Multiplayer Networking in Games",
        Description = "Master multiplayer game development with authoritative servers, client prediction, and lag compensation techniques.",
        Slug = "multiplayer-networking",
        EstimatedHours = 80,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Art and Design Courses (Beginner)
      new Modules.Programs.Program {
        Title = "2D Game Art Creation",
        Description = "Master the art of creating 2D sprites, animations, and game assets using Photoshop and Aseprite. Learn industry-standard techniques for pixel art and digital illustration.",
        Slug = "2d-game-art",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Pixel Art Fundamentals",
        Description = "Learn the basics of pixel art creation for indie games. Understand color theory, animation principles, and pixel-perfect techniques.",
        Slug = "pixel-art-fundamentals",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "UI/UX Design for Games",
        Description = "Design intuitive and beautiful game interfaces. Learn about user experience principles specific to game development.",
        Slug = "game-ui-ux-design",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Design,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Art and Design Courses (Intermediate)
      new Modules.Programs.Program {
        Title = "3D Modeling for Games",
        Description = "Create game-ready 3D models using Blender. Learn low-poly modeling, UV mapping, and optimization techniques for real-time rendering.",
        Slug = "3d-modeling-games",
        EstimatedHours = 55,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Character Animation for Games",
        Description = "Bring game characters to life with professional animation techniques. Learn rigging, keyframe animation, and state machines.",
        Slug = "character-animation",
        EstimatedHours = 65,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Environment Art and Level Design",
        Description = "Create stunning game environments and design engaging levels. Learn composition, lighting, and world-building techniques.",
        Slug = "environment-art-level-design",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Game Design Courses
      new Modules.Programs.Program {
        Title = "Game Design Fundamentals",
        Description = "Learn the core principles of game design including mechanics, dynamics, and aesthetics. Understand what makes games fun and engaging.",
        Slug = "game-design-fundamentals",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Design,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Advanced Game Design Principles",
        Description = "Explore advanced concepts in game design, including player psychology, narrative design, and game mechanics. Learn to create engaging and memorable gaming experiences.",
        Slug = "advanced-game-design",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Design,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Narrative Design for Games",
        Description = "Craft compelling stories and characters for games. Learn about branching narratives, dialogue systems, and environmental storytelling.",
        Slug = "narrative-design",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Design,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Analytics and Monetization",
        Description = "Learn to analyze player behavior and implement effective monetization strategies for free-to-play and premium games.",
        Slug = "game-analytics-monetization",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Business,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Audio Courses
      new Modules.Programs.Program {
        Title = "Game Audio Design",
        Description = "Create immersive soundscapes and effects for games. Learn about dynamic audio, spatial sound, and interactive music systems.",
        Slug = "game-audio-design",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Music Composition for Games",
        Description = "Compose adaptive and interactive music for video games. Learn about looping, layering, and emotional storytelling through music.",
        Slug = "music-composition-games",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.CreativeArts,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // VR/AR Courses
      new Modules.Programs.Program {
        Title = "Virtual Reality Game Development",
        Description = "Build immersive VR experiences using Unity and VR SDKs. Learn about VR interaction design and optimization for VR hardware.",
        Slug = "vr-game-development",
        EstimatedHours = 60,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.GameDevelopment,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Augmented Reality Games",
        Description = "Create AR games for mobile devices using ARCore and ARKit. Learn about marker-based and markerless AR development.",
        Slug = "ar-game-development",
        EstimatedHours = 50,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.GameDevelopment,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Specialized Courses
      new Modules.Programs.Program {
        Title = "AI for Game Development",
        Description = "Implement intelligent behaviors in games using AI techniques like state machines, behavior trees, and pathfinding algorithms.",
        Slug = "ai-game-development",
        EstimatedHours = 55,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.AI,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Procedural Generation in Games",
        Description = "Learn to create infinite worlds and content using procedural generation algorithms. Master noise functions, cellular automata, and more.",
        Slug = "procedural-generation",
        EstimatedHours = 65,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Physics Programming",
        Description = "Implement realistic physics systems in games. Learn about collision detection, rigid body dynamics, and particle systems.",
        Slug = "game-physics-programming",
        EstimatedHours = 70,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Programming,
        Difficulty = ProgramDifficulty.Advanced,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Security and Anti-Cheat",
        Description = "Protect your games from cheating and exploitation. Learn about client-server validation, encryption, and anti-cheat systems.",
        Slug = "game-security-anti-cheat",
        EstimatedHours = 45,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Cybersecurity,
        Difficulty = ProgramDifficulty.Expert,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Business and Production
      new Modules.Programs.Program {
        Title = "Indie Game Marketing",
        Description = "Learn to market your indie game effectively. Understand social media marketing, community building, and launch strategies.",
        Slug = "indie-game-marketing",
        EstimatedHours = 30,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Marketing,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Project Management",
        Description = "Master agile development methodologies for game projects. Learn about scrum, sprint planning, and team coordination.",
        Slug = "game-project-management",
        EstimatedHours = 35,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.ProjectManagement,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Publishing and Distribution",
        Description = "Navigate the complex world of game publishing. Learn about platform requirements, legal considerations, and distribution strategies.",
        Slug = "game-publishing-distribution",
        EstimatedHours = 25,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.Business,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },

      // Retro and Indie Focus
      new Modules.Programs.Program {
        Title = "Retro Game Development",
        Description = "Create games inspired by classic 8-bit and 16-bit consoles. Learn about limitations-driven design and authentic retro aesthetics.",
        Slug = "retro-game-development",
        EstimatedHours = 40,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.GameDevelopment,
        Difficulty = ProgramDifficulty.Intermediate,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Game Jam Survival Guide",
        Description = "Master the art of rapid game development in game jams. Learn time management, scope control, and prototyping techniques.",
        Slug = "game-jam-survival",
        EstimatedHours = 15,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.GameDevelopment,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Modules.Programs.Program {
        Title = "Building Your Game Development Portfolio",
        Description = "Create a compelling portfolio that showcases your game development skills. Learn what recruiters and studios are looking for.",
        Slug = "game-dev-portfolio",
        EstimatedHours = 20,
        Thumbnail = "/placeholder.svg",
        Category = ProgramCategory.PersonalDevelopment,
        Difficulty = ProgramDifficulty.Beginner,
        Visibility = AccessLevel.Public,
        Status = ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
    };

    await context.Set<Modules.Programs.Program>().AddRangeAsync(samplePrograms);
    logger.LogInformation("Sample programs seeded successfully with {ProgramCount} programs", samplePrograms.Length);
  }

  public async Task SeedSampleTracksAsync() {
    logger.LogInformation("Seeding sample tracks...");

    // Check if track programs already exist (using slug pattern to identify tracks)
    var existingTracks = await context.Set<Modules.Programs.Program>()
                                      .Where(p => p.Slug.Contains("-track-") || p.Title.Contains("Track"))
                                      .AnyAsync();

    logger.LogInformation("Found {ExistingTracksCount} existing tracks", existingTracks ? "some" : "no");

    // Temporarily force track seeding for debugging
    // if (existingTracks) {
    //   logger.LogInformation("Track programs already exist, skipping seeding");
    //   return;
    // }

    // Get existing course programs to link to tracks
    var coursePrograms = await context.Set<Modules.Programs.Program>()
                                      .Where(p => !p.Slug.Contains("-track-") && !p.Title.Contains("Track"))
                                      .ToListAsync();

    logger.LogInformation("Found {CourseCount} course programs for track creation", coursePrograms.Count);

    if (coursePrograms.Count == 0) {
      logger.LogWarning("No course programs found. Make sure to seed courses before tracks.");

      return;
    }

    var trackPrograms = new List<Modules.Programs.Program>();
    var trackContentList = new List<ProgramContent>();

    // Beginner Track - Game Development Fundamentals
    var beginnerTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Beginner Track - Game Development Fundamentals",
      Description = "Kickstart your game development journey. Learn the basics of game design, programming, and art. This comprehensive track will take you from zero to your first complete game.",
      Slug = "beginner-track-game-dev-fundamentals",
      EstimatedHours = 100,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.GameDevelopment,
      Difficulty = ProgramDifficulty.Beginner,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add related courses to beginner track as ProgramContent
    var beginnerCourses = coursePrograms
                          .Where(c => c.Difficulty == ProgramDifficulty.Beginner &&
                                      (c.Category == ProgramCategory.Programming ||
                                       c.Category == ProgramCategory.GameDevelopment ||
                                       c.Category == ProgramCategory.Design)
                          )
                          .Take(5)
                          .ToList();

    for (int i = 0; i < beginnerCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = beginnerTrack.Id,
          Title = beginnerCourses[i].Title,
          Description = beginnerCourses[i].Description ?? "",
          Type = ProgramContentType.Page, // Using Page type to reference other programs
          Body = $"{{\"programId\": \"{beginnerCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = true,
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // Intermediate Track - 2D Game Development
    var intermediateTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Intermediate Track - 2D Game Development",
      Description = "Enhance your skills in 2D game development. Focus on Unity, C#, and pixel art. Build more complex games and learn advanced 2D techniques.",
      Slug = "intermediate-track-2d-game-dev",
      EstimatedHours = 120,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.GameDevelopment,
      Difficulty = ProgramDifficulty.Intermediate,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add related courses to intermediate track
    var intermediateCourses = coursePrograms
                              .Where(c => c.Difficulty == ProgramDifficulty.Intermediate &&
                                          (c.Category == ProgramCategory.Programming ||
                                           c.Category == ProgramCategory.GameDevelopment ||
                                           c.Category == ProgramCategory.CreativeArts ||
                                           c.Category == ProgramCategory.Design ||
                                           c.Category == ProgramCategory.MobileDevelopment)
                              )
                              .Take(6)
                              .ToList();

    for (int i = 0; i < intermediateCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = intermediateTrack.Id,
          Title = intermediateCourses[i].Title,
          Description = intermediateCourses[i].Description ?? "",
          Type = ProgramContentType.Page,
          Body = $"{{\"programId\": \"{intermediateCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = i < 4, // First 4 courses are required
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // Advanced Track - Multiplayer Game Development
    var advancedTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Advanced Track - Multiplayer Game Development",
      Description = "Master advanced topics in game development. Learn about networking, multiplayer game mechanics, and server management. Build scalable multiplayer games.",
      Slug = "advanced-track-multiplayer-game-dev",
      EstimatedHours = 150,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.GameDevelopment,
      Difficulty = ProgramDifficulty.Advanced,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add related courses to advanced track
    var advancedCourses = coursePrograms
                          .Where(c => c.Difficulty == ProgramDifficulty.Advanced &&
                                      (c.Category == ProgramCategory.GameDevelopment ||
                                       c.Category == ProgramCategory.Programming)
                          )
                          .Take(7)
                          .ToList();

    for (int i = 0; i < advancedCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = advancedTrack.Id,
          Title = advancedCourses[i].Title,
          Description = advancedCourses[i].Description ?? "",
          Type = ProgramContentType.Page,
          Body = $"{{\"programId\": \"{advancedCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = i < 5, // First 5 courses are required
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // AI Specialization Track
    var aiTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Specialization Track - AI in Games",
      Description = "Dive deep into AI programming for games. Explore pathfinding, decision trees, behavior trees, and machine learning in game contexts.",
      Slug = "specialization-track-ai-in-games",
      EstimatedHours = 110,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.AI,
      Difficulty = ProgramDifficulty.Advanced,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add AI-related courses
    var aiCourses = coursePrograms
                    .Where(c => c.Category == ProgramCategory.AI ||
                                (c.Category == ProgramCategory.Programming &&
                                 (c.Title.Contains("AI") ||
                                  c.Title.Contains("Algorithm") ||
                                  c.Title.Contains("Procedural") ||
                                  c.Title.Contains("Physics") ||
                                  c.Difficulty == ProgramDifficulty.Advanced))
                    )
                    .Take(5)
                    .ToList();

    for (int i = 0; i < aiCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = aiTrack.Id,
          Title = aiCourses[i].Title,
          Description = aiCourses[i].Description ?? "",
          Type = ProgramContentType.Page,
          Body = $"{{\"programId\": \"{aiCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = true,
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // Creative Arts Track
    var creativeTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Creative Track - Game Art and Animation",
      Description = "Unleash your creativity in game art and animation. Learn 2D/3D art, animation principles, and asset creation for games.",
      Slug = "creative-track-game-art-animation",
      EstimatedHours = 130,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.CreativeArts,
      Difficulty = ProgramDifficulty.Intermediate,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add creative arts courses
    var creativeCourses = coursePrograms
                          .Where(c => c.Category == ProgramCategory.CreativeArts)
                          .Take(6)
                          .ToList();

    for (int i = 0; i < creativeCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = creativeTrack.Id,
          Title = creativeCourses[i].Title,
          Description = creativeCourses[i].Description ?? "",
          Type = ProgramContentType.Page,
          Body = $"{{\"programId\": \"{creativeCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = i < 4, // First 4 courses are required
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // Business Track
    var businessTrack = new Modules.Programs.Program {
      Id = Guid.NewGuid(),
      Title = "Business Track - Game Marketing and Monetization",
      Description = "Learn the business side of games. Focus on marketing, monetization strategies, project management, and building sustainable game businesses.",
      Slug = "business-track-game-marketing-monetization",
      EstimatedHours = 90,
      Thumbnail = "/placeholder.svg",
      Category = ProgramCategory.Business,
      Difficulty = ProgramDifficulty.Intermediate,
      Visibility = AccessLevel.Public,
      Status = ContentStatus.Published,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    // Add business courses
    var businessCourses = coursePrograms
                          .Where(c => c.Category == ProgramCategory.Business ||
                                      c.Category == ProgramCategory.Marketing ||
                                      c.Category == ProgramCategory.ProjectManagement ||
                                      (c.Category == ProgramCategory.PersonalDevelopment)
                          )
                          .Take(4)
                          .ToList();

    for (int i = 0; i < businessCourses.Count; i++) {
      trackContentList.Add(
        new ProgramContent {
          Id = Guid.NewGuid(),
          ProgramId = businessTrack.Id,
          Title = businessCourses[i].Title,
          Description = businessCourses[i].Description ?? "",
          Type = ProgramContentType.Page,
          Body = $"{{\"programId\": \"{businessCourses[i].Id}\", \"type\": \"program_reference\"}}",
          SortOrder = i + 1,
          IsRequired = true,
          Visibility = Visibility.Published,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        }
      );
    }

    // Add all tracks to the list
    trackPrograms.AddRange(
      [beginnerTrack, intermediateTrack, advancedTrack, aiTrack, creativeTrack, businessTrack,]
    );

    // Save track programs and their content
    logger.LogInformation(
      "Saving {TrackCount} tracks and {ContentCount} track content items to database",
      trackPrograms.Count,
      trackContentList.Count
    );

    await context.Set<Modules.Programs.Program>().AddRangeAsync(trackPrograms);
    await context.Set<ProgramContent>().AddRangeAsync(trackContentList);

    logger.LogInformation(
      "Sample tracks seeded successfully with {TrackCount} tracks and {ContentCount} track content items",
      trackPrograms.Count,
      trackContentList.Count
    );
  }

  public async Task SeedMockUsersAsync() {
    logger.LogInformation("Seeding mock users...");

    // Check if mock users already exist
    var existingMockUsers = await context.Users
                                         .Where(u => u.Email.Contains("test") || u.Email.Contains("demo"))
                                         .AnyAsync();

    if (existingMockUsers) {
      logger.LogInformation("Mock users already exist, skipping seeding");
      return;
    }

    var mockUsers = new[] {
      new User {
        Name = "Alice Johnson",
        Email = "alice.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new User {
        Name = "Bob Smith",
        Email = "bob.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new User {
        Name = "Carol Williams",
        Email = "carol.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new User {
        Name = "David Brown",
        Email = "david.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new User {
        Name = "Eva Garcia",
        Email = "eva.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new User {
        Name = "Frank Miller",
        Email = "frank.test@gameguild.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
    };

    foreach (var user in mockUsers) {
      var createdUser = await userService.CreateUserAsync(user);

      // Create password credential for each mock user
      var passwordCredential = new Credential {
        UserId = createdUser.Id,
        Type = "password",
        Value = HashPassword("test123"),
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      context.Credentials.Add(passwordCredential);
    }

    await context.SaveChangesAsync();
    logger.LogInformation("Mock users seeded successfully with {UserCount} users", mockUsers.Length);
  }

  public async Task SeedMockProjectsAsync() {
    logger.LogInformation("Seeding mock projects...");

    // Check if mock projects already exist
    var existingMockProjects = await context.Set<Project>()
                                            .Where(p => p.Title.Contains("Test") || p.Title.Contains("Demo"))
                                            .AnyAsync();

    if (existingMockProjects) {
      logger.LogInformation("Mock projects already exist, skipping seeding");
      return;
    }

    // Get some mock users to assign as project creators
    var mockUsers = await context.Users
                                 .Where(u => u.Email.Contains("test"))
                                 .Take(3)
                                 .ToListAsync();

    if (mockUsers.Count == 0) {
      logger.LogWarning("No mock users found, cannot create mock projects");
      return;
    }

    var mockProjects = new[] {
      new Project {
        Title = "Space Explorer Demo",
        Description = "A 3D space exploration game where players navigate through different planets and collect resources. Features stunning visuals and engaging gameplay mechanics.",
        ShortDescription = "Explore the universe in this immersive 3D space adventure game.",
        Type = Common.ProjectType.Game,
        DevelopmentStatus = DevelopmentStatus.InDevelopment,
        ImageUrl = "/placeholder.svg",
        WebsiteUrl = "https://spaceexplorer.demo",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        CreatedById = mockUsers[0].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Project {
        Title = "Puzzle Quest Test Game",
        Description = "A challenging puzzle game that combines classic match-3 mechanics with RPG elements. Players solve puzzles to progress through an epic adventure.",
        ShortDescription = "Match-3 puzzle game with RPG elements and epic storyline.",
        Type = Common.ProjectType.Game,
        DevelopmentStatus = DevelopmentStatus.Beta,
        ImageUrl = "/placeholder.svg",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        CreatedById = mockUsers[1].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Project {
        Title = "Racing Simulator Demo",
        Description = "Realistic racing simulator with authentic physics, detailed car models, and multiple racing tracks from around the world.",
        ShortDescription = "Realistic racing simulation with authentic physics and detailed tracks.",
        Type = Common.ProjectType.Game,
        DevelopmentStatus = DevelopmentStatus.Alpha,
        ImageUrl = "/placeholder.svg",
        RepositoryUrl = "https://github.com/demo/racing-sim",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        CreatedById = mockUsers[2].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Project {
        Title = "Strategy War Test",
        Description = "Turn-based strategy game where players build armies, manage resources, and conquer territories in a medieval fantasy setting.",
        ShortDescription = "Medieval fantasy turn-based strategy with army building and conquest.",
        Type = Common.ProjectType.Game,
        DevelopmentStatus = DevelopmentStatus.InDevelopment,
        ImageUrl = "/placeholder.svg",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        CreatedById = mockUsers[0].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new Project {
        Title = "Retro Platformer Demo",
        Description = "Classic 2D platformer inspired by the golden age of gaming. Features pixel art graphics, challenging levels, and nostalgic gameplay.",
        ShortDescription = "Classic 2D platformer with pixel art and challenging gameplay.",
        Type = Common.ProjectType.Game,
        DevelopmentStatus = DevelopmentStatus.Released,
        ImageUrl = "/placeholder.svg",
        DownloadUrl = "https://store.example.com/retro-platformer",
        Status = ContentStatus.Published,
        Visibility = AccessLevel.Public,
        CreatedById = mockUsers[1].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
    };

    await context.Set<Project>().AddRangeAsync(mockProjects);
    await context.SaveChangesAsync();

    // Create project versions for each project
    foreach (var project in mockProjects) {
      var projectVersion = new ProjectVersion {
        ProjectId = project.Id,
        VersionNumber = "1.0.0",
        ReleaseNotes = "Initial release version for testing purposes.",
        Status = "published",
        CreatedById = project.CreatedById ?? Guid.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      context.Set<ProjectVersion>().Add(projectVersion);
    }

    await context.SaveChangesAsync();
    logger.LogInformation("Mock projects seeded successfully with {ProjectCount} projects", mockProjects.Length);
  }

  public async Task SeedMockTestingLocationsAsync() {
    logger.LogInformation("Seeding mock testing locations...");

    // Check if testing locations already exist
    var existingLocations = await context.Set<TestingLocation>().AnyAsync();

    if (existingLocations) {
      logger.LogInformation("Testing locations already exist, skipping seeding");
      return;
    }

    var mockLocations = new[] {
      new TestingLocation {
        Name = "Downtown Gaming Lab",
        Description = "Modern gaming facility in the heart of downtown with state-of-the-art equipment and comfortable testing environment.",
        Address = "123 Gaming Street, Downtown City, ST 12345",
        MaxTestersCapacity = 20,
        MaxProjectsCapacity = 5,
        EquipmentAvailable = "Gaming PCs, Consoles (PS5, Xbox Series X, Nintendo Switch), VR Headsets, High-end audio equipment",
        Status = LocationStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new TestingLocation {
        Name = "University Research Center",
        Description = "Academic testing facility with focus on user experience research and data collection.",
        Address = "456 University Avenue, Campus Town, ST 67890",
        MaxTestersCapacity = 15,
        MaxProjectsCapacity = 3,
        EquipmentAvailable = "Research PCs, Eye-tracking systems, Biometric sensors, Recording equipment",
        Status = LocationStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new TestingLocation {
        Name = "Tech Hub Innovation Space",
        Description = "Collaborative workspace for indie developers and testing enthusiasts.",
        Address = "789 Innovation Drive, Tech District, ST 13579",
        MaxTestersCapacity = 12,
        MaxProjectsCapacity = 4,
        EquipmentAvailable = "Development workstations, Mobile devices, Streaming setup, Collaboration tools",
        Status = LocationStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
    };

    await context.Set<TestingLocation>().AddRangeAsync(mockLocations);
    await context.SaveChangesAsync();
    logger.LogInformation("Mock testing locations seeded successfully with {LocationCount} locations", mockLocations.Length);
  }

  public async Task SeedMockTestingRequestsAsync() {
    logger.LogInformation("Seeding mock testing requests...");

    // Check if testing requests already exist
    var existingRequests = await context.Set<TestingRequest>().AnyAsync();

    if (existingRequests) {
      logger.LogInformation("Testing requests already exist, skipping seeding");
      return;
    }

    // Get mock projects and their versions
    var projectVersions = await context.Set<ProjectVersion>()
                                       .Include(pv => pv.Project)
                                       .Where(pv => pv.Project.Title.Contains("Test") || pv.Project.Title.Contains("Demo"))
                                       .ToListAsync();

    if (projectVersions.Count == 0) {
      logger.LogWarning("No mock project versions found, cannot create testing requests");
      return;
    }

    var mockUsers = await context.Users
                                 .Where(u => u.Email.Contains("test"))
                                 .ToListAsync();

    if (mockUsers.Count == 0) {
      logger.LogWarning("No mock users found, cannot create testing requests");
      return;
    }

    var mockRequests = new[] {
      new TestingRequest {
        ProjectVersionId = projectVersions[0].Id,
        Title = "Space Explorer Beta Testing",
        Description = "We need feedback on the new space exploration mechanics and planet generation system. Focus on gameplay balance and user experience.",
        DownloadUrl = "https://downloads.example.com/space-explorer-beta.zip",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "1. Download and install the game\n2. Create a new character\n3. Complete the tutorial\n4. Explore at least 3 different planets\n5. Collect various resources\n6. Try the spacecraft customization\n7. Report any bugs or issues\n8. Provide feedback on gameplay mechanics",
        FeedbackFormContent = "1. How intuitive was the navigation system?\n2. Did you encounter any bugs?\n3. How would you rate the graphics quality?\n4. What features would you like to see improved?\n5. Overall rating (1-10)?",
        MaxTesters = 15,
        StartDate = DateTime.UtcNow.AddDays(-5),
        EndDate = DateTime.UtcNow.AddDays(10),
        Status = TestingRequestStatus.Open,
        CreatedById = mockUsers[0].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new TestingRequest {
        ProjectVersionId = projectVersions[1].Id,
        Title = "Puzzle Quest Gameplay Testing",
        Description = "Help us test the new puzzle mechanics and difficulty balancing. We're particularly interested in the RPG progression system.",
        DownloadUrl = "https://downloads.example.com/puzzle-quest-alpha.zip",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "1. Install the game\n2. Play through the first 10 levels\n3. Try different character classes\n4. Test the crafting system\n5. Attempt a boss battle\n6. Note any difficulty spikes\n7. Check for UI/UX issues",
        FeedbackFormContent = "1. Was the difficulty progression appropriate?\n2. Which character class did you prefer?\n3. How was the puzzle-to-RPG balance?\n4. Any suggestions for improvement?\n5. Rate your enjoyment (1-10)?",
        MaxTesters = 20,
        StartDate = DateTime.UtcNow.AddDays(-3),
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open,
        CreatedById = mockUsers[1].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new TestingRequest {
        ProjectVersionId = projectVersions[2].Id,
        Title = "Racing Sim Physics Testing",
        Description = "We need experienced racing game players to test our new physics engine and provide feedback on car handling and track design.",
        DownloadUrl = "https://downloads.example.com/racing-sim-demo.zip",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "1. Download and install the racing simulator\n2. Calibrate your steering controls\n3. Try at least 5 different cars\n4. Race on all available tracks\n5. Test different weather conditions\n6. Focus on physics realism\n7. Report any handling issues",
        FeedbackFormContent = "1. How realistic did the car physics feel?\n2. Which car was your favorite to drive?\n3. Any issues with track design?\n4. How was the weather effect implementation?\n5. Overall physics rating (1-10)?",
        MaxTesters = 10,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(14),
        Status = TestingRequestStatus.Open,
        CreatedById = mockUsers[2].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
      new TestingRequest {
        ProjectVersionId = projectVersions[3].Id,
        Title = "Strategy War Balance Testing",
        Description = "Test the new faction balance changes and provide feedback on gameplay mechanics and AI behavior.",
        DownloadUrl = "https://downloads.example.com/strategy-war-beta.zip",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "1. Install the strategy game\n2. Play tutorial campaign\n3. Try each faction in skirmish mode\n4. Play against AI opponents\n5. Test multiplayer if available\n6. Focus on balance issues\n7. Report AI behavior problems",
        FeedbackFormContent = "1. Which faction felt most/least powerful?\n2. How was the AI difficulty?\n3. Any balance issues you noticed?\n4. Suggestions for improvement?\n5. Strategy depth rating (1-10)?",
        MaxTesters = 12,
        StartDate = DateTime.UtcNow.AddDays(2),
        EndDate = DateTime.UtcNow.AddDays(21),
        Status = TestingRequestStatus.Draft,
        CreatedById = mockUsers[0].Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      },
    };

    await context.Set<TestingRequest>().AddRangeAsync(mockRequests);
    await context.SaveChangesAsync();
    logger.LogInformation("Mock testing requests seeded successfully with {RequestCount} requests", mockRequests.Length);
  }

  public async Task SeedMockTestingSessionsAsync() {
    logger.LogInformation("Seeding mock testing sessions...");

    // Check if testing sessions already exist
    var existingSessions = await context.Set<TestingSession>().AnyAsync();

    if (existingSessions) {
      logger.LogInformation("Testing sessions already exist, skipping seeding");
      return;
    }

    // Get testing requests and locations
    var testingRequests = await context.Set<TestingRequest>()
                                       .Where(tr => tr.Status == TestingRequestStatus.Open)
                                       .ToListAsync();

    var testingLocations = await context.Set<TestingLocation>()
                                        .Where(tl => tl.Status == LocationStatus.Active)
                                        .ToListAsync();

    var mockUsers = await context.Users
                                 .Where(u => u.Email.Contains("test"))
                                 .ToListAsync();

    if (testingRequests.Count == 0 || testingLocations.Count == 0 || mockUsers.Count == 0) {
      logger.LogWarning("Insufficient data to create testing sessions (requests: {RequestCount}, locations: {LocationCount}, users: {UserCount})", 
                       testingRequests.Count, testingLocations.Count, mockUsers.Count);
      return;
    }

    var baseDate = DateTime.UtcNow;
    var mockSessions = new List<TestingSession>();

    // Create sessions for the next 2 weeks
    for (int day = 1; day <= 14; day++) {
      var sessionDate = baseDate.AddDays(day);
      
      // Skip weekends for some variety
      if (sessionDate.DayOfWeek == DayOfWeek.Saturday || sessionDate.DayOfWeek == DayOfWeek.Sunday) {
        continue;
      }

      // Create 1-3 sessions per day
      var sessionsPerDay = new Random().Next(1, 4);
      
      for (int session = 0; session < sessionsPerDay; session++) {
        var startHour = 9 + (session * 4); // 9 AM, 1 PM, 5 PM
        var sessionStart = sessionDate.Date.AddHours(startHour);
        var sessionEnd = sessionStart.AddHours(2); // 2-hour sessions

        var randomRequest = testingRequests[new Random().Next(testingRequests.Count)];
        var randomLocation = testingLocations[new Random().Next(testingLocations.Count)];
        var randomManager = mockUsers[new Random().Next(mockUsers.Count)];

        mockSessions.Add(new TestingSession {
          TestingRequestId = randomRequest.Id,
          LocationId = randomLocation.Id,
          SessionName = $"{randomRequest.Title} - Session {session + 1}",
          SessionDate = sessionDate,
          StartTime = sessionStart,
          EndTime = sessionEnd,
          MaxTesters = Math.Min(randomLocation.MaxTestersCapacity, 15),
          MaxProjects = Math.Min(randomLocation.MaxProjectsCapacity, 5), // Add MaxProjects capacity
          RegisteredTesterCount = new Random().Next(0, 8), // Random number of registered testers
          RegisteredProjectCount = new Random().Next(1, 4), // Random number of registered projects
          Status = SessionStatus.Scheduled,
          ManagerId = randomManager.Id,
          ManagerUserId = randomManager.Id,
          CreatedById = randomManager.Id,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        });
      }
    }

    // Add some completed sessions in the past
    for (int day = 1; day <= 7; day++) {
      var pastDate = baseDate.AddDays(-day);
      if (pastDate.DayOfWeek == DayOfWeek.Saturday || pastDate.DayOfWeek == DayOfWeek.Sunday) {
        continue;
      }

      var randomRequest = testingRequests[new Random().Next(testingRequests.Count)];
      var randomLocation = testingLocations[new Random().Next(testingLocations.Count)];
      var randomManager = mockUsers[new Random().Next(mockUsers.Count)];

      mockSessions.Add(new TestingSession {
        TestingRequestId = randomRequest.Id,
        LocationId = randomLocation.Id,
        SessionName = $"{randomRequest.Title} - Completed Session",
        SessionDate = pastDate,
        StartTime = pastDate.Date.AddHours(14), // 2 PM
        EndTime = pastDate.Date.AddHours(16), // 4 PM
        MaxTesters = 10,
        MaxProjects = 3, // Add MaxProjects capacity
        RegisteredTesterCount = new Random().Next(5, 11),
        RegisteredProjectCount = new Random().Next(1, 4), // Add registered project count
        Status = SessionStatus.Completed,
        ManagerId = randomManager.Id,
        ManagerUserId = randomManager.Id,
        CreatedById = randomManager.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      });
    }

    await context.Set<TestingSession>().AddRangeAsync(mockSessions);
    await context.SaveChangesAsync();

    // Now seed SessionProjects for each session
    await SeedSessionProjectsAsync(mockSessions);

    logger.LogInformation("Mock testing sessions seeded successfully with {SessionCount} sessions", mockSessions.Count);
  }

  /// <summary>
  /// Seed SessionProjects (projects registered to be tested in sessions)
  /// </summary>
  private async Task SeedSessionProjectsAsync(List<TestingSession> sessions) {
    logger.LogInformation("Seeding session projects...");

    // Get available projects
    var projects = await context.Set<Project>()
                                .Where(p => p.Status == ContentStatus.Published)
                                .Take(20) // Limit to 20 projects for seeding
                                .ToListAsync();

    if (projects.Count == 0) {
      logger.LogWarning("No published projects found for session project seeding");
      return;
    }

    var sessionProjects = new List<SessionProject>();
    var random = new Random();

    foreach (var session in sessions) {
      // Register 1-3 random projects per session (up to MaxProjects)
      var projectCount = Math.Min(random.Next(1, 4), session.MaxProjects);
      var selectedProjects = projects.OrderBy(x => random.Next()).Take(projectCount);

      foreach (var project in selectedProjects) {
        sessionProjects.Add(new SessionProject {
          SessionId = session.Id,
          ProjectId = project.Id,
          Notes = $"Testing {project.Title} in {session.SessionName}",
          RegisteredAt = DateTime.UtcNow.AddDays(-random.Next(1, 7)), // Random registration date in past week
          RegisteredById = session.CreatedById,
          IsActive = true,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
        });
      }
    }

    await context.Set<SessionProject>().AddRangeAsync(sessionProjects);
    await context.SaveChangesAsync();
    logger.LogInformation("Session projects seeded successfully with {ProjectCount} session-project relationships", sessionProjects.Count);
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
