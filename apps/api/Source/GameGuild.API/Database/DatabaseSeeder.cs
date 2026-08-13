using Microsoft.AspNetCore.Identity;
using GameGuild.Identity.Tenants;
using GameGuild.LaunchPad;
using GameGuild.Projects;
using GameGuild.TestingLab;

namespace GameGuild.API.Database;

// Type aliases for ASP.NET Core Identity
using Role = IdentityRole;
using LegacyIdentityUser = IdentityUser;
using AppUser = GameGuild.Identity.Users.User;

/// <summary>
///     Database seeder for default roles and admin user
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    ///     Seeds the database with default roles and admin user.
    ///     Note: Database creation/migration is handled by DatabaseInitializationService.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // Ensure ApplicationDbContext is registered (validates DI configuration)
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Try to get Identity managers - they may not be registered in all configurations
        var userManager = serviceProvider.GetService<UserManager<LegacyIdentityUser>>();
        var roleManager = serviceProvider.GetService<RoleManager<Role>>();
        var logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();
        var configuration = serviceProvider.GetService<IConfiguration>();

        // Seed roles if RoleManager is available
        if (roleManager != null)
        {
            await SeedRolesAsync(roleManager, logger).ConfigureAwait(false);
        }
        else
        {
            logger?.LogInformation("RoleManager not registered - skipping legacy role seeding");
        }

        // Seed the application user that the live /auth/sign-in endpoint authenticates against.
        var adminUser = await SeedApplicationAdminUserAsync(dbContext, logger, configuration).ConfigureAwait(false);
        await SeedPlatformTenantAsync(dbContext, adminUser, logger, configuration).ConfigureAwait(false);
        await SeedProjectBackedDemoWorkflowsAsync(dbContext, adminUser, logger).ConfigureAwait(false);

        // Seed legacy ASP.NET Identity admin user if UserManager is available.
        // This is kept for compatibility with older identity surfaces.
        if (userManager != null)
        {
            await SeedLegacyIdentityAdminUserAsync(userManager, logger, configuration).ConfigureAwait(false);
        }
        else
        {
            logger?.LogInformation("UserManager not registered - skipping legacy admin user seeding");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, ILogger? logger)
    {
        var roles = new[] { "Admin", "User", "Manager" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };

                var result = await roleManager.CreateAsync(role).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    logger?.LogInformation("  Created role: {RoleName}", roleName);
                }
            }
        }
    }

    private static async Task<AppUser> SeedApplicationAdminUserAsync(ApplicationDbContext dbContext, ILogger? logger, IConfiguration? configuration = null)
    {
        const string adminEmail = "admin@game-guild.com";
        const string adminName = "Game Guild Admin";
        const string adminUsername = "admin";
        var adminPassword = configuration?["Seed:AdminPassword"] ?? "Admin123!";

        var adminUser = await dbContext.Set<AppUser>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Email == adminEmail)
            .ConfigureAwait(false);

        if (adminUser is null)
        {
            adminUser = AppUser.CreateWithPassword(
                adminEmail,
                adminName,
                BCrypt.Net.BCrypt.HashPassword(adminPassword),
                adminUsername);
            adminUser.VerifyEmail();

            dbContext.Set<AppUser>().Add(adminUser);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            logger?.LogInformation("  Created application admin user: {Email}", adminEmail);
            return adminUser;
        }

        var updated = false;

        if (adminUser.IsDeleted)
        {
            adminUser.Restore();
            updated = true;
        }

        if (!adminUser.HasPassword || adminUser.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(adminPassword, adminUser.PasswordHash))
        {
            adminUser.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(adminPassword));
            updated = true;
        }

        if (!adminUser.IsEmailVerified)
        {
            adminUser.VerifyEmail();
            updated = true;
        }

        if (!adminUser.IsActive)
        {
            adminUser.Activate();
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(adminUser.Username))
        {
            adminUser.Username = adminUsername;
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(adminUser.Name))
        {
            adminUser.Name = adminName;
            updated = true;
        }

        if (updated)
        {
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            logger?.LogInformation("  Updated application admin user: {Email}", adminEmail);
        }

        return adminUser;
    }

    private static async Task SeedPlatformTenantAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        ILogger? logger,
        IConfiguration? configuration = null)
    {
        const string defaultTenantName = "GameGuild Platform";
        const string defaultTenantSlug = "gameguild-platform";
        const string defaultTenantDescription = "Default platform tenant for GameGuild administration.";

        var tenantName = configuration?["Seed:DefaultTenantName"] ?? defaultTenantName;
        var tenantSlug = configuration?["Seed:DefaultTenantSlug"] ?? defaultTenantSlug;
        var tenantDescription = configuration?["Seed:DefaultTenantDescription"] ?? defaultTenantDescription;
        var tenantRole = configuration?["Seed:AdminTenantRole"] ?? "SystemAdmin";

        var tenants = dbContext.Set<Tenant>();
        var tenant = await tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Slug == tenantSlug || item.IsDefault)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = tenantName,
                Slug = tenantSlug,
                Description = tenantDescription,
                AdminEmail = adminUser.Email,
                IsActive = true,
                IsDefault = true
            };
            tenants.Add(tenant);
            logger?.LogInformation("  Created default platform tenant: {TenantSlug}", tenantSlug);
        }
        else
        {
            tenant.Name = string.IsNullOrWhiteSpace(tenant.Name) ? tenantName : tenant.Name;
            tenant.Slug = string.IsNullOrWhiteSpace(tenant.Slug) ? tenantSlug : tenant.Slug;
            tenant.Description = string.IsNullOrWhiteSpace(tenant.Description) ? tenantDescription : tenant.Description;
            tenant.AdminEmail = string.IsNullOrWhiteSpace(tenant.AdminEmail) ? adminUser.Email : tenant.AdminEmail;
            tenant.IsActive = true;
            tenant.IsArchived = false;
            tenant.ArchivedAt = null;
            tenant.IsDefault = true;
        }

        var otherDefaultTenants = await tenants
            .IgnoreQueryFilters()
            .Where(item => item.Id != tenant.Id && item.IsDefault)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var otherTenant in otherDefaultTenants)
        {
            otherTenant.IsDefault = false;
        }

        var tenantMembers = dbContext.Set<TenantMember>();
        var membership = await tenantMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == tenant.Id && item.UserId == adminUser.Id)
            .ConfigureAwait(false);

        if (membership is null)
        {
            tenantMembers.Add(new TenantMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                Role = tenantRole,
                IsActive = true,
                JoinedAt = SystemClock.UtcNow,
                Metadata = """{"bootstrap":true,"scope":"platform"}"""
            });
            logger?.LogInformation("  Created platform tenant admin membership for: {Email}", adminUser.Email);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(membership.Role) ||
                string.Equals(membership.Role, TenantRole.Owner.Value, StringComparison.OrdinalIgnoreCase))
            {
                membership.Role = tenantRole;
            }
            membership.Activate();
        }

        if (!await dbContext.Set<TenantSettings>()
                .IgnoreQueryFilters()
                .AnyAsync(item => item.TenantId == tenant.Id)
                .ConfigureAwait(false))
        {
            dbContext.Set<TenantSettings>().Add(TenantSettings.CreateDefault(tenant.Id));
        }

        if (!await dbContext.Set<TenantStatistics>()
                .IgnoreQueryFilters()
                .AnyAsync(item => item.TenantId == tenant.Id)
                .ConfigureAwait(false))
        {
            dbContext.Set<TenantStatistics>().Add(new TenantStatistics
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StatisticDate = SystemClock.UtcNow.Date,
                TotalMembers = 1,
                ActiveMembers = 1,
                NewMembers = 1
            });
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedProjectBackedDemoWorkflowsAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        ILogger? logger)
    {
        var seedProjects = new[]
        {
            new DemoWorkflowSeed(
                "Neon Runner",
                "gameguild-showcase-neon-runner",
                "Fast parkour runner focused on readable combat arenas and replayable time trials.",
                "0.7.0",
                "Remote Playtest Room",
                "Validate first-session onboarding and combat clarity.",
                "Steam, Itch.io, Discord"),
            new DemoWorkflowSeed(
                "Skyforge Arena",
                "gameguild-showcase-skyforge-arena",
                "Student-built arena battler with modular abilities, team roles, and spectator feedback loops.",
                "0.4.0",
                "Campus QA Lab",
                "Stress test match pacing, ability readability, and controller feel.",
                "Website, Discord, Newsletter"),
            new DemoWorkflowSeed(
                "Echo Grove",
                "gameguild-showcase-echo-grove",
                "Narrative exploration prototype balancing environmental puzzles with lightweight creature systems.",
                "0.3.2",
                "Remote Playtest Room",
                "Measure puzzle comprehension, accessibility notes, and emotional beats.",
                "Website, Itch.io, Press kit"),
        };

        var locations = await SeedTestingLocationsAsync(dbContext).ConfigureAwait(false);

        foreach (var seed in seedProjects)
        {
            var project = await EnsureSeedProjectAsync(dbContext, adminUser, seed).ConfigureAwait(false);
            var version = await EnsureSeedProjectVersionAsync(dbContext, adminUser, project, seed).ConfigureAwait(false);
            await EnsureSeedProjectReleaseAsync(dbContext, project, seed).ConfigureAwait(false);

            var request = await EnsureSeedTestingRequestAsync(dbContext, adminUser, version, seed).ConfigureAwait(false);
            var location = locations.FirstOrDefault(candidate => candidate.Name == seed.TestingLocationName) ?? locations[0];
            await EnsureSeedTestingSessionAsync(dbContext, adminUser, request, location, seed).ConfigureAwait(false);
            await EnsureSeedLaunchPlanAsync(dbContext, project, seed).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        logger?.LogInformation("  Seeded project-backed Testing Lab and Launch Pad starter workflows");
    }

    private static async Task<List<TestingLocation>> SeedTestingLocationsAsync(ApplicationDbContext dbContext)
    {
        var definitions = new[]
        {
            new
            {
                Name = "Remote Playtest Room",
                Description = "Moderated online lab for remote student project playtests.",
                IsVirtual = true,
                City = (string?)null,
                Country = "Online",
                Capacity = 24,
                MaxProjectsCapacity = 6,
                VirtualUrl = (string?)"https://meet.gameguild.gg/testing-lab",
                Equipment = "Discord, screen share, capture notes, controller checklist"
            },
            new
            {
                Name = "Campus QA Lab",
                Description = "In-person QA station with controller, keyboard, and accessibility coverage.",
                IsVirtual = false,
                City = (string?)"Orlando",
                Country = "United States",
                Capacity = 18,
                MaxProjectsCapacity = 4,
                VirtualUrl = (string?)null,
                Equipment = "PC stations, gamepads, capture cards, observer seats"
            }
        };

        foreach (var definition in definitions)
        {
            var location = await dbContext.Set<TestingLocation>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(candidate => candidate.Name == definition.Name)
                .ConfigureAwait(false);

            if (location is null)
            {
                location = new TestingLocation
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    ContactEmail = "testing-lab@gameguild.gg"
                };
                dbContext.Set<TestingLocation>().Add(location);
            }

            location.Description = definition.Description;
            location.IsVirtual = definition.IsVirtual;
            location.City = definition.City;
            location.Country = definition.Country;
            location.Capacity = definition.Capacity;
            location.MaxProjectsCapacity = definition.MaxProjectsCapacity;
            location.VirtualUrl = definition.VirtualUrl;
            location.Equipment = definition.Equipment;
            location.Status = LocationStatus.Active;
            location.DeletedAt = null;
            location.Touch();
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return await dbContext.Set<TestingLocation>()
            .Where(location => definitions.Select(definition => definition.Name).Contains(location.Name))
            .OrderBy(location => location.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    private static async Task<Project> EnsureSeedProjectAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        DemoWorkflowSeed seed)
    {
        var project = await dbContext.Set<Project>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Slug == seed.Slug)
            .ConfigureAwait(false);

        if (project is null)
        {
            project = new Project
            {
                Id = Guid.NewGuid(),
                Slug = seed.Slug,
                CreatedById = adminUser.Id
            };
            dbContext.Set<Project>().Add(project);
        }

        project.Title = seed.Title;
        project.ShortDescription = seed.ShortDescription;
        project.Description = $"{seed.ShortDescription} This seeded project backs real Testing Lab and Launch Pad workflows.";
        // These demo records do not ship image assets. Keep the media fields empty so
        // clients use their valid design-system fallback instead of requesting fabricated
        // CDN paths (which surface as 404s in browsers and deployment smoke tests).
        project.ImageUrl = null;
        project.FeaturedImageUrl = null;
        project.DownloadUrl = $"https://downloads.gameguild.gg/{seed.Slug}/{seed.VersionNumber}.zip";
        project.WebsiteUrl = $"https://gameguild.gg/projects/{seed.Slug}";
        project.RepositoryUrl = $"https://github.com/gameguild/{seed.Slug}";
        project.Tags = """["student-project","testing-lab","launch-pad","seeded"]""";
        project.SocialLinks = """{"discord":"https://discord.gg/gameguild"}""";
        project.Type = GameGuild.Projects.ProjectType.Game;
        project.DevelopmentStatus = GameGuild.Projects.DevelopmentStatus.InDevelopment;
        project.Status = ContentStatus.Published;
        project.Visibility = ContentVisibility.Public;
        project.CreatedById = adminUser.Id;
        project.PublishedAt ??= SystemClock.UtcNow.AddDays(-14);
        project.DeletedAt = null;
        project.Touch();

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        return project;
    }

    private static async Task<ProjectVersion> EnsureSeedProjectVersionAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        Project project,
        DemoWorkflowSeed seed)
    {
        var version = await dbContext.Set<ProjectVersion>()
            .FirstOrDefaultAsync(candidate => candidate.ProjectId == project.Id && candidate.VersionNumber == seed.VersionNumber)
            .ConfigureAwait(false);

        if (version is null)
        {
            version = new ProjectVersion
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                VersionNumber = seed.VersionNumber,
                CreatedById = adminUser.Id
            };
            dbContext.Set<ProjectVersion>().Add(version);
        }

        version.ReleaseNotes = $"{seed.Title} seeded build for moderated playtesting and launch readiness.";
        version.Status = "testing";
        version.CreatedById = adminUser.Id;
        version.Touch();

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        return version;
    }

    private static async Task EnsureSeedProjectReleaseAsync(
        ApplicationDbContext dbContext,
        Project project,
        DemoWorkflowSeed seed)
    {
        var release = await dbContext.Set<ProjectRelease>()
            .FirstOrDefaultAsync(candidate => candidate.ProjectId == project.Id && candidate.ReleaseVersion == seed.VersionNumber)
            .ConfigureAwait(false);

        if (release is null)
        {
            release = new ProjectRelease
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReleaseVersion = seed.VersionNumber
            };
            dbContext.Set<ProjectRelease>().Add(release);
        }

        release.Title = $"{seed.Title} {seed.VersionNumber}";
        release.Description = seed.ShortDescription;
        release.ReleaseNotes = $"{seed.Focus} Capture playtester blockers before the public launch beat.";
        release.DownloadUrl = project.DownloadUrl;
        release.IsLatest = true;
        release.IsPrerelease = true;
        release.ReleaseType = "testing";
        release.Status = ContentStatus.Published;
        release.ReleasedAt = SystemClock.UtcNow.AddDays(-2);
        release.SupportedPlatforms = """["Windows","WebGL"]""";
        release.SystemRequirements = "Windows 10 or modern browser, gamepad recommended.";
        release.Touch();
    }

    private static async Task<TestingRequest> EnsureSeedTestingRequestAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        ProjectVersion version,
        DemoWorkflowSeed seed)
    {
        var title = $"{seed.Title} moderated playtest";
        var request = await dbContext.Set<TestingRequest>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.ProjectVersionId == version.Id && candidate.Title == title)
            .ConfigureAwait(false);

        if (request is null)
        {
            request = new TestingRequest
            {
                Id = Guid.NewGuid(),
                ProjectVersionId = version.Id,
                CreatedById = adminUser.Id
            };
            dbContext.Set<TestingRequest>().Add(request);
        }

        request.Title = title;
        request.Description = seed.Focus;
        request.DownloadUrl = $"https://downloads.gameguild.gg/{seed.Slug}/{seed.VersionNumber}.zip";
        request.InstructionsType = InstructionType.Text;
        request.InstructionsContent = "Install the build, play the first 20 minutes, record the first point of confusion, and submit one polish note.";
        request.FeedbackFormContent = "Where did you hesitate?\nWhat felt polished?\nWhich issue should the team fix first?";
        request.MaxTesters = 12;
        request.CurrentTesterCount = 3;
        request.StartDate = SystemClock.UtcNow.AddDays(-1);
        request.EndDate = SystemClock.UtcNow.AddDays(21);
        request.Status = TestingRequestStatus.Open;
        request.Priority = TestingPriority.High;
        request.EstimatedDurationHours = 2;
        request.Mode = TestingMode.Online;
        request.DeletedAt = null;
        request.Touch();

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        return request;
    }

    private static async Task EnsureSeedTestingSessionAsync(
        ApplicationDbContext dbContext,
        AppUser adminUser,
        TestingRequest request,
        TestingLocation location,
        DemoWorkflowSeed seed)
    {
        var sessionName = $"{seed.Title} feedback lab";
        var session = await dbContext.Set<TestingSession>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.TestingRequestId == request.Id && candidate.SessionName == sessionName)
            .ConfigureAwait(false);

        if (session is null)
        {
            session = new TestingSession
            {
                Id = Guid.NewGuid(),
                TestingRequestId = request.Id,
                SessionName = sessionName
            };
            dbContext.Set<TestingSession>().Add(session);
        }

        var start = SystemClock.UtcNow.Date.AddDays(7).AddHours(18);
        session.LocationId = location.Id;
        session.SessionDate = start.Date;
        session.StartTime = start;
        session.EndTime = start.AddHours(2);
        session.MaxTesters = 12;
        session.MaxProjects = 3;
        session.RegisteredTesterCount = 4;
        session.RegisteredProjectCount = 1;
        session.RegisteredProjectMemberCount = 2;
        session.Status = SessionStatus.Scheduled;
        session.ManagerId = adminUser.Id;
        session.ManagerUserId = adminUser.Id;
        session.CreatedById = adminUser.Id;
        session.DeletedAt = null;
        session.Touch();
    }

    private static async Task EnsureSeedLaunchPlanAsync(
        ApplicationDbContext dbContext,
        Project project,
        DemoWorkflowSeed seed)
    {
        var plan = await dbContext.Set<LaunchPlan>()
            .Include(candidate => candidate.ChecklistItems)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.ProjectId == project.Id)
            .ConfigureAwait(false);

        if (plan is null)
        {
            plan = new LaunchPlan
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id
            };
            dbContext.Set<LaunchPlan>().Add(plan);
        }

        plan.Name = $"{seed.Title} launch readiness";
        plan.Positioning = $"{seed.Title} is positioned for players who want student-made experiments with a clear playable hook. {seed.Focus}";
        plan.TargetLaunchAt = SystemClock.UtcNow.AddDays(30);
        plan.LaunchedAt = null;
        plan.Channels = seed.Channels.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        plan.DeletedAt = null;

        var checklistDefinitions = new[]
        {
            ("Storefront", "Landing page copy and key art approved", true),
            ("Quality", "Release build smoke tested through Testing Lab", true),
            ("Distribution", "Download channel, version, and release notes confirmed", true),
            ("Community", "Discord announcement and tester follow-up prepared", false),
            ("Analytics", "Launch metrics and post-launch review dashboard ready", false),
        };

        foreach (var (category, title, isComplete) in checklistDefinitions)
        {
            var item = plan.ChecklistItems.FirstOrDefault(candidate => candidate.Category == category && candidate.Title == title);
            if (item is null)
            {
                item = new LaunchChecklistItem
                {
                    Id = Guid.NewGuid(),
                    Category = category,
                    Title = title,
                    LaunchPlanId = plan.Id
                };
                plan.ChecklistItems.Add(item);
            }

            item.IsRequired = true;
            item.IsComplete = isComplete;
            item.CompletedAt = isComplete ? SystemClock.UtcNow.AddDays(-1) : null;
            item.DeletedAt = null;
            item.Touch();
        }

        plan.RecalculateStatus();
        plan.Touch();
    }

    private sealed record DemoWorkflowSeed(
        string Title,
        string Slug,
        string ShortDescription,
        string VersionNumber,
        string TestingLocationName,
        string Focus,
        string Channels);

    private static async Task SeedLegacyIdentityAdminUserAsync(UserManager<LegacyIdentityUser> userManager, ILogger? logger, IConfiguration? configuration = null)
    {
        const string adminEmail = "admin@game-guild.com";
        var adminPassword = configuration?["Seed:AdminPassword"] ?? "Admin123!";
        if (adminPassword == "Admin123!")
        {
            logger?.LogWarning("Using default admin password. Set 'Seed:AdminPassword' in configuration for production");
        }

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new LegacyIdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin").ConfigureAwait(false);
                logger?.LogInformation("  Created admin user: {Email}", adminEmail);
            }
            else
            {
                logger?.LogWarning("  Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
