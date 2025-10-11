using GameGuild.Database;
using GameGuild.Modules.Tenants;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Experiments.Entities;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Resources;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;
using GameGuild.Modules.TestingLab.Entities;
using GameGuild.Source.Database.Seeding;

namespace GameGuild.Database;

/// <summary>
/// Main application database context for GameGuild
/// Manages all entities and provides unified data access
/// </summary>
public class ApplicationDbContext : DbContext {
    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    #region DbSets

    // Core Entities
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();

    // User Profiles
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // Localization
    public DbSet<Language> Languages => Set<Language>();

    public DbSet<ResourceLocalization> ResourceLocalizations => Set<ResourceLocalization>();

    // Resources
    public DbSet<ResourceQuota> ResourceQuotas => Set<ResourceQuota>();

    public DbSet<ResourceUsageRecord> ResourceUsageRecords => Set<ResourceUsageRecord>();

    // Authentication
    public DbSet<AuthenticationAttempt> AuthenticationAttempts => Set<AuthenticationAttempt>();

    public DbSet<MfaAttempt> MfaAttempts => Set<MfaAttempt>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();

    public DbSet<UserMfaConfiguration> UserMfaConfigurations => Set<UserMfaConfiguration>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Permissions
    public DbSet<TenantPermission> TenantPermissions => Set<TenantPermission>();

    public DbSet<ContentTypePermission> ContentTypePermissions => Set<ContentTypePermission>();

    public DbSet<PermissionAuditLog> PermissionAuditLogs => Set<PermissionAuditLog>();

    public DbSet<GameGuild.Modules.Permissions.Entities.PermissionTemplate> PermissionTemplates => Set<GameGuild.Modules.Permissions.Entities.PermissionTemplate>();

    public DbSet<PermissionDelegation> PermissionDelegations => Set<PermissionDelegation>();

    // Experiments
    public DbSet<PricingExperiment> PricingExperiments => Set<PricingExperiment>();

    public DbSet<ExperimentVariant> ExperimentVariants => Set<ExperimentVariant>();

    public DbSet<UserAssignment> UserAssignments => Set<UserAssignment>();

    public DbSet<ExperimentResult> ExperimentResults => Set<ExperimentResult>();

    // Feature Flags
    // public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    // public DbSet<FeatureFlagTarget> FeatureFlagTargets => Set<FeatureFlagTarget>();

    // public DbSet<FeatureFlagUsage> FeatureFlagUsage => Set<FeatureFlagUsage>();

    // User Achievements (Gamification)
    public DbSet<GameGuild.Modules.UserAchievements.Achievement> Achievements => Set<GameGuild.Modules.UserAchievements.Achievement>();

    public DbSet<GameGuild.Modules.UserAchievements.UserAchievement> UserAchievements => Set<GameGuild.Modules.UserAchievements.UserAchievement>();

    public DbSet<GameGuild.Modules.UserAchievements.AchievementLevel> AchievementLevels => Set<GameGuild.Modules.UserAchievements.AchievementLevel>();

    public DbSet<GameGuild.Modules.UserAchievements.AchievementPrerequisite> AchievementPrerequisites => Set<GameGuild.Modules.UserAchievements.AchievementPrerequisite>();

    public DbSet<GameGuild.Modules.UserAchievements.AchievementProgress> AchievementProgress => Set<GameGuild.Modules.UserAchievements.AchievementProgress>();

    // TestingLab Module
    public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();

    public DbSet<TestingSession> TestingSessions => Set<TestingSession>();

    public DbSet<TestingParticipant> TestingParticipants => Set<TestingParticipant>();

    public DbSet<TestingFeedback> TestingFeedbacks => Set<TestingFeedback>();

    public DbSet<TestingFeedbackForm> TestingFeedbackForms => Set<TestingFeedbackForm>();

    public DbSet<FeedbackQualityRating> FeedbackQualityRatings => Set<FeedbackQualityRating>();

    public DbSet<TestingLocation> TestingLocations => Set<TestingLocation>();

    public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();

    public DbSet<SessionWaitlist> SessionWaitlists => Set<SessionWaitlist>();

    public DbSet<SessionProject> SessionProjects => Set<SessionProject>();

    public DbSet<TestingAnalytics> TestingAnalytics => Set<TestingAnalytics>();

    public DbSet<TestingContext> TestingContexts => Set<TestingContext>();

    public DbSet<TestingLabSettings> TestingLabSettings => Set<TestingLabSettings>();

    public DbSet<TestingFeedbackStats> TestingFeedbackStats => Set<TestingFeedbackStats>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // Configure base entities (timestamps, soft delete, etc.)
        modelBuilder.ConfigureBaseEntities();
        modelBuilder.ConfigureSoftDelete();
        modelBuilder.ConfigureTimestamps();

