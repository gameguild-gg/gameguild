using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using GameGuild.Source.Database.Seeding;

namespace GameGuild.Database;

/// <summary>
/// Main application database context for GameGuild
/// Manages all entities and provides unified data access
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    #region DbSets

    // Core Entities
    public DbSet<User> Users => Set<User>();

    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();

    // Localization
    public DbSet<Language> Languages => Set<Language>();

    public DbSet<ResourceLocalization> ResourceLocalizations => Set<ResourceLocalization>();

    // Feature Flags
    // public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    // public DbSet<FeatureFlagTarget> FeatureFlagTargets => Set<FeatureFlagTarget>();

    // public DbSet<FeatureFlagUsage> FeatureFlagUsage => Set<FeatureFlagUsage>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure base entities (timestamps, soft delete, etc.)
        modelBuilder.ConfigureBaseEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureTimestamps();

        // Apply all entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Tenant entity to not have a circular self-reference
        // Tenant entities should be global and not belong to other tenants
        modelBuilder.Entity<Tenant>().Ignore(t => t.Tenant);

        // Configure filtered unique index for PostgreSQL to ensure only one default tenant
        // PostgreSQL supports partial indexes with WHERE clauses
        modelBuilder.Entity<Tenant>().HasIndex(t => t.IsDefault).IsUnique().HasFilter("is_default = true").HasDatabaseName("ix_tenant_unique_default");

        // Configure filtered unique index to guarantee a single default language
        modelBuilder.Entity<Language>().HasIndex(language => language.IsDefault)
            .IsUnique()
            .HasFilter("is_default = true")
            .HasDatabaseName("ix_language_unique_default");

        // Configure snake_case naming for ALL database objects AFTER configurations are applied
        // This ensures that even explicitly set names in configurations get transformed to snake_case
        var snakeTransformer = CaseTransformerFactory.Snake;

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Transform table names to snake_case
            var tableName = entity.GetTableName();

            if (!string.IsNullOrEmpty(tableName)) { entity.SetTableName(snakeTransformer.Transform(tableName)); }

            // Transform column names to snake_case (including those set by configurations)
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();

                if (!string.IsNullOrEmpty(columnName)) { property.SetColumnName(snakeTransformer.Transform(columnName)); }
            }

            // Transform index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();

                if (!string.IsNullOrEmpty(indexName)) { index.SetDatabaseName(snakeTransformer.Transform(indexName)); }
            }

            // Transform foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var foreignKeyName = foreignKey.GetConstraintName();

                if (!string.IsNullOrEmpty(foreignKeyName)) { foreignKey.SetConstraintName(snakeTransformer.Transform(foreignKeyName)); }
            }

            // Transform primary key name to snake_case
            var primaryKey = entity.FindPrimaryKey();

            if (primaryKey == null) continue;

            var primaryKeyName = primaryKey.GetName();

            if (!string.IsNullOrEmpty(primaryKeyName)) { primaryKey.SetName(snakeTransformer.Transform(primaryKeyName)); }
        }
    }

    /// <summary>
    /// Override SaveChanges to handle timestamps and domain events
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();

        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to handle timestamps and domain events
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates timestamps and version numbers for tracked entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries().Where(entry => entry is { Entity: EntityBase, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            var entity = (EntityBase)entry.Entity;
            DateTime now = DateTime.UtcNow;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = now;
                    entity.UpdatedAt = now;
                    entity.Version = 1;

                    break;

                case EntityState.Modified:
                    entity.UpdatedAt = now;
                    entity.Version++;

                    break;
            }
        }
    }

    /// <summary>
    /// Gets entities with pending domain events
    /// </summary>
    public IEnumerable<IHasDomainEvents> GetEntitiesWithDomainEvents() { return ChangeTracker.Entries<IHasDomainEvents>().Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity); }

    /// <summary>
    /// Clears domain events from all tracked entities
    /// </summary>
    public void ClearDomainEvents()
    {
        var entitiesWithEvents = GetEntitiesWithDomainEvents().ToList();

        foreach (var entity in entitiesWithEvents) { entity.ClearDomainEvents(); }
    }

    /// <summary>
    /// Seeds the database with initial data and initializes tenant cache
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
    ILogger<LanguageSeeder> languageSeederLogger = serviceProvider.GetRequiredService<ILogger<LanguageSeeder>>();
    var languageSeeder = new LanguageSeeder(languageSeederLogger);
    await languageSeeder.SeedAsync(this, cancellationToken);

    ILogger<TenantSeeder> logger = serviceProvider.GetRequiredService<ILogger<TenantSeeder>>();
    var tenantSeeder = new TenantSeeder(logger);
        await tenantSeeder.SeedAsync(this, cancellationToken);

        // Initialize tenant cache after seeding
        var tenantCacheService = serviceProvider.GetRequiredService<ITenantCacheService>();
        await tenantCacheService.InitializeCacheAsync(cancellationToken);
    }
}
