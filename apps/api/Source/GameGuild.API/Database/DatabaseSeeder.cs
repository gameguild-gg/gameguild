using Microsoft.AspNetCore.Identity;
using GameGuild.Identity.Tenants;

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
        var tenantRole = configuration?["Seed:AdminTenantRole"] ?? TenantRole.Owner.Value;

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
            logger?.LogInformation("  Created platform tenant owner membership for: {Email}", adminUser.Email);
        }
        else
        {
            membership.Role = string.IsNullOrWhiteSpace(membership.Role) ? tenantRole : membership.Role;
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
