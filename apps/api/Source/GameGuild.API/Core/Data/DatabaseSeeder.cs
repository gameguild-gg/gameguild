using Microsoft.AspNetCore.Identity;

namespace GameGuild.API.Data;

// Type aliases for ASP.NET Core Identity
using Role = IdentityRole;
using User = IdentityUser;

/// <summary>
///     Database seeder for default roles and admin user
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    ///     Seeds the database with default roles and admin user
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed admin user
        await SeedAdminUserAsync(userManager, context);
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        var roles = new[ ] { "Admin", "User", "Manager" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(), Name = roleName, NormalizedName = roleName.ToUpper()
                    // Note: IdentityRole doesn't have Description, TenantId, CreatedAt, UpdatedAt properties
                };

                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager, ApplicationDbContext context)
    {
        const string adminEmail = "admin@game-guild.com";
        const string adminPassword = "Admin123!";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            // TODO: Create default tenant when tenant functionality is implemented
            // var defaultTenant = new Tenant { ... };
            // context.Tenants.Add(defaultTenant);
            // await context.SaveChangesAsync();

            // Create admin user with basic IdentityUser properties
            var adminUser = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(), UserName = adminEmail, Email = adminEmail, EmailConfirmed = true
                // Note: IdentityUser doesn't have FirstName, LastName, IsActive, TenantId, CreatedAt, UpdatedAt properties
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded) { await userManager.AddToRoleAsync(adminUser, "Admin"); }
        }
    }
}
