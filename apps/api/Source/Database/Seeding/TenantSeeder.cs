using GameGuild.Database;
using GameGuild.Database.Seeding;
using GameGuild.Modules.Tenants;

namespace GameGuild.Source.Database.Seeding;

/// <summary>
/// Seeds initial tenant data into the database
/// </summary>
/// <param name="logger">The logger to use for this seeder</param>
public class TenantSeeder(ILogger<TenantSeeder> logger) : IDataSeeder
{
    private readonly ILogger<TenantSeeder> _logger = logger;

    /// <summary>
    /// Seeds the initial tenant data
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting tenant seeding...");

        // Check if default tenant already exists
        _logger.LogInformation("Checking for existing default tenant...");
        Tenant? existingDefaultTenant = await context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsDefault, cancellationToken);

        if (existingDefaultTenant != null) { _logger.LogInformation("Default tenant already exists: {TenantName} (ID: {TenantId})", existingDefaultTenant.Name, existingDefaultTenant.Id); }

        // Check for GameGuild tenant specifically
        _logger.LogInformation("Checking for GameGuild tenant...");
        Tenant? gameGuildTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "gameguild", cancellationToken);

        if (gameGuildTenant != null)
        {
            _logger.LogInformation("GameGuild tenant already exists: {TenantName} (ID: {TenantId})", gameGuildTenant.Name, gameGuildTenant.Id);

            // Ensure it's set as default if no other default exists
            if (existingDefaultTenant == null && !gameGuildTenant.IsDefault)
            {
                _logger.LogInformation("Setting GameGuild tenant as default...");
                gameGuildTenant.IsDefault = true;
                context.Tenants.Update(gameGuildTenant);
            }
        }
        else
        {
            _logger.LogInformation("Creating new default GameGuild tenant...");
            gameGuildTenant = new Tenant { Name = "GameGuild", Description = "The default GameGuild tenant for the application", Slug = "gameguild", IsActive = true, IsDefault = true };

            context.Tenants.Add(gameGuildTenant);
            _logger.LogInformation("Added new GameGuild tenant to context");
        }

        _logger.LogInformation("Saving tenant changes to database...");
        int changesSaved = await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {ChangeCount} changes to database", changesSaved);

        // Now seed the tenant settings and domain
        await SeedTenantSettingsAsync(context, gameGuildTenant, cancellationToken);
        await SeedTenantDomainAsync(context, gameGuildTenant, cancellationToken);

        // Seed Champlain College tenant
        await SeedChamplainCollegeTenantAsync(context, cancellationToken);

        _logger.LogInformation("Tenant seeding completed successfully");
    }

    /// <summary>
    /// Seeds tenant settings for the specified tenant
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="tenant">The tenant to seed settings for</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private async Task SeedTenantSettingsAsync(ApplicationDbContext context, Tenant tenant, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for existing TenantSettings for tenant {TenantId}...", tenant.Id);

        TenantSettings? existingSettings = await context.TenantSettings.FirstOrDefaultAsync(ts => ts.TenantId == tenant.Id, cancellationToken);

        if (existingSettings != null)
        {
            _logger.LogInformation("TenantSettings already exist for tenant {TenantId}", tenant.Id);

            return;
        }

        _logger.LogInformation("Creating default TenantSettings for tenant {TenantId}...", tenant.Id);
        TenantSettings defaultSettings = TenantSettings.CreateDefault(tenant.Id);

        context.TenantSettings.Add(defaultSettings);

        int settingsChangesSaved = await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {ChangeCount} TenantSettings changes to database", settingsChangesSaved);
    }

    /// <summary>
    /// Seeds tenant domain for the specified tenant
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="tenant">The tenant to seed domain for</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private async Task SeedTenantDomainAsync(ApplicationDbContext context, Tenant tenant, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for existing TenantDomain for tenant {TenantId}...", tenant.Id);

        TenantDomain? existingDomain = await context.TenantDomains.FirstOrDefaultAsync(td => td.TenantId == tenant.Id, cancellationToken);

        if (existingDomain != null)
        {
            _logger.LogInformation("TenantDomain already exists for tenant {TenantId}: {DomainName}", tenant.Id, existingDomain.FullDomainName);

            return;
        }

        _logger.LogInformation("Creating default TenantDomain for tenant {TenantId}...", tenant.Id);

        string domainName = tenant.Slug switch
        {
            "gameguild" => "gameguild.com",
            "champlain" => "champlain.edu",
            _ => "example.com"
        };

        var defaultDomain = new TenantDomain
        {
            TenantId = tenant.Id,
            TopLevelDomain = domainName,
            Subdomain = null, // Main domain, no subdomain
            IsMainDomain = true,
            IsSecondaryDomain = false
        };

        context.TenantDomains.Add(defaultDomain);

        int domainChangesSaved = await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {ChangeCount} TenantDomain changes to database for domain: {DomainName}", domainChangesSaved, defaultDomain.FullDomainName);
    }

    /// <summary>
    /// Seeds Champlain College tenant with settings and domains
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private async Task SeedChamplainCollegeTenantAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for Champlain College tenant...");

        Tenant? champlainTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "champlain", cancellationToken);

        if (champlainTenant != null) { _logger.LogInformation("Champlain College tenant already exists: {TenantName} (ID: {TenantId})", champlainTenant.Name, champlainTenant.Id); }
        else
        {
            _logger.LogInformation("Creating Champlain College tenant...");
            champlainTenant = new Tenant { Name = "Champlain College", Description = "Champlain College educational institution tenant", Slug = "champlain", IsActive = true, IsDefault = false };

            context.Tenants.Add(champlainTenant);
            _logger.LogInformation("Added Champlain College tenant to context");
        }

        _logger.LogInformation("Saving Champlain College tenant changes to database...");
        int changesSaved = await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {ChangeCount} Champlain College tenant changes to database", changesSaved);

        // Seed tenant settings and domains for Champlain College
        await SeedTenantSettingsAsync(context, champlainTenant, cancellationToken);
        await SeedTenantDomainAsync(context, champlainTenant, cancellationToken);
        await SeedChamplainSubdomainsAsync(context, champlainTenant, cancellationToken);
    }

    /// <summary>
    /// Seeds subdomains for Champlain College
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="tenant">The Champlain College tenant</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private async Task SeedChamplainSubdomainsAsync(ApplicationDbContext context, Tenant tenant, CancellationToken cancellationToken)
    {
        var subdomains = new[ ] { "student", "faculty", "staff", "alumni" };

        foreach (string subdomain in subdomains)
        {
            TenantDomain? existingSubdomain = await context.TenantDomains.FirstOrDefaultAsync(td => td.TenantId == tenant.Id && td.Subdomain == subdomain, cancellationToken);

            if (existingSubdomain != null)
            {
                _logger.LogInformation("Subdomain already exists: {SubdomainName}.champlain.edu", subdomain);

                continue;
            }

            var tenantSubdomain = new TenantDomain { TenantId = tenant.Id, TopLevelDomain = "champlain.edu", Subdomain = subdomain, IsMainDomain = false, IsSecondaryDomain = true };

            context.TenantDomains.Add(tenantSubdomain);
            _logger.LogInformation("Added subdomain: {SubdomainName}.champlain.edu", subdomain);
        }

        int subdomainChangesSaved = await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully saved {ChangeCount} Champlain College subdomain changes to database", subdomainChangesSaved);
    }
}
