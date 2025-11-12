using GameGuild.Abstractions;
using GameGuild.Authentication.Entities;
using GameGuild.Payments.Entities;
using GameGuild.Resources.Entities;
using GameGuild.Tenants.Entities;
using GameGuild.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.API.Data;

/// <summary>
///     Main application database context that implements IApplicationDbContext.
///     Contains all entities from all modules in the application.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) { return await Database.BeginTransactionAsync(cancellationToken); }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply Tenants module configurations
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMemberConfiguration());
        modelBuilder.ApplyConfiguration(new TenantDomainConfiguration());
        modelBuilder.ApplyConfiguration(new TenantSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new TenantStatisticsConfiguration());
        modelBuilder.ApplyConfiguration(new UsageTrackingConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMetadataConfiguration());

        // Apply Resources module configurations (using automatic discovery)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourceQuota).Assembly, type => type.Namespace?.StartsWith("GameGuild.Resources.Entities") == true);

        // Apply Authentication module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthUser).Assembly, type => type.Namespace?.StartsWith("GameGuild.Authentication.Entities") == true);

        // Apply Payments module configurations  
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RevenueEvent).Assembly, type => type.Namespace?.StartsWith("GameGuild.Payments.Entities") == true);

        // Apply Users module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(User).Assembly, type => type.Namespace?.StartsWith("GameGuild.Users.Entities") == true);

        base.OnModelCreating(modelBuilder);
    }

    #region Users Module

    public DbSet<User> Users { get => Set<User>(); }

    public DbSet<UserMetadata> UserMetadata { get => Set<UserMetadata>(); }

    public DbSet<UserPreferences> UserPreferences { get => Set<UserPreferences>(); }

    public DbSet<UserProfile> UserProfiles { get => Set<UserProfile>(); }

    public DbSet<UserNotification> UserNotifications { get => Set<UserNotification>(); }

    #endregion

    #region Tenants Module

    public DbSet<Tenant> Tenants { get => Set<Tenant>(); }

    public DbSet<TenantMember> TenantMembers { get => Set<TenantMember>(); }

    public DbSet<TenantDomain> TenantDomains { get => Set<TenantDomain>(); }

    public DbSet<TenantSettings> TenantSettings { get => Set<TenantSettings>(); }

    public DbSet<TenantStatistics> TenantStatistics { get => Set<TenantStatistics>(); }

    public DbSet<UsageTracking> UsageTracking { get => Set<UsageTracking>(); }

    public DbSet<TenantMetadata> TenantMetadata { get => Set<TenantMetadata>(); }

    #endregion

    #region Resources Module

    public DbSet<ResourceQuota> ResourceQuotas { get => Set<ResourceQuota>(); }

    public DbSet<UsageRecord> UsageRecords { get => Set<UsageRecord>(); }

    public DbSet<CostAllocationReport> CostAllocationReports { get => Set<CostAllocationReport>(); }

    public DbSet<ResourceUsageTrend> ResourceUsageTrends { get => Set<ResourceUsageTrend>(); }

    public DbSet<ResourceThrottlingPolicy> ResourceThrottlingPolicies { get => Set<ResourceThrottlingPolicy>(); }

    public DbSet<UsageRetentionPolicy> UsageRetentionPolicies { get => Set<UsageRetentionPolicy>(); }

    public DbSet<SlaImpactAnalysis> SlaImpactAnalyses { get => Set<SlaImpactAnalysis>(); }

    #endregion

    #region Audit Module

    public DbSet<GameGuild.Audit.AuditLog> AuditLogs { get => Set<GameGuild.Audit.AuditLog>(); }

    #endregion

    #region Authentication Module

    public DbSet<AuthUser> AuthUsers { get => Set<AuthUser>(); }

    public DbSet<AuthenticationAttempt> AuthenticationAttempts { get => Set<AuthenticationAttempt>(); }

    public DbSet<RefreshToken> RefreshTokens { get => Set<RefreshToken>(); }

    public DbSet<UserSession> UserSessions { get => Set<UserSession>(); }

    public DbSet<TrustedDevice> TrustedDevices { get => Set<TrustedDevice>(); }

    public DbSet<MfaAttempt> MfaAttempts { get => Set<MfaAttempt>(); }

    public DbSet<UserMfaConfiguration> UserMfaConfigurations { get => Set<UserMfaConfiguration>(); }

    public DbSet<Role> Roles { get => Set<Role>(); }

    public DbSet<UserRole> UserRoles { get => Set<UserRole>(); }

    public DbSet<AbacPolicy> AbacPolicies { get => Set<AbacPolicy>(); }

    public DbSet<ConditionalPolicy> ConditionalPolicies { get => Set<ConditionalPolicy>(); }

    public DbSet<TenantPermission> TenantPermissions { get => Set<TenantPermission>(); }

    #endregion

    #region Payments Module

    public DbSet<RevenueEvent> RevenueEvents { get => Set<RevenueEvent>(); }

    public DbSet<FinancialLedgerEntry> FinancialLedgerEntries { get => Set<FinancialLedgerEntry>(); }

    public DbSet<AuditTrail> AuditTrails { get => Set<AuditTrail>(); }

    #endregion
}