        // Apply all entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure PostgreSQL-specific features (skip for InMemory provider)
        // Check if using InMemory provider by examining the database provider name
        var providerName = Database.ProviderName;
        var isInMemory = providerName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isInMemory) {
            // InMemory provider doesn't support Dictionary<string, object> properties
            // Ignore these properties for InMemory database
            modelBuilder.Entity<PermissionAuditLog>().Ignore(pal => pal.Metadata);
            modelBuilder.Entity<PermissionDelegation>().Ignore(pd => pd.Conditions);
            modelBuilder.Entity<PermissionTemplate>().Ignore(pt => pt.Metadata);
        }
        else {
            // Configure jsonb column types for Dictionary<string, object> properties (PostgreSQL)
            modelBuilder.Entity<PermissionAuditLog>()
                .Property(pal => pal.Metadata)
                .HasColumnType("jsonb");

            modelBuilder.Entity<PermissionDelegation>()
                .Property(pd => pd.Conditions)
                .HasColumnType("jsonb");

            modelBuilder.Entity<PermissionTemplate>()
                .Property(pt => pt.Metadata)
                .HasColumnType("jsonb");
        }

        // Configure Tenant entity to not have a circular self-reference
        // Tenant entities should be global and not belong to other tenants
        modelBuilder.Entity<Tenant>().Ignore(t => t.Tenant);

        // Configure filtered unique index for PostgreSQL to ensure only one default tenant
        // PostgreSQL supports partial indexes with WHERE clauses
        modelBuilder.Entity<Tenant>().HasIndex(t => t.IsDefault).IsUnique().HasFilter("is_default = true").HasDatabaseName("ix_tenant_unique_default");

        // Configure filtered unique index to guarantee a single default language
        modelBuilder.Entity<Language>().HasIndex(language => language.IsDefault).IsUnique().HasFilter("is_default = true").HasDatabaseName("ix_language_unique_default");

        // Configure snake_case naming for ALL database objects AFTER configurations are applied
        // This ensures that even explicitly set names in configurations get transformed to snake_case
        var snakeTransformer = CaseTransformerFactory.Snake;

        foreach (var entity in modelBuilder.Model.GetEntityTypes()) {
            // Transform table names to snake_case
            var tableName = entity.GetTableName();

            if (!string.IsNullOrEmpty(tableName)) { entity.SetTableName(snakeTransformer.Transform(tableName)); }

            // Transform column names to snake_case (including those set by configurations)
            foreach (var property in entity.GetProperties()) {
                var columnName = property.GetColumnName();

                if (!string.IsNullOrEmpty(columnName)) { property.SetColumnName(snakeTransformer.Transform(columnName)); }
            }

            // Transform index names to snake_case
            foreach (var index in entity.GetIndexes()) {
                var indexName = index.GetDatabaseName();

                if (!string.IsNullOrEmpty(indexName)) { index.SetDatabaseName(snakeTransformer.Transform(indexName)); }
            }

            // Transform foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys()) {
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
    public override int SaveChanges() {
        UpdateTimestamps();

        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to handle timestamps and domain events
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        UpdateTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates timestamps and version numbers for tracked entities
    /// </summary>
    private void UpdateTimestamps() {
        var entries = ChangeTracker.Entries().Where(entry => entry is { Entity: EntityBase, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries) {
            var entity = (EntityBase)entry.Entity;
            DateTime now = DateTime.UtcNow;

            switch (entry.State) {
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
    public void ClearDomainEvents() {
        var entitiesWithEvents = GetEntitiesWithDomainEvents().ToList();

        foreach (var entity in entitiesWithEvents) { entity.ClearDomainEvents(); }
    }

    /// <summary>
    /// Seeds the database with initial data and initializes tenant cache
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) {
        var languageSeederLogger = serviceProvider.GetRequiredService<ILogger<LanguageSeeder>>();
        LanguageSeeder languageSeeder = new(languageSeederLogger);
        await languageSeeder.SeedAsync(this, cancellationToken);

        var logger = serviceProvider.GetRequiredService<ILogger<TenantSeeder>>();
        var languageRepository = serviceProvider.GetRequiredService<ILanguageRepository>();
        TenantSeeder tenantSeeder = new(logger, languageRepository);
        await tenantSeeder.SeedAsync(this, cancellationToken);

        // Initialize tenant cache after seeding
        var tenantCacheService = serviceProvider.GetRequiredService<ITenantCacheService>();
        await tenantCacheService.InitializeCacheAsync(cancellationToken);
    }
}
